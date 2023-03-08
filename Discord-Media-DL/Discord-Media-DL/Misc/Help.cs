using System;
using System.Diagnostics;
using System.IO;

namespace Discord_Media_DL.Misc
{
    public static class Help
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

                DirectoryInfo info = new(Path);

                foreach (FileInfo file in info.GetFiles())
                {
                    if (file.Exists == false)
                        continue;

                    callback(file.FullName);
                }

                foreach (DirectoryInfo folder in info.GetDirectories())
                {
                    if (folder.Exists == false)
                        continue;

                    ExploreFilesRecursively(folder.FullName, callback);
                }
            }
        }

        public static string DumpProcessAsString(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return "";

            try
            {
                var result = "";

                Process proc = null;

                foreach (Process p in Process.GetProcessesByName(processName))
                {
                    if (p == null || p.HasExited == true || p.Handle == IntPtr.Zero)
                        continue;

                    proc = p;
                    break;
                }

                if (proc == null)
                    return "";

                var dumpContainer = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                try
                {
                    if (File.Exists(dumpContainer) == true)
                        File.Delete(dumpContainer);

                    using (FileStream fs = File.Create(dumpContainer))
                    {
                        Native.MiniDumpWriteDump(proc.Handle, (uint)proc.Id, fs.SafeFileHandle, 0x2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                    }

                    result = File.ReadAllText(dumpContainer);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    try
                    {
                        File.Delete(dumpContainer);
                    }
                    catch { }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return "";
        }
    }
}
