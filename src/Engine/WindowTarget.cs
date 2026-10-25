using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace KMMacro.Engine
{
    public sealed class WindowInfo
    {
        public IntPtr Handle;
        public string Title = string.Empty;
        public string ClassName = string.Empty;
        public int ProcessId;

        public override string ToString()
        {
            string t = string.IsNullOrEmpty(Title) ? "(无标题)" : Title;
            if (t.Length > 58) t = t.Substring(0, 58) + "…";
            return string.Format("{0}    [{1}]  pid={2}", t, ClassName, ProcessId);
        }
    }

    public static class WindowTarget
    {
        public static List<WindowInfo> Enumerate()
        {
            List<WindowInfo> list = new List<WindowInfo>();
            Win32.EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                if (!Win32.IsWindowVisible(h)) return true;
                string title = Win32.GetWindowTitle(h);
                if (title.Length == 0) return true;
                uint pid;
                Win32.GetWindowThreadProcessId(h, out pid);
                WindowInfo wi = new WindowInfo();
                wi.Handle = h;
                wi.Title = title;
                wi.ClassName = Win32.GetWindowClass(h);
                wi.ProcessId = (int)pid;
                list.Add(wi);
                return true;
            }, IntPtr.Zero);
            list.Sort(delegate(WindowInfo a, WindowInfo b)
            {
                return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        public static WindowInfo FromHandle(IntPtr hwnd)
        {
            WindowInfo wi = new WindowInfo();
            wi.Handle = hwnd;
            wi.Title = Win32.GetWindowTitle(hwnd);
            wi.ClassName = Win32.GetWindowClass(hwnd);
            uint pid;
            Win32.GetWindowThreadProcessId(hwnd, out pid);
            wi.ProcessId = (int)pid;
            return wi;
        }

        /// <summary>取光标下最顶层窗口（根窗口），用于「从光标拾取」。</summary>
        public static WindowInfo FromCursor()
        {
            Win32.POINT pt;
            if (!Win32.GetCursorPos(out pt)) return null;
            IntPtr h = Win32.WindowFromPoint(pt);
            if (h == IntPtr.Zero) return null;
            IntPtr root = Win32.GetAncestor(h, Win32.GA_ROOT);
            if (root == IntPtr.Zero) root = h;
            WindowInfo wi = FromHandle(root);
            if (string.IsNullOrEmpty(wi.Title) && string.IsNullOrEmpty(wi.ClassName)) return null;
            return wi;
        }

        /// <summary>按标题 + 类名找回窗口（方案加载后重新绑定用）。</summary>
        public static IntPtr FindByTitle(string title, string className)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(className)) return IntPtr.Zero;
            IntPtr found = IntPtr.Zero;
            Win32.EnumWindows(delegate(IntPtr h, IntPtr l)
            {
                if (!Win32.IsWindowVisible(h)) return true;
                string t = Win32.GetWindowTitle(h);
                string c = Win32.GetWindowClass(h);
                bool titleOk = string.IsNullOrEmpty(title) || t == title;
                bool classOk = string.IsNullOrEmpty(className) || c == className;
                if (titleOk && classOk)
                {
                    found = h;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        public static bool IsAlive(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && Win32.IsWindow(hwnd);
        }

        public static bool IsForeground(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && Win32.GetForegroundWindow() == hwnd;
        }

        /// <summary>目标窗口消息队列是否响应（WM_NULL 探测，500ms 超时）。</summary>
        public static bool IsResponsive(IntPtr hwnd, out int elapsedMs)
        {
            elapsedMs = -1;
            if (!IsAlive(hwnd)) return false;
            Stopwatch sw = Stopwatch.StartNew();
            IntPtr result;
            IntPtr ok = Win32.SendMessageTimeoutW(hwnd, Win32.WM_NULL, IntPtr.Zero, IntPtr.Zero,
                Win32.SMTO_ABORTIFHUNG | Win32.SMTO_BLOCK, 500, out result);
            sw.Stop();
            elapsedMs = (int)sw.ElapsedMilliseconds;
            return ok != IntPtr.Zero;
        }

        /// <summary>
        /// 把目标窗口拉到前台。三级兜底：SetForegroundWindow → AttachThreadInput → Alt 抖动。
        /// trace 返回实际走到了哪一级，便于日志如实呈现，绝不谎报成功。
        /// </summary>
        public static bool Activate(IntPtr hwnd, out string trace)
        {
            if (hwnd == IntPtr.Zero)
            {
                trace = "未绑定目标窗口";
                return false;
            }
            if (!Win32.IsWindow(hwnd))
            {
                trace = "目标窗口已关闭";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            if (Win32.IsIconic(hwnd))
            {
                Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
                Thread.Sleep(80);
                sb.Append("解除最小化 → ");
            }

            if (Win32.GetForegroundWindow() == hwnd)
            {
                trace = sb.ToString() + "已在前台";
                return true;
            }

            Win32.SetForegroundWindow(hwnd);
            Thread.Sleep(50);
            if (Win32.GetForegroundWindow() == hwnd)
            {
                trace = sb.ToString() + "SetForegroundWindow 成功";
                return true;
            }
            sb.Append("SetForegroundWindow 被前台锁拒绝 → ");

            uint pid;
            uint targetThread = Win32.GetWindowThreadProcessId(hwnd, out pid);
            uint myThread = Win32.GetCurrentThreadId();
            bool attached = false;
            if (targetThread != 0 && targetThread != myThread)
                attached = Win32.AttachThreadInput(myThread, targetThread, true);
            try
            {
                Win32.BringWindowToTop(hwnd);
                Win32.SetForegroundWindow(hwnd);
                Win32.SetFocus(hwnd);
            }
            finally
            {
                if (attached) Win32.AttachThreadInput(myThread, targetThread, false);
            }
            Thread.Sleep(60);
            if (Win32.GetForegroundWindow() == hwnd)
            {
                trace = sb.ToString() + "AttachThreadInput 成功";
                return true;
            }
            sb.Append("AttachThreadInput 失败 → ");

            // Alt 抖动：局部模拟一次修饰键，解开前台锁的“用户输入”判定
            Win32.keybd_event(0x12, 0, 0, UIntPtr.Zero);
            Thread.Sleep(20);
            Win32.keybd_event(0x12, 0, Win32.KEYEVENTF_KEYUP, UIntPtr.Zero);
            Win32.SetForegroundWindow(hwnd);
            Thread.Sleep(150);
            if (Win32.GetForegroundWindow() == hwnd)
            {
                trace = sb.ToString() + "Alt 抖动绕过前台锁";
                return true;
            }

            trace = sb.ToString() + "全部手段失败（目标可能以更高权限运行，请用管理员身份重启本工具）";
            return false;
        }

        public static string Describe(IntPtr hwnd)
        {
            if (!IsAlive(hwnd)) return "未绑定 / 窗口已关闭";
            WindowInfo wi = FromHandle(hwnd);
            string state = Win32.IsIconic(hwnd) ? "已最小化" : (IsForeground(hwnd) ? "前台" : "后台");
            return string.Format("{0}  [{1}]  0x{2:X}  {3}", wi.Title, wi.ClassName,
                hwnd.ToInt64(), state);
        }
    }

    /// <summary>
    /// 保活：把窗口移出屏幕而不是最小化。最小化会让部分程序挂起渲染与输入处理，
    /// 移出屏幕的窗口仍在正常消息循环里，所以后台投递成功率更高。停止时还原原位置。
    /// </summary>
    public sealed class WindowMover
    {
        const int OffscreenX = -32000;
        readonly Dictionary<IntPtr, Win32.RECT> _saved = new Dictionary<IntPtr, Win32.RECT>();

        public int MovedCount { get { return _saved.Count; } }

        public bool MoveOffscreen(IntPtr hwnd)
        {
            if (!Win32.IsWindow(hwnd) || _saved.ContainsKey(hwnd)) return false;
            if (Win32.IsIconic(hwnd))
            {
                Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
                Thread.Sleep(100);
            }
            Win32.RECT r;
            if (!Win32.GetWindowRect(hwnd, out r)) return false;
            if (r.Width <= 0 || r.Height <= 0) return false;
            _saved[hwnd] = r;
            int y = r.Top > OffscreenX + 1000 ? r.Top : 100;
            return Win32.SetWindowPos(hwnd, IntPtr.Zero, OffscreenX, y, r.Width, r.Height,
                Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
        }

        public void RestoreAll()
        {
            foreach (KeyValuePair<IntPtr, Win32.RECT> kv in _saved)
            {
                if (!Win32.IsWindow(kv.Key)) continue;
                Win32.RECT r = kv.Value;
                Win32.SetWindowPos(kv.Key, IntPtr.Zero, r.Left, r.Top, r.Width, r.Height,
                    Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
            }
            _saved.Clear();
        }
    }
}
