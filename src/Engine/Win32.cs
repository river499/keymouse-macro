using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KMMacro.Engine
{
    /// <summary>所有 P/Invoke 声明集中于此，便于审计与维护。</summary>
    public static class Win32
    {
        // ---------------- 窗口消息 ----------------
        public const int WM_NULL = 0x0000;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_LBUTTONDBLCLK = 0x0203;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_RBUTTONDBLCLK = 0x0206;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;
        public const int WM_HOTKEY = 0x0312;

        // ---------------- 鼠标按键状态位 ----------------
        public const int MK_LBUTTON = 0x0001;
        public const int MK_RBUTTON = 0x0002;
        public const int MK_SHIFT = 0x0004;
        public const int MK_CONTROL = 0x0008;
        public const int MK_MBUTTON = 0x0010;
        public const int MK_XBUTTON1 = 0x0020;
        public const int MK_XBUTTON2 = 0x0040;

        // ---------------- SendInput ----------------
        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;

        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_UNICODE = 0x0004;
        public const uint KEYEVENTF_SCANCODE = 0x0008;

        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;

        // ---------------- ShowWindow / SetWindowPos ----------------
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE = 9;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;

        // ---------------- 其它常量 ----------------
        public const uint CWP_SKIPINVISIBLE = 0x0001;
        public const uint CWP_SKIPDISABLED = 0x0002;
        public const uint CWP_SKIPTRANSPARENT = 0x0004;

        public const uint GA_ROOT = 2;
        public const uint SMTO_BLOCK = 0x0001;
        public const uint SMTO_ABORTIFHUNG = 0x0002;
        public const uint MAPVK_VK_TO_VSC = 0;

        public static readonly IntPtr HWND_TOP = IntPtr.Zero;

        // ---------------- 结构体 ----------------
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
            public override string ToString() { return X + "," + Y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
            public override string ToString()
            {
                return string.Format("({0},{1})-({2},{3})", Left, Top, Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // ---------------- user32 ----------------
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT p);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT p);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessageTimeoutW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hWnd, POINT pt, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref POINT lpPoints, int cPoints);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKeyW(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        // ---------------- 辅助 ----------------
        public static int MakeLParam(int lo, int hi)
        {
            return unchecked((hi << 16) | (lo & 0xFFFF));
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return string.Empty;
            StringBuilder sb = new StringBuilder(512);
            GetWindowTextW(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetWindowClass(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return string.Empty;
            StringBuilder sb = new StringBuilder(256);
            GetClassNameW(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static bool IsExtendedKey(int vk)
        {
            switch (vk)
            {
                case 0x21: // PRIOR
                case 0x22: // NEXT
                case 0x23: // END
                case 0x24: // HOME
                case 0x25: // LEFT
                case 0x26: // UP
                case 0x27: // RIGHT
                case 0x28: // DOWN
                case 0x2D: // INSERT
                case 0x2E: // DELETE
                case 0x5B: // LWIN
                case 0x5C: // RWIN
                case 0x5D: // APPS
                case 0x6F: // DIVIDE
                case 0x90: // NUMLOCK
                case 0xA3: // RCONTROL
                case 0xA5: // RMENU
                    return true;
                default:
                    return false;
            }
        }
    }
}
