using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Media_DL.Misc
{
    public static  class Help
    {
        public static class Paths
        {
            public static string Roaming { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            public static string Documents { get; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            public static string ProgramData { get; } = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            public static string AppDataLocal { get; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            public static string ProgramFilesX86 { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        }

        public static class Processes
        {
            public static void KillProcess(string name)
            {
                foreach (Process p in Process.GetProcessesByName(name))
                {
                    p.Kill();
                    p.WaitForExit();
                }
            }
        }

        public static class Directories
        {
            public static void ExploreFilesRecursively(string Path, Action<string> callback)
            {
                if (Directory.Exists(Path) == false)
                    return;

                DirectoryInfo info = new DirectoryInfo(Path);

                foreach (var file in info.GetFiles())
                {
                    if (file.Exists == false)
                        continue;

                    callback(file.FullName);
                }

                foreach (var folder in info.GetDirectories())
                {
                    if (folder.Exists == false)
                        continue;

                    ExploreFilesRecursively(folder.FullName, callback);
                }
            }
        }
    }
}
