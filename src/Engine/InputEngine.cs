using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KMMacro.Engine
{
    public enum EngineKind { Foreground = 0, Background = 1, KeepAlive = 2 }
    public enum MouseButtonKind { Left = 0, Right = 1, Middle = 2, X1 = 3, X2 = 4 }
    public enum MouseActionKind { Click = 0, DoubleClick = 1, Down = 2, Up = 3 }

    /// <summary>
    /// 输入原语。前台部分走 SendInput（真实硬件级事件），后台部分走 PostMessage（不抢焦点）。
    /// 本类型不依赖 WinForms，可被测试程序直接复用。
    /// </summary>
    public static class InputEngine
    {
        static readonly int InputSize = Marshal.SizeOf(typeof(Win32.INPUT));

        static void Send(Win32.INPUT[] inputs)
        {
            uint sent = Win32.SendInput((uint)inputs.Length, inputs, InputSize);
            if (sent == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput 调用失败");
        }

        // ==================== 前台：硬件级模拟 ====================

        public static void ForegroundMove(int x, int y)
        {
            Win32.SetCursorPos(x, y);
        }

        public static void ForegroundMouse(MouseButtonKind button, bool down)
        {
            uint flag;
            uint data = 0;
            switch (button)
            {
                case MouseButtonKind.Left:
                    flag = down ? Win32.MOUSEEVENTF_LEFTDOWN : Win32.MOUSEEVENTF_LEFTUP;
                    break;
                case MouseButtonKind.Right:
                    flag = down ? Win32.MOUSEEVENTF_RIGHTDOWN : Win32.MOUSEEVENTF_RIGHTUP;
                    break;
                case MouseButtonKind.Middle:
                    flag = down ? Win32.MOUSEEVENTF_MIDDLEDOWN : Win32.MOUSEEVENTF_MIDDLEUP;
                    break;
                case MouseButtonKind.X1:
                    flag = down ? Win32.MOUSEEVENTF_XDOWN : Win32.MOUSEEVENTF_XUP;
                    data = 1;
                    break;
                default:
                    flag = down ? Win32.MOUSEEVENTF_XDOWN : Win32.MOUSEEVENTF_XUP;
                    data = 2;
                    break;
            }
            Win32.INPUT[] inp = new Win32.INPUT[1];
            inp[0].type = Win32.INPUT_MOUSE;
            inp[0].u.mi.dwFlags = flag;
            inp[0].u.mi.mouseData = data;
            Send(inp);
        }

        public static void ForegroundWheel(int notches)
        {
            Win32.INPUT[] inp = new Win32.INPUT[1];
            inp[0].type = Win32.INPUT_MOUSE;
            inp[0].u.mi.dwFlags = Win32.MOUSEEVENTF_WHEEL;
            inp[0].u.mi.mouseData = unchecked((uint)(notches * 120));
            Send(inp);
        }

        public static void ForegroundKey(int vk, bool down)
        {
            Win32.INPUT[] inp = new Win32.INPUT[1];
            inp[0].type = Win32.INPUT_KEYBOARD;
            inp[0].u.ki.wVk = (ushort)vk;
            inp[0].u.ki.dwFlags = down ? 0u : Win32.KEYEVENTF_KEYUP;
            if (Win32.IsExtendedKey(vk))
                inp[0].u.ki.dwFlags |= Win32.KEYEVENTF_EXTENDEDKEY;
            Send(inp);
        }

        /// <summary>用 UNICODE 事件直接输入文本，绕过输入法与键盘布局。</summary>
        public static void ForegroundText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Win32.INPUT[] arr = new Win32.INPUT[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                arr[i * 2].type = Win32.INPUT_KEYBOARD;
                arr[i * 2].u.ki.wVk = 0;
                arr[i * 2].u.ki.wScan = text[i];
                arr[i * 2].u.ki.dwFlags = Win32.KEYEVENTF_UNICODE;

                arr[i * 2 + 1].type = Win32.INPUT_KEYBOARD;
                arr[i * 2 + 1].u.ki.wVk = 0;
                arr[i * 2 + 1].u.ki.wScan = text[i];
                arr[i * 2 + 1].u.ki.dwFlags = Win32.KEYEVENTF_UNICODE | Win32.KEYEVENTF_KEYUP;
            }
            Send(arr);
        }

        // ==================== 后台：消息投递 ====================

        /// <summary>从 root 逐层下钻到坐标处最深的子窗口，并把坐标换算到该子窗口的客户区。</summary>
        public static IntPtr DeepestChild(IntPtr root, int clientX, int clientY, out int outX, out int outY)
        {
            IntPtr cur = root;
            int cx = clientX;
            int cy = clientY;
            for (int depth = 0; depth < 8; depth++)
            {
                Win32.POINT probe = new Win32.POINT(cx, cy);
                IntPtr child = Win32.ChildWindowFromPointEx(cur, probe,
                    Win32.CWP_SKIPINVISIBLE | Win32.CWP_SKIPDISABLED);
                if (child == IntPtr.Zero || child == cur) break;
                Win32.POINT mapped = new Win32.POINT(cx, cy);
                Win32.MapWindowPoints(cur, child, ref mapped, 1);
                cx = mapped.X;
                cy = mapped.Y;
                cur = child;
            }
            outX = cx;
            outY = cy;
            return cur;
        }

        public static bool PostMove(IntPtr hwnd, int x, int y)
        {
            return Win32.PostMessageW(hwnd, Win32.WM_MOUSEMOVE, IntPtr.Zero,
                (IntPtr)Win32.MakeLParam(x, y));
        }

        public static bool PostMouse(IntPtr hwnd, MouseButtonKind button, bool down, int x, int y)
        {
            int msg;
            int wp = 0;
            switch (button)
            {
                case MouseButtonKind.Left:
                    msg = down ? Win32.WM_LBUTTONDOWN : Win32.WM_LBUTTONUP;
                    wp = down ? Win32.MK_LBUTTON : 0;
                    break;
                case MouseButtonKind.Right:
                    msg = down ? Win32.WM_RBUTTONDOWN : Win32.WM_RBUTTONUP;
                    wp = down ? Win32.MK_RBUTTON : 0;
                    break;
                case MouseButtonKind.Middle:
                    msg = down ? Win32.WM_MBUTTONDOWN : Win32.WM_MBUTTONUP;
                    wp = down ? Win32.MK_MBUTTON : 0;
                    break;
                case MouseButtonKind.X1:
                    msg = down ? Win32.WM_XBUTTONDOWN : Win32.WM_XBUTTONUP;
                    wp = (1 << 16) | (down ? Win32.MK_XBUTTON1 : 0);
                    break;
                default:
                    msg = down ? Win32.WM_XBUTTONDOWN : Win32.WM_XBUTTONUP;
                    wp = (2 << 16) | (down ? Win32.MK_XBUTTON2 : 0);
                    break;
            }
            return Win32.PostMessageW(hwnd, msg, (IntPtr)wp, (IntPtr)Win32.MakeLParam(x, y));
        }

        /// <summary>滚轮消息的坐标必须是屏幕坐标，delta 放在 wParam 高字。</summary>
        public static bool PostWheel(IntPtr hwnd, int notches, int screenX, int screenY)
        {
            int delta = notches * 120;
            int wp = unchecked(delta << 16);
            return Win32.PostMessageW(hwnd, Win32.WM_MOUSEWHEEL, (IntPtr)wp,
                (IntPtr)Win32.MakeLParam(screenX, screenY));
        }

        public static bool PostKey(IntPtr hwnd, int vk, bool down)
        {
            int lp = KeyLParam(vk, down);
            return Win32.PostMessageW(hwnd, down ? Win32.WM_KEYDOWN : Win32.WM_KEYUP,
                (IntPtr)vk, (IntPtr)lp);
        }

        public static bool PostChar(IntPtr hwnd, char ch)
        {
            int lp = 1 | ((int)(Win32.MapVirtualKeyW(ch, Win32.MAPVK_VK_TO_VSC) & 0xFF) << 16);
            return Win32.PostMessageW(hwnd, Win32.WM_CHAR, (IntPtr)(int)ch, (IntPtr)lp);
        }

        /// <summary>构造合理的键盘 lParam：重复计数、扫描码、扩展键标志、前一状态、转换状态。</summary>
        public static int KeyLParam(int vk, bool down)
        {
            uint scan = Win32.MapVirtualKeyW((uint)vk, Win32.MAPVK_VK_TO_VSC);
            int lp = 1 | ((int)(scan & 0xFF) << 16);
            if (Win32.IsExtendedKey(vk)) lp |= (1 << 24);
            if (!down) lp |= (1 << 30) | (1 << 31);
            return lp;
        }
    }
}
