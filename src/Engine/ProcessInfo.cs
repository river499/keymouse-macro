using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KMMacro.Engine
{
    /// <summary>
    /// 进程权限探测。目标以管理员运行时，未提权的本工具注入的输入会被 Windows（UIPI）
    /// 静默丢弃——不报错、看起来"没有反应"。所以必须提前判断并明确告知用户。
    /// </summary>
    public static class ProcessInfo
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass,
            out int TokenInformation, int TokenInformationLength, out int ReturnLength);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        const uint TOKEN_QUERY = 0x0008;
        const int TokenElevation = 20;

        /// <summary>该进程是否以提升后的（管理员）令牌运行。</summary>
        public static bool IsElevated(int pid)
        {
            if (pid <= 0) return false;
            IntPtr proc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (proc == IntPtr.Zero) return false;
            try
            {
                IntPtr token;
                if (!OpenProcessToken(proc, TOKEN_QUERY, out token)) return false;
                try
                {
                    int elevation;
                    int returned;
                    if (!GetTokenInformation(token, TokenElevation, out elevation, 4, out returned)) return false;
                    return elevation != 0;
                }
                finally
                {
                    CloseHandle(token);
                }
            }
            finally
            {
                CloseHandle(proc);
            }
        }

        /// <summary>本进程是否已提权。</summary>
        public static bool IsSelfElevated()
        {
            return IsElevated(Process.GetCurrentProcess().Id);
        }

        /// <summary>
        /// 目标窗口所属进程是否比本工具权限更高。若是，输入一定会被系统丢弃，必须提权后重试。
        /// </summary>
        public static bool TargetBlocksInput(IntPtr hwnd, out int targetPid)
        {
            targetPid = 0;
            if (hwnd == IntPtr.Zero) return false;
            uint pid;
            Win32.GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0) return false;
            targetPid = (int)pid;
            if (IsSelfElevated()) return false;
            return IsElevated(targetPid);
        }
    }
}
