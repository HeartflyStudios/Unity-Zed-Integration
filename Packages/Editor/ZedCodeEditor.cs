using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using Unity.CodeEditor;
using Debug = UnityEngine.Debug;

namespace HFS.ZedEditor
{
    [InitializeOnLoad]
    public class ZedCodeEditor : IExternalCodeEditor
    {
        static ZedCodeEditor()
        {
            CodeEditor.Register(new ZedCodeEditor());
        }

        private ZedProjectGenerator _generator;

        public CodeEditor.Installation[] Installations => new[]
        {
            new CodeEditor.Installation
            {
                Name = "Zed Editor",
                Path = ZedPathLocator.GetPath()
            }
        };

        public void Initialize(string editorInstallationPath)
        {
            string projectDir = Directory.GetParent(Application.dataPath).FullName;
            _generator = new ZedProjectGenerator(projectDir);
        }

        private ProjectGenerationFlag CurrentFlags
        {
            get => (ProjectGenerationFlag)EditorPrefs.GetInt("ZedEditor_ProjectGenerationFlags", (int)(ProjectGenerationFlag.Local | ProjectGenerationFlag.Embedded));
            set => EditorPrefs.SetInt("ZedEditor_ProjectGenerationFlags", (int)value);
        }

        public bool TryGetInstallationForPath(string installationPath, out CodeEditor.Installation installation)
        {
            var lowerPath = installationPath.ToLower();
            if (lowerPath.Contains("zed"))
            {
                installation = new CodeEditor.Installation
                {
                    Name = "Zed Editor",
                    Path = installationPath
                };
                return true;
            }

            installation = default;
            return false;
        }

        public bool OpenProject(string path, int line, int column)
        {
            string executable = ZedPathLocator.GetPath();
            string projectDir = Directory.GetParent(Application.dataPath).FullName;
            string args = $"\"{projectDir}\" \"{path}";

            if (line > 0)
            {
                args += column > 0 ? $":{line}:{column}\"" : $":{line}\"";
            }
            else
            {
                args += "\"";
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = args,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Zed] Failed to open {path}: {e.Message}");
                return false;
            }
        }

        public void SyncAll()
        {
            if (_generator == null)
            {
                string projectDir = Directory.GetParent(Application.dataPath).FullName;
                _generator = new ZedProjectGenerator(projectDir);
            }

            var allAssemblies = CompilationPipeline.GetAssemblies();

            _generator.GenerateAll(allAssemblies, CurrentFlags);

            AssetDatabase.Refresh();
        }

        public void SyncIfNeeded(string[] added, string[] deleted, string[] moved, string[] asset, string[] affected)
        {
            var scriptOrMetadataChanges = added.Concat(deleted).Concat(moved).Concat(affected)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".asmdef") || f.EndsWith(".asmref"))
                .ToArray();

            if (scriptOrMetadataChanges.Length == 0 || _generator == null)
            {
                return;
            }

            // 1. Check if the physical layout of the files changed
            bool structureChanged = added.Length > 0 ||
                                    deleted.Length > 0 ||
                                    moved.Length > 0 ||
                                    scriptOrMetadataChanges.Any(f => f.EndsWith(".asmdef")) ||
                                    scriptOrMetadataChanges.Any(f => f.EndsWith(".asmref"));

            if (structureChanged)
            {
                // This ensures Unity has completely finished mapping the new/moved files
                EditorApplication.delayCall += () =>
                {
                    var allAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

                    var filteredAssemblies = allAssemblies
                        .Where(a => _generator.ShouldGenerate(a, CurrentFlags))
                        .ToArray();

                    foreach (var assembly in filteredAssemblies)
                    {
                        _generator.GenerateCsproj(assembly, allAssemblies);
                    }

                    _generator.GenerateSolution(filteredAssemblies);

                    Debug.Log($"[Zed] Workspace synced. Added/Moved/Deleted files updated.");
                };
            }

            // var allAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            // var assembliesToRebuild = new HashSet<UnityEditor.Compilation.Assembly>();

            // foreach (var path in scriptOrMetadataChanges)
            // {
            //     string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(path);
            //     if (string.IsNullOrEmpty(assemblyName))
            //     {
            //         continue;
            //     }

            //     string cleanName = assemblyName.Replace(".dll", "");
            //     var assembly = allAssemblies.FirstOrDefault(a => a.name == cleanName);

            //     if (assembly != null && _generator.ShouldGenerate(assembly, CurrentFlags))
            //     {
            //         assembliesToRebuild.Add(assembly);
            //     }
            // }

            // // Surgical Update: Only rewrite the .csproj files for affected assemblies
            // foreach (var assembly in assembliesToRebuild)
            // {
            //     _generator.GenerateCsproj(assembly, allAssemblies);
            // }

            // // Structural Update: If files were moved/added/deleted, or an asmdef changed, rebuild the .sln
            // bool structureChanged = added.Length > 0 || deleted.Length > 0 || moved.Length > 0 ||
            //                         scriptOrMetadataChanges.Any(f => f.EndsWith(".asmdef"));

            // if (structureChanged)
            // {
            //     var filtered = allAssemblies.Where(a => _generator.ShouldGenerate(a, CurrentFlags)).ToArray();
            //     _generator.GenerateSolution(filtered);
            // }
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Zed Project Generation Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Generate .csproj files for:", EditorStyles.label);

            // Using a scope for automatic indentation management
            using (new EditorGUI.IndentLevelScope())
            {
                CurrentFlags = DrawToggle(ProjectGenerationFlag.Embedded, "Embedded packages", CurrentFlags);
                CurrentFlags = DrawToggle(ProjectGenerationFlag.Local, "Local packages", CurrentFlags);
                CurrentFlags = DrawToggle(ProjectGenerationFlag.OpenedPackages, "Registry packages", CurrentFlags);
                CurrentFlags = DrawToggle(ProjectGenerationFlag.Git, "Git packages", CurrentFlags);
                CurrentFlags = DrawToggle(ProjectGenerationFlag.BuiltInPackages, "Built-in packages", CurrentFlags);
                CurrentFlags = DrawToggle(ProjectGenerationFlag.LocalTarBall, "Local tarball", CurrentFlags);
                CurrentFlags = DrawToggle(ProjectGenerationFlag.Unknown, "Packages from unknown sources", CurrentFlags);
                CurrentFlags = DrawToggle(ProjectGenerationFlag.PlayerAssemblies, "Player projects", CurrentFlags);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Regenerate project files", GUILayout.Height(25)))
            {
                SyncAll();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SyncAll();
            }
        }

        private ProjectGenerationFlag DrawToggle(ProjectGenerationFlag flag, string label, ProjectGenerationFlag current)
        {
            bool isActive = (current & flag) != 0;
            bool newValue = EditorGUILayout.Toggle(label, isActive);

            if (newValue != isActive)
            {
                return newValue ? (current | flag) : (current & ~flag);
            }
            return current;
        }
    }

    [Flags]
    public enum ProjectGenerationFlag
    {
        None = 0,
        Local = 1 << 0,
        Embedded = 1 << 1,
        OpenedPackages = 1 << 2,
        Git = 1 << 3,
        BuiltInPackages = 1 << 4,
        Unknown = 1 << 5,
        PlayerAssemblies = 1 << 6,
        LocalTarBall = 1 << 7,
        All = ~0
    }
}
