using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;

namespace HFS.ZedEditor
{
    public class ZedProjectGenerator
    {
        internal string ProjectDirectory { get; }

        public ZedProjectGenerator(string projectDirectory)
        {
            ProjectDirectory = Path.GetFullPath(projectDirectory).Replace("\\", "/");
        }

        internal string SolutionFile() => Path.Combine(ProjectDirectory, $"{PlayerSettings.productName}.sln");
        internal string ProjectFile(string assemblyName) => Path.Combine(ProjectDirectory, $"{assemblyName}.csproj");

        // Not sure this is the best way to do this, but it works
        // Unity probably already has a deterministic GUID generator - should probably be using that if so
        private string GetDeterministicGuid(string name)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
                return new Guid(hash).ToString().ToUpper();
            }
        }

        private string GetAnalyzerPath()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ZedCodeEditor).Assembly);
            if (packageInfo != null)
            {
                string path = Path.Combine(packageInfo.resolvedPath, "Analyzers~/Microsoft.Unity.Analyzers.dll");
                return Path.GetFullPath(path).Replace("\\", "/");
            }
            return string.Empty;
        }

        internal void GenerateAll(UnityEditor.Compilation.Assembly[] _, ProjectGenerationFlag flags)
        {
            var generatedProjectFiles = new List<string>();
            var validAssembliesForSolution = new List<UnityEditor.Compilation.Assembly>();

            GenerateZedSettings();
            GenerateDirectoryBuildProps();

            var comprehensiveAssemblyList = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            foreach (var assembly in comprehensiveAssemblyList)
            {
                if (ShouldGenerate(assembly, flags))
                {
                    GenerateCsproj(assembly, comprehensiveAssemblyList);
                    generatedProjectFiles.Add($"{assembly.name}.csproj");
                    validAssembliesForSolution.Add(assembly);
                }
            }

            GenerateSolution(validAssembliesForSolution.ToArray());
            CleanupOrphanedProjects(generatedProjectFiles);
        }

        internal void GenerateDirectoryBuildProps()
        {
            string path = Path.Combine(ProjectDirectory, "Directory.Build.props");

            string content = @"<Project>
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <IsRestorable>false</IsRestorable>
    <RestoreProjectStyle>Unknown</RestoreProjectStyle>
    <RegisterForRestore>false</RegisterForRestore>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
    <CheckForOverflowUnderflow>false</CheckForOverflowUnderflow>
    <Deterministic>true</Deterministic>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <ResolveNuGetPackages>false</ResolveNuGetPackages>
  </PropertyGroup>
  <Target Name=""Restore"" />
  <Target Name=""_IsProjectRestoreSupported"" Returns=""false"" />
</Project>";

            File.WriteAllText(path, content);
        }

        internal void GenerateCsproj(UnityEditor.Compilation.Assembly assembly, UnityEditor.Compilation.Assembly[] allAssemblies)
        {
            var csprojPath = ProjectFile(assembly.name);
            var sb = new StringBuilder();
            string projectGuid = GetDeterministicGuid(assembly.name);


            // Project File Header
            sb.AppendLine($@"<Project Sdk=""Microsoft.NET.Sdk"">");
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine($@"    <ProjectGuid>{{{projectGuid}}}</ProjectGuid>");
            sb.AppendLine($@"    <AssemblyName>{assembly.name}</AssemblyName>");

            if (!string.IsNullOrEmpty(assembly.rootNamespace))
            {
                sb.AppendLine($@"    <RootNamespace>{assembly.rootNamespace}</RootNamespace>");
            }

            sb.AppendLine($"    <DefineConstants>{string.Join(";", assembly.defines)}</DefineConstants>");
            sb.AppendLine("    <OutputPath>Temp/bin/Debug/</OutputPath>");
            sb.AppendLine("  </PropertyGroup>");

            // Absolute paths enforced for source and refs because I was getting annoyed - probably not necessary
            // Source Files
            sb.AppendLine("  <ItemGroup>");
            foreach (var file in assembly.sourceFiles)
            {
                string fullPath = Path.GetFullPath(file).Replace("\\", "/");
                sb.AppendLine($"    <Compile Include=\"{fullPath}\" />");
            }
            sb.AppendLine("  </ItemGroup>");

            // References
            sb.AppendLine("  <ItemGroup>");
            foreach (var referencePath in assembly.allReferences)
            {
                string refName = Path.GetFileNameWithoutExtension(referencePath);
                string fullPath = Path.GetFullPath(referencePath).Replace("\\", "/");

                if (File.Exists(ProjectFile(refName)))
                {
                    sb.AppendLine($@"    <ProjectReference Include=""{refName}.csproj"" />");
                }
                else
                {
                    sb.AppendLine($@"    <Reference Include=""{refName}"">");
                    sb.AppendLine($@"      <HintPath>{fullPath}</HintPath>");
                    sb.AppendLine("    </Reference>");
                }
            }
            sb.AppendLine("  </ItemGroup>");

            // Analyzers
            string analyzerPath = GetAnalyzerPath();
            if (File.Exists(analyzerPath))
            {
                sb.AppendLine("  <ItemGroup>");
                sb.AppendLine($@"    <Analyzer Include=""{analyzerPath}"" />");
                sb.AppendLine("  </ItemGroup>");
            }

            sb.AppendLine("</Project>");
            string newContent = sb.ToString();
            if (File.Exists(csprojPath))
            {
                try
                {
                    if (File.ReadAllText(csprojPath) == newContent)
                    {
                        return;
                    }
                }
                catch (IOException)
                {
                    // If we can't even read it, Unity is likely mid-write.
                    // We'll fall through to the retry loop.
                }
            }

            int retries = 10;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    File.WriteAllText(csprojPath, newContent);
                    break;
                }
                catch (IOException)
                {
                    if (i == retries - 1)
                    {
                        Debug.LogWarning($"[Zed] Could not update {assembly.name}.csproj - File is locked.");
                    }

                    System.Threading.Thread.Sleep(50);
                }
            }
        }

        internal bool ShouldGenerate(UnityEditor.Compilation.Assembly assembly, ProjectGenerationFlag flags)
        {
            if (flags.HasFlag(ProjectGenerationFlag.All))
            {
                return true;
            }

            string firstFile = assembly.sourceFiles.FirstOrDefault();

            if (string.IsNullOrEmpty(firstFile))
            {
                return false;
            }

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(firstFile);
            if (packageInfo == null)
            {
                // If it's in 'Assets/' and not a package, it's user code (Local)
                if (firstFile.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    return flags.HasFlag(ProjectGenerationFlag.Local);
                }

                return flags.HasFlag(ProjectGenerationFlag.Unknown);
            }

            return packageInfo.source switch
            {
                // "Embedded" = Packages inside the 'Packages' folder of the project
                PackageSource.Embedded => flags.HasFlag(ProjectGenerationFlag.Embedded),

                // "Local" = Packages referenced via 'file:..' in manifest.json (outside project)
                PackageSource.Local => flags.HasFlag(ProjectGenerationFlag.Local),

                // "Git" = Packages pulled via URL
                PackageSource.Git => flags.HasFlag(ProjectGenerationFlag.Git),

                // "Registry" = Standard scoped registries or Unity's main registry
                PackageSource.Registry => flags.HasFlag(ProjectGenerationFlag.OpenedPackages),

                // "BuiltIn" = Unity's internal modules (UI, Physics, etc.)
                PackageSource.BuiltIn => flags.HasFlag(ProjectGenerationFlag.BuiltInPackages),

                PackageSource.LocalTarball => flags.HasFlag(ProjectGenerationFlag.LocalTarBall),

                _ => flags.HasFlag(ProjectGenerationFlag.Unknown),
            };
        }

        internal void GenerateSolution(UnityEditor.Compilation.Assembly[] assemblies)
        {
            var slnPath = SolutionFile();
            var sb = new StringBuilder();

            // Old .sln format is most compatible. I don't know if this first line is necessary
            sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sb.AppendLine("# Visual Studio 15");

            foreach (var assembly in assemblies)
            {
                string typeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
                string projectGuid = GetDeterministicGuid(assembly.name);
                sb.AppendLine($"Project(\"{typeGuid}\") = \"{assembly.name}\", \"{assembly.name}.csproj\", \"{{{projectGuid}}}\"");
                sb.AppendLine("EndProject");
            }

            sb.AppendLine("Global");
            sb.AppendLine("    GlobalSection(SolutionConfigurationPlatforms) = preSolution");
            sb.AppendLine("        Debug|Any CPU = Debug|Any CPU");
            sb.AppendLine("        Release|Any CPU = Release|Any CPU");
            sb.AppendLine("    EndGlobalSection");
            sb.AppendLine("    GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var assembly in assemblies)
            {
                string projectGuid = GetDeterministicGuid(assembly.name);
                sb.AppendLine($"        {{{projectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                sb.AppendLine($"        {{{projectGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                sb.AppendLine($"        {{{projectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                sb.AppendLine($"        {{{projectGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU");
            }
            sb.AppendLine("    EndGlobalSection");
            sb.AppendLine("EndGlobal");

            File.WriteAllText(slnPath, sb.ToString());
        }

        internal void GenerateZedSettings()
        {
            string zedDir = Path.Combine(ProjectDirectory, ".zed");
            if (!Directory.Exists(zedDir))
            {
                Directory.CreateDirectory(zedDir);
            }

            string settingsPath = Path.Combine(zedDir, "settings.json");
            string[] requiredExclusions = {
                    "**/.*", "**/*~", "*.csproj", "*.sln", "*.slnx",
                    "**/*.meta", "**/*.dll", "**/*.asset", "**/*.prefab",
                    "**/*.unity", "Library/", "Temp/", "Logs/", "Obj/", "ProjectSettings/", "UIElementsSchema/",
                    "library/", "temp/", "logs/", "obj/", "projectsettings/", "uielelementsschema/", "Directory.Build.props" };

            if (File.Exists(settingsPath))
            {
                try
                {
                    string existingContent = File.ReadAllText(settingsPath);
                    JObject settings = JObject.Parse(existingContent);
                    bool isModified = false;

                    if (settings["lsp"] == null)
                    {
                        settings["lsp"] = new JObject();
                    }

                    if (settings["lsp"]["roslyn"] == null)
                    {
                        // some perfomance tuning on lsp options
                        settings["lsp"]["roslyn"] = JObject.Parse(@"{
                                ""initialization_options"": {
                                    ""automaticWorkspaceInit"": false,
                                    ""analyzeOpenDocumentsOnly"": true,
                                    ""enableRoslynAnalyzers"": true
                                }
                            }");
                        isModified = true;
                    }

                    if (settings["file_scan_exclusions"] == null)
                    {
                        settings["file_scan_exclusions"] = JArray.FromObject(requiredExclusions);
                        isModified = true;
                    }
                    else
                    {
                        JArray existingExclusions = (JArray)settings["file_scan_exclusions"];
                        foreach (string req in requiredExclusions)
                        {
                            if (!existingExclusions.Any(e => e.ToString() == req))
                            {
                                existingExclusions.Add(req);
                                isModified = true;
                            }
                        }
                    }

                    if (isModified)
                    {
                        File.WriteAllText(settingsPath, settings.ToString(Newtonsoft.Json.Formatting.Indented));
                        Debug.Log("[ZedEditor] Appended Unity requirements to existing .zed/settings.json");
                    }
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ZedEditor] Failed to merge settings, JSON might be malformed. Aborting merge to protect user data. Error: {ex.Message}");
                    return;
                }
            }

            string json = @"{
   ""lsp"": {
    ""roslyn"": {
      ""initialization_options"": {
        ""automaticWorkspaceInit"": false,
        ""enableImportCompletion"": true,
        ""analyzeOpenDocumentsOnly"": false,
        ""enableRoslynAnalyzers"": true,
      },
    },
  },
  ""file_scan_exclusions"": [
    ""**/.*"",
    ""**/*~"",
    ""*.csproj"",
    ""*.sln"",
    ""*.slnx"",
    ""**/*.meta"",
    ""**/*.booproj"",
    ""**/*.pibd"",
    ""**/*.suo"",
    ""**/*.user"",
    ""**/*.userprefs"",
    ""**/*.unityproj"",
    ""**/*.dll"",
    ""**/*.exe"",
    ""**/*.pdf"",
    ""**/*.mid"",
    ""**/*.midi"",
    ""**/*.wav"",
    ""**/*.gif"",
    ""**/*.ico"",
    ""**/*.jpg"",
    ""**/*.jpeg"",
    ""**/*.png"",
    ""**/*.psd"",
    ""**/*.tga"",
    ""**/*.tif"",
    ""**/*.tiff"",
    ""**/*.3ds"",
    ""**/*.3DS"",
    ""**/*.fbx"",
    ""**/*.FBX"",
    ""**/*.lxo"",
    ""**/*.LXO"",
    ""**/*.ma"",
    ""**/*.MA"",
    ""**/*.obj"",
    ""**/*.OBJ"",
    ""**/*.asset"",
    ""**/*.cubemap"",
    ""**/*.flare"",
    ""**/*.mat"",
    ""**/*.meta"",
    ""**/*.prefab"",
    ""**/*.unity"",
    ""Directory.Build.props"",
    ""build/"",
    ""Build/"",
    ""library/"",
    ""Library/"",
    ""obj/"",
    ""Obj/"",
    ""ProjectSettings/"",
    ""projectsettings/"",
    ""UserSettings/"",
    ""usersettings/"",
    ""UIElementsSchema/"",
    ""uielementsschema/"",
    ""temp/"",
    ""Temp/"",
    ""logs"",
    ""Logs"",
  ],
}";

            File.WriteAllText(settingsPath, json);
        }

        private void CleanupOrphanedProjects(List<string> activeProjects)
        {
            if (!Directory.Exists(ProjectDirectory)) return;
            var files = Directory.GetFiles(ProjectDirectory, "*.csproj");
            foreach (var file in files)
            {
                if (!activeProjects.Contains(Path.GetFileName(file)))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }

    }
}
