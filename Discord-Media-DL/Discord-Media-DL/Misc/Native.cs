using System;
using System.Runtime.InteropServices;

namespace Discord_Media_DL.Misc
{
    public static class Native
    {
        [DllImport("dbghelp.dll", SetLastError = true)]
        public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeHandle hFile, int DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);
    }
}
