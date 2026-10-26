using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KMMacro.Engine;

namespace KMMacro.Core
{
    /// <summary>
    /// 宏调度线程：与界面完全解耦，界面只发指令、收事件。
    /// 负责随机间隔、按住时长、循环次数、运行时长上限、暂停/停止、保活窗口还原。
    /// </summary>
    public sealed class Runner : IDisposable
    {
        public event Action<string> Log;
        public event Action<string> StateChanged;
        public event Action<long, TimeSpan> Progress;

        readonly ManualResetEventSlim _stopEvent = new ManualResetEventSlim(false);
        readonly ManualResetEventSlim _pauseGate = new ManualResetEventSlim(true);
        readonly Random _rnd = new Random();
        readonly WindowMover _mover = new WindowMover();
        readonly Stopwatch _sw = new Stopwatch();

        Thread _thread;
        Profile _profile;
        Step _fallback;
        IntPtr _hwnd;
        long _count;
        long _lastReportMs;
        int _diagClicks;

        public bool IsRunning
        {
            get { Thread t = _thread; return t != null && t.IsAlive; }
        }

        public bool IsPaused { get { return !_pauseGate.IsSet; } }

        public long Count { get { return Interlocked.Read(ref _count); } }

        public TimeSpan Elapsed { get { return _sw.Elapsed; } }

        public void Start(Profile profile, Step fallback, IntPtr hwnd)
        {
            if (IsRunning) return;
            if (profile == null) throw new ArgumentNullException("profile");
            _profile = profile.Clone();
            _fallback = fallback == null ? null : fallback.Clone();
            _hwnd = hwnd;
            _count = 0;
            _lastReportMs = -1000;
            _stopEvent.Reset();
            _pauseGate.Set();
            _sw.Reset();
            _thread = new Thread(Loop);
            _thread.IsBackground = true;
            _thread.Name = "MacroRunner";
            _thread.Start();
        }

        public void RequestStop()
        {
            _stopEvent.Set();
            _pauseGate.Set();
        }

        /// <summary>返回 true 表示当前处于暂停状态。</summary>
        public bool TogglePause()
        {
            if (!IsRunning) return false;
            if (_pauseGate.IsSet)
            {
                _pauseGate.Reset();
                FireState("paused");
                return true;
            }
            _pauseGate.Set();
            FireState("running");
            return false;
        }

        public void Dispose()
        {
            RequestStop();
            Thread t = _thread;
            if (t != null && t.IsAlive) t.Join(1500);
            _mover.RestoreAll();
        }

        // ==================== 主循环 ====================

        void Loop()
        {
            _sw.Start();
            FireState("running");
            try
            {
                PrepareEngine();

                List<Step> steps = new List<Step>();
                if (_profile.Steps != null && _profile.Steps.Count > 0) steps.AddRange(_profile.Steps);
                else if (_fallback != null) steps.Add(_fallback);

                if (steps.Count == 0)
                {
                    LogLine("没有可执行的动作");
                    return;
                }

                LogLine(string.Format("开始运行：引擎 {0}，{1} 个动作，间隔 {2}",
                    Names.EngineName(_profile.Engine), steps.Count, IntervalSummary()));

                int loops = 0;
                while (!_stopEvent.IsSet)
                {
                    for (int i = 0; i < steps.Count; i++)
                    {
                        if (_stopEvent.IsSet) break;
                        if (!CheckTargetAlive()) break;
                        WaitWhilePaused();
                        if (_stopEvent.IsSet) break;

                        Step s = steps[i];
                        try
                        {
                            Execute(s);
                        }
                        catch (Exception ex)
                        {
                            LogLine("步骤异常：" + ex.Message);
                        }

                        Interlocked.Increment(ref _count);
                        Report();

                        if (_stopEvent.IsSet) break;
                        int gap = _profile.NextIntervalMs(_rnd, s);
                        if (gap > 0 && !SleepInterruptible(gap)) break;
                    }

                    if (_stopEvent.IsSet) break;

                    loops++;
                    if (_profile.LoopCount > 0 && loops >= _profile.LoopCount)
                    {
                        LogLine(string.Format("已完成 {0} 轮，达到循环上限，自动停止", loops));
                        break;
                    }
                    if (_profile.RunSecondsLimit > 0 && _sw.Elapsed.TotalSeconds >= _profile.RunSecondsLimit)
                    {
                        LogLine(string.Format("已达到运行时长上限 {0} 秒，自动停止", _profile.RunSecondsLimit));
                        break;
                    }
                    if (_profile.Engine == EngineKind.Foreground && _profile.KeepForeground
                        && _hwnd != IntPtr.Zero && Win32.GetForegroundWindow() != _hwnd)
                    {
                        ActivateTarget();
                    }
                }
            }
            catch (Exception ex)
            {
                LogLine("运行线程异常：" + ex.Message);
            }
            finally
            {
                _sw.Stop();
                _mover.RestoreAll();
                LogLine(string.Format("已停止：共执行 {0} 次，用时 {1:hh\\:mm\\:ss}", _count, _sw.Elapsed));
                FireState("stopped");
            }
        }

        void PrepareEngine()
        {
            if (_hwnd == IntPtr.Zero)
            {
                if (_profile.Engine == EngineKind.Foreground)
                    LogLine("提示：未绑定目标窗口，将按当前光标/前台焦点执行");
                else
                    LogLine("警告：后台引擎未绑定目标窗口，动作会被跳过");
                return;
            }
            if (!Win32.IsWindow(_hwnd))
            {
                LogLine("目标窗口已关闭");
                return;
            }

            LogLine("目标窗口详情：" + WindowTarget.Describe(_hwnd));

            if (_profile.Engine == EngineKind.KeepAlive)
            {
                if (_mover.MoveOffscreen(_hwnd))
                    LogLine("保活：目标窗口已移出屏幕（停止后自动还原）");
                else
                    LogLine("保活：无法移动窗口，回退为普通后台投递");
            }
            else if (_profile.Engine == EngineKind.Foreground)
            {
                ActivateTarget();
            }
            else
            {
                LogLine("后台模式：不抢焦点，直接向目标窗口投递消息");
            }
        }

        string IntervalSummary()
        {
            if (_profile.IntervalFixed)
                return string.Format("固定 {0} ms", _profile.IntervalMin);
            return string.Format("随机 {0}~{1} ms", _profile.IntervalMin, _profile.IntervalMax);
        }

        void ActivateTarget()
        {
            string trace;
            bool ok = WindowTarget.Activate(_hwnd, out trace);
            LogLine((ok ? "目标窗口已激活：" : "激活目标窗口失败：") + trace);
        }

        bool CheckTargetAlive()
        {
            if (_hwnd == IntPtr.Zero) return true;
            if (Win32.IsWindow(_hwnd)) return true;
            LogLine("目标窗口已关闭，运行自动结束");
            _stopEvent.Set();
            return false;
        }

        // ==================== 动作执行 ====================

        void Execute(Step s)
        {
            switch (s.Kind)
            {
                case StepKind.Mouse: DoMouse(s); break;
                case StepKind.Wheel: DoWheel(s); break;
                case StepKind.Key: DoKey(s); break;
                case StepKind.Text: DoText(s); break;
                case StepKind.Delay: SleepInterruptible(s.DelayMs); break;
            }
        }

        void DoMouse(Step s)
        {
            int hold = s.HoldMs < 0 ? 0 : s.HoldMs;
            Win32.POINT pt = ResolvePoint(s);

            if (_profile.Engine == EngineKind.Foreground)
            {
                Win32.SetCursorPos(pt.X, pt.Y);
                if (s.Action == MouseActionKind.Down)
                {
                    InputEngine.ForegroundMouse(s.Button, true);
                    return;
                }
                if (s.Action == MouseActionKind.Up)
                {
                    InputEngine.ForegroundMouse(s.Button, false);
                    return;
                }
                InputEngine.ForegroundMouse(s.Button, true);
                SleepInterruptible(hold);
                InputEngine.ForegroundMouse(s.Button, false);
                if (s.Action == MouseActionKind.DoubleClick)
                {
                    SleepInterruptible(60);
                    InputEngine.ForegroundMouse(s.Button, true);
                    SleepInterruptible(hold);
                    InputEngine.ForegroundMouse(s.Button, false);
                }
                return;
            }

            if (!RequireTarget()) return;
            Win32.POINT client = pt;
            Win32.ScreenToClient(_hwnd, ref client);
            int cx;
            int cy;
            IntPtr child = InputEngine.DeepestChild(_hwnd, client.X, client.Y, out cx, out cy);

            InputEngine.PostMove(child, cx, cy);

            if (s.Action == MouseActionKind.Down)
            {
                InputEngine.PostMouse(child, s.Button, true, cx, cy);
                return;
            }
            if (s.Action == MouseActionKind.Up)
            {
                InputEngine.PostMouse(child, s.Button, false, cx, cy);
                return;
            }

            if (!InputEngine.PostMouse(child, s.Button, true, cx, cy))
                LogLine("警告：后台点击投递失败（目标窗口可能已失效）");
            SleepInterruptible(hold);
            InputEngine.PostMouse(child, s.Button, false, cx, cy);
            if (s.Action == MouseActionKind.DoubleClick)
            {
                SleepInterruptible(50);
                InputEngine.PostMouse(child, s.Button, true, cx, cy);
                SleepInterruptible(hold);
                InputEngine.PostMouse(child, s.Button, false, cx, cy);
            }
        }

        void DoWheel(Step s)
        {
            Win32.POINT pt = ResolvePoint(s);
            if (_profile.Engine == EngineKind.Foreground)
            {
                Win32.SetCursorPos(pt.X, pt.Y);
                InputEngine.ForegroundWheel(s.Notches);
                return;
            }
            if (!RequireTarget()) return;
            InputEngine.PostWheel(_hwnd, s.Notches, pt.X, pt.Y);
        }

        void DoKey(Step s)
        {
            if (s.Vk == 0)
            {
                LogLine("键盘步骤未设置主键，已跳过");
                return;
            }
            int[] mods = ModifierVks(s);

            if (_profile.Engine == EngineKind.Foreground)
            {
                for (int i = 0; i < mods.Length; i++) InputEngine.ForegroundKey(mods[i], true);
                InputEngine.ForegroundKey(s.Vk, true);
                SleepInterruptible(s.HoldMs);
                InputEngine.ForegroundKey(s.Vk, false);
                for (int i = mods.Length - 1; i >= 0; i--) InputEngine.ForegroundKey(mods[i], false);
                return;
            }

            if (!RequireTarget()) return;
            for (int i = 0; i < mods.Length; i++) InputEngine.PostKey(_hwnd, mods[i], true);
            InputEngine.PostKey(_hwnd, s.Vk, true);

            char ch;
            if (_profile.PostChar && !s.Ctrl && !s.Alt && !s.Win && TryChar(s.Vk, s.Shift, out ch))
                InputEngine.PostChar(_hwnd, ch);

            SleepInterruptible(s.HoldMs);
            InputEngine.PostKey(_hwnd, s.Vk, false);
            for (int i = mods.Length - 1; i >= 0; i--) InputEngine.PostKey(_hwnd, mods[i], false);
        }

        void DoText(Step s)
        {
            if (string.IsNullOrEmpty(s.Text)) return;

            if (_profile.Engine == EngineKind.Foreground)
            {
                InputEngine.ForegroundText(s.Text);
                return;
            }
            if (!RequireTarget()) return;

            // 文本要送到具体输入控件：定位到坐标处最深子窗口，没设坐标就投给主窗口
            IntPtr target = _hwnd;
            if (s.Pos != PosMode.Cursor)
            {
                Win32.POINT pt = ResolvePoint(s);
                Win32.POINT client = pt;
                Win32.ScreenToClient(_hwnd, ref client);
                int cx;
                int cy;
                target = InputEngine.DeepestChild(_hwnd, client.X, client.Y, out cx, out cy);
            }
            for (int i = 0; i < s.Text.Length; i++)
                InputEngine.PostChar(target, s.Text[i]);
        }

        bool RequireTarget()
        {
            if (_hwnd != IntPtr.Zero) return true;
            LogLine("后台模式需要绑定目标窗口，已跳过该动作");
            return false;
        }

        Win32.POINT ResolvePoint(Step s)
        {
            Win32.POINT p = new Win32.POINT(s.X, s.Y);
            if (s.Pos == PosMode.Cursor)
            {
                Win32.GetCursorPos(out p);
            }
            else if (s.Pos == PosMode.Client)
            {
                if (_hwnd != IntPtr.Zero) Win32.ClientToScreen(_hwnd, ref p);
            }
            if (s.Jitter > 0)
            {
                p.X = p.X + _rnd.Next(-s.Jitter, s.Jitter + 1);
                p.Y = p.Y + _rnd.Next(-s.Jitter, s.Jitter + 1);
            }
            return p;
        }

        static int[] ModifierVks(Step s)
        {
            List<int> list = new List<int>(4);
            if (s.Ctrl) list.Add(0x11);
            if (s.Shift) list.Add(0x10);
            if (s.Alt) list.Add(0x12);
            if (s.Win) list.Add(0x5B);
            return list.ToArray();
        }

        /// <summary>后台按键补发 WM_CHAR 用：把可打印键换成字符。</summary>
        static bool TryChar(int vk, bool shift, out char ch)
        {
            ch = '\0';
            if (vk >= 0x30 && vk <= 0x39)
            {
                const string shifted = ")!@#$%^&*(";
                ch = shift ? shifted[vk - 0x30] : (char)vk;
                return true;
            }
            if (vk >= 0x41 && vk <= 0x5A)
            {
                ch = shift ? (char)vk : (char)(vk + 32);
                return true;
            }
            if (vk == 0x20)
            {
                ch = ' ';
                return true;
            }
            return false;
        }

        // ==================== 时间控制 ====================

        bool SleepInterruptible(int ms)
        {
            if (ms <= 0) return !_stopEvent.IsSet;
            int left = ms;
            while (left > 0)
            {
                if (_stopEvent.IsSet) return false;
                if (!_pauseGate.IsSet)
                {
                    WaitWhilePaused();
                    continue;
                }
                int slice = left > 15 ? 15 : left;
                if (_stopEvent.Wait(slice)) return false;
                left -= slice;
            }
            return !_stopEvent.IsSet;
        }

        void WaitWhilePaused()
        {
            while (!_stopEvent.IsSet && !_pauseGate.IsSet) Thread.Sleep(20);
        }

        void Report()
        {
            long now = _sw.ElapsedMilliseconds;
            if (now - _lastReportMs < 60) return;
            _lastReportMs = now;
            Action<long, TimeSpan> h = Progress;
            if (h == null) return;
            try { h(Interlocked.Read(ref _count), _sw.Elapsed); }
            catch { }
        }

        void LogLine(string msg)
        {
            Action<string> h = Log;
            if (h == null) return;
            try { h(msg); }
            catch { }
        }

        void FireState(string state)
        {
            Action<string> h = StateChanged;
            if (h == null) return;
            try { h(state); }
            catch { }
        }
    }
}
