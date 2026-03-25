using System;
using System.IO;
#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

namespace HFS.ZedEditor
{
    public static class ZedPathLocator
    {
        public static string GetPath()
        {
#if UNITY_EDITOR_WIN
            return FindWindowsPath();
#elif UNITY_EDITOR_OSX
            return FindMacPath();
#else
            return FindLinuxPath();
#endif
        }

#if UNITY_EDITOR_WIN
        private static string FindWindowsPath()
        {
            try
            {
                // 1. Check Registry (URI Scheme)
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\zed\DefaultIcon"))
                {
                    if (key?.GetValue("") is string regPath)
                    {
                        string cleanPath = regPath.Split(',')[0].Trim('"');
                        if (File.Exists(cleanPath))
                        {
                            return cleanPath;
                        }
                    }
                }
            }
            catch { /* Registry access failed */ }

            // 2. Check Local AppData (Standard Install)
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "zed", "bin", "zed.exe");
            if (File.Exists(appData))
            {
                return appData;
            }

            return "zed.exe"; // Fallback to PATH
        }
#endif

#if UNITY_EDITOR_OSX
        private static string FindMacPath()
        {
            const string standardPath = "/Applications/Zed.app/Contents/MacOS/zed";
            if (File.Exists(standardPath)) return standardPath;

            // Check user-level Applications folder
            string userPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Applications/Zed.app/Contents/MacOS/zed");
            if (File.Exists(userPath))
            {
                return userPath;
            }

            return "zed"; // Fallback to PATH
        }
#endif

#if UNITY_EDITOR_LINUX
        private static string FindLinuxPath()
        {
            // Use 'which' to find the binary location
            try
            {
                using (var process = Process.Start(new ProcessStartInfo("which", "zed")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }))
                {
                    string path = process.StandardOutput.ReadToEnd().Trim();
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            catch { /* which command failed */ }

            return "zed"; // Fallback to PATH
        }
#endif
    }
}
