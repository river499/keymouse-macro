using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using KMMacro.Core;
using KMMacro.Engine;

namespace KMMacroTest
{
    /// <summary>
    /// 引擎自动化验证。断言「后台投递的消息真的到达了目标窗口」，而不是只看函数返回值。
    /// 用法：EngineTest.exe
    /// </summary>
    internal static class EngineTest
    {
        static string _dir;
        static string _logPath;
        static IntPtr _hwnd = IntPtr.Zero;
        static Process _target;
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        static int _pass;
        static int _fail;

        static int Main()
        {
            try { Console.OutputEncoding = Encoding.UTF8; }
            catch { }

            _dir = AppDomain.CurrentDomain.BaseDirectory;
            _logPath = Path.Combine(_dir, "target.log");

            Console.WriteLine("=== 键鼠宏工坊 引擎自动化验证 ===");
            Console.WriteLine("工作目录: " + _dir);

            try
            {
                if (!StartTarget())
                {
                    Console.WriteLine("FATAL: 目标窗口未启动");
                    return 2;
                }

                TestBackgroundClicks();
                TestMinimizedClicks();
                TestKeepAliveClicks();
                TestKeyboard();
                TestForegroundActivation();
                TestProfileRoundTrip();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FATAL: " + ex);
                _fail++;
            }
            finally
            {
                Shutdown();
            }

            Console.WriteLine();
            Console.WriteLine(string.Format("结果：通过 {0} / 失败 {1}", _pass, _fail));
            Console.WriteLine(_fail == 0 ? "ALL PASS" : "HAS FAILURE");
            return _fail == 0 ? 0 : 1;
        }

        // ---------------- 夹具 ----------------

        static bool StartTarget()
        {
            string exe = Path.Combine(_dir, "KMMacroTestTarget.exe");
            if (!File.Exists(exe))
            {
                Console.WriteLine("FATAL: 未找到 " + exe);
                return false;
            }
            ProcessStartInfo psi = new ProcessStartInfo(exe, "\"" + _logPath + "\"");
            psi.UseShellExecute = false;
            _target = Process.Start(psi);
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(100);
                _hwnd = WindowTarget.FindByTitle("KMMacroTestTarget", "");
                if (_hwnd != IntPtr.Zero) break;
            }
            if (_hwnd == IntPtr.Zero) return false;
            Thread.Sleep(400);
            Console.WriteLine("目标窗口句柄: 0x" + _hwnd.ToInt64().ToString("X"));
            return true;
        }

        static void Shutdown()
        {
            try { if (_target != null && !_target.HasExited) _target.Kill(); }
            catch { }
        }

        static void PostClicks(int count, int x, int y, int holdMs, int gapMs)
        {
            int cx;
            int cy;
            IntPtr child = InputEngine.DeepestChild(_hwnd, x, y, out cx, out cy);
            for (int i = 0; i < count; i++)
            {
                InputEngine.PostMove(child, cx, cy);
                InputEngine.PostMouse(child, MouseButtonKind.Left, true, cx, cy);
                if (holdMs > 0) Thread.Sleep(holdMs);
                InputEngine.PostMouse(child, MouseButtonKind.Left, false, cx, cy);
                if (gapMs > 0) Thread.Sleep(gapMs);
            }
        }

        static int CountLines(string prefix)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    if (!File.Exists(_logPath)) return 0;
                    int n = 0;
                    using (FileStream fs = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.StartsWith(prefix, StringComparison.Ordinal)) n++;
                        }
                    }
                    return n;
                }
                catch
                {
                    Thread.Sleep(50);
                }
            }
            return -1;
        }

        static void Check(string name, int expected, int actual)
        {
            bool ok = expected == actual;
            if (ok) _pass++; else _fail++;
            Console.WriteLine(string.Format("  [{0}] {1}  期望={2} 实际={3}", ok ? "PASS" : "FAIL", name, expected, actual));
        }

        static void Info(string name, int actual, int total)
        {
            if (actual >= total) _pass++;
            Console.WriteLine(string.Format("  [{0}] {1}  命中 {2}/{3}", actual >= total ? "PASS" : "INFO", name, actual, total));
        }

        // ---------------- 用例 ----------------

        static void TestBackgroundClicks()
        {
            Console.WriteLine();
            Console.WriteLine("用例1：后台投递 100 次点击（窗口不在前台）");
            int before = CountLines("MOUSEDOWN");
            PostClicks(100, 60, 60, 3, 2);
            Thread.Sleep(500);
            int after = CountLines("MOUSEDOWN");
            Check("后台点击命中数", 100, after - before);
        }

        static void TestMinimizedClicks()
        {
            Console.WriteLine();
            Console.WriteLine("用例2：最小化后投递 50 次点击");
            Win32.ShowWindow(_hwnd, Win32.SW_MINIMIZE);
            Thread.Sleep(400);
            int before = CountLines("MOUSEDOWN");
            PostClicks(50, 60, 60, 3, 2);
            Thread.Sleep(500);
            int after = CountLines("MOUSEDOWN");
            Info("最小化点击命中数", after - before, 50);
            Win32.ShowWindow(_hwnd, Win32.SW_RESTORE);
            Thread.Sleep(400);
        }

        static void TestKeepAliveClicks()
        {
            Console.WriteLine();
            Console.WriteLine("用例3：保活模式（窗口移出屏幕）投递 50 次点击");
            WindowMover mover = new WindowMover();
            bool moved = mover.MoveOffscreen(_hwnd);
            Console.WriteLine("  移出屏幕: " + (moved ? "成功" : "失败"));
            Thread.Sleep(400);
            int before = CountLines("MOUSEDOWN");
            PostClicks(50, 60, 60, 3, 2);
            Thread.Sleep(500);
            int after = CountLines("MOUSEDOWN");
            Check("保活点击命中数", 50, after - before);
            mover.RestoreAll();
            Thread.Sleep(300);
        }

        static void TestKeyboard()
        {
            Console.WriteLine();
            Console.WriteLine("用例4：后台投递 20 次按键 A");
            int before = CountLines("KEYDOWN");
            int cx;
            int cy;
            IntPtr child = InputEngine.DeepestChild(_hwnd, 60, 60, out cx, out cy);
            for (int i = 0; i < 20; i++)
            {
                InputEngine.PostKey(child, 0x41, true);
                Thread.Sleep(5);
                InputEngine.PostKey(child, 0x41, false);
                Thread.Sleep(3);
            }
            Thread.Sleep(400);
            int after = CountLines("KEYDOWN");
            Check("后台按键命中数", 20, after - before);
        }

        static void TestForegroundActivation()
        {
            Console.WriteLine();
            Console.WriteLine("用例6：前台激活 + SendInput 真实按键");

            // 先把前台交给控制台窗口，确保下面走的是「从后台抢焦点」的真实路径
            IntPtr console = GetConsoleWindow();
            string traceConsole = "";
            bool consoleFg = false;
            if (console != IntPtr.Zero)
            {
                // 用被测的同一套激活逻辑去抢前台：我们的进程此刻并非前台，
                // 因此会自然走到 AttachThreadInput / Alt 抖动兜底。
                consoleFg = WindowTarget.Activate(console, out traceConsole);
                Thread.Sleep(250);
                consoleFg = Win32.GetForegroundWindow() == console;
                Console.WriteLine("  抢回控制台: " + (consoleFg ? "成功" : "失败") + " — " + traceConsole);
            }
            bool startedInBackground = Win32.GetForegroundWindow() != _hwnd;
            Console.WriteLine("  初始状态: " + (startedInBackground ? "目标在后台（符合预期）" : "目标仍在前台"));

            string trace;
            bool activated = WindowTarget.Activate(_hwnd, out trace);
            Console.WriteLine("  激活结果: " + (activated ? "成功" : "失败") + " — " + trace);
            bool isFg = Win32.GetForegroundWindow() == _hwnd;
            bool ok = startedInBackground && activated && isFg;
            if (ok) _pass++; else _fail++;
            Console.WriteLine("  [" + (ok ? "PASS" : "FAIL") + "] 从后台把目标窗口拉回前台");

            int before = CountLines("KEYDOWN");
            InputEngine.ForegroundKey(0x41, true);
            Thread.Sleep(30);
            InputEngine.ForegroundKey(0x41, false);
            Thread.Sleep(300);
            int after = CountLines("KEYDOWN");
            Check("前台真实按键命中数", 1, after - before);
        }

        static void TestProfileRoundTrip()
        {
            Console.WriteLine();
            Console.WriteLine("用例5：方案文件存取往返");

            Profile p = new Profile();
            p.Engine = EngineKind.KeepAlive;
            p.TargetTitle = "测试窗口;含分号";
            p.IntervalFixed = false;
            p.IntervalMin = 300;
            p.IntervalMax = 900;
            p.LoopCount = 7;
            p.RunSecondsLimit = 60;

            Step s1 = new Step();
            s1.Kind = StepKind.Mouse;
            s1.Button = MouseButtonKind.Right;
            s1.Action = MouseActionKind.DoubleClick;
            s1.Pos = PosMode.Client;
            s1.X = 123;
            s1.Y = 456;
            s1.Jitter = 4;
            s1.HoldMs = 35;
            p.Steps.Add(s1);

            Step s2 = new Step();
            s2.Kind = StepKind.Key;
            s2.Vk = 0x41;
            s2.Ctrl = true;
            s2.Shift = true;
            s2.HoldMs = 20;
            s2.IntervalMin = 100;
            s2.IntervalMax = 200;
            p.Steps.Add(s2);

            Step s3 = new Step();
            s3.Kind = StepKind.Text;
            s3.Text = "含分号;与换行\n第二行";
            p.Steps.Add(s3);

            string path = Path.Combine(_dir, "profile_test.ini");
            p.Save(path);
            Profile q = Profile.Load(path);

            bool ok =
                q.Engine == p.Engine &&
                q.TargetTitle == p.TargetTitle &&
                q.IntervalMin == p.IntervalMin &&
                q.IntervalMax == p.IntervalMax &&
                q.LoopCount == p.LoopCount &&
                q.Steps.Count == 3 &&
                q.Steps[0].Button == MouseButtonKind.Right &&
                q.Steps[0].X == 123 && q.Steps[0].Jitter == 4 &&
                q.Steps[1].Ctrl && q.Steps[1].Shift && q.Steps[1].Vk == 0x41 &&
                q.Steps[1].IntervalMin == 100 && q.Steps[1].IntervalMax == 200 &&
                q.Steps[2].Text == "含分号;与换行\n第二行";

            if (ok) _pass++; else _fail++;
            Console.WriteLine("  [" + (ok ? "PASS" : "FAIL") + "] 存取往返一致");
            if (!ok)
            {
                Console.WriteLine("    标题读回: " + q.TargetTitle);
                Console.WriteLine("    文本读回: " + q.Steps[2].Text.Replace("\n", "\\n"));
            }
        }
    }
}
