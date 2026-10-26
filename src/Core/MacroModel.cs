using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using KMMacro.Engine;

namespace KMMacro.Core
{
    public enum StepKind { Mouse = 0, Key = 1, Text = 2, Delay = 3, Wheel = 4 }
    public enum PosMode { Cursor = 0, Screen = 1, Client = 2 }

    public sealed class Step
    {
        public StepKind Kind = StepKind.Mouse;
        public MouseButtonKind Button = MouseButtonKind.Left;
        public MouseActionKind Action = MouseActionKind.Click;
        public PosMode Pos = PosMode.Cursor;
        public int X;
        public int Y;
        public int Jitter;
        public int HoldMs = 50;
        public int Notches = 1;
        public int Vk;
        public bool Ctrl;
        public bool Alt;
        public bool Shift;
        public bool Win;
        public string Text = string.Empty;
        public int DelayMs = 500;
        public int IntervalMin;
        public int IntervalMax;

        public Step Clone()
        {
            Step s = new Step();
            s.Kind = Kind;
            s.Button = Button;
            s.Action = Action;
            s.Pos = Pos;
            s.X = X;
            s.Y = Y;
            s.Jitter = Jitter;
            s.HoldMs = HoldMs;
            s.Notches = Notches;
            s.Vk = Vk;
            s.Ctrl = Ctrl;
            s.Alt = Alt;
            s.Shift = Shift;
            s.Win = Win;
            s.Text = Text;
            s.DelayMs = DelayMs;
            s.IntervalMin = IntervalMin;
            s.IntervalMax = IntervalMax;
            return s;
        }

        public string KindText()
        {
            return Names.StepKindName(Kind);
        }

        public string ModifierText()
        {
            StringBuilder sb = new StringBuilder();
            if (Ctrl) sb.Append("Ctrl+");
            if (Alt) sb.Append("Alt+");
            if (Shift) sb.Append("Shift+");
            if (Win) sb.Append("Win+");
            return sb.ToString();
        }

        public string ParamText()
        {
            switch (Kind)
            {
                case StepKind.Mouse:
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(Names.ButtonName(Button));
                        sb.Append(" ");
                        sb.Append(Names.ActionName(Action));
                        if (Pos == PosMode.Cursor)
                            sb.Append("  @当前光标");
                        else
                            sb.AppendFormat("  @{0}({1},{2})", Names.PosName(Pos), X, Y);
                        if (Jitter > 0) sb.AppendFormat("  ±{0}px", Jitter);
                        if (Action == MouseActionKind.Click || Action == MouseActionKind.DoubleClick)
                            sb.AppendFormat("  按住{0}ms", HoldMs);
                        return sb.ToString();
                    }
                case StepKind.Key:
                    {
                        string k = Vk == 0 ? "(未设置)" : KeyMap.NameOf(Vk);
                        return string.Format("{0}{1}  按住{2}ms", ModifierText(), k, HoldMs);
                    }
                case StepKind.Text:
                    {
                        string t = Text == null ? string.Empty : Text;
                        string shown = t.Length > 40 ? t.Substring(0, 40) + "…" : t;
                        return string.Format("输入 «{0}»  ({1} 字符)", shown, t.Length);
                    }
                case StepKind.Delay:
                    return string.Format("等待 {0} ms", DelayMs);
                case StepKind.Wheel:
                    return string.Format("{0} {1} 格", Notches >= 0 ? "向上滚" : "向下滚", Math.Abs(Notches));
                default:
                    return string.Empty;
            }
        }

        public string IntervalText()
        {
            if (IntervalMin <= 0 && IntervalMax <= 0) return "跟随全局";
            if (IntervalMax <= IntervalMin) return IntervalMin.ToString(CultureInfo.InvariantCulture);
            return string.Format("{0}~{1}", IntervalMin, IntervalMax);
        }
    }

    public static class Names
    {
        public static string EngineName(EngineKind e)
        {
            switch (e)
            {
                case EngineKind.Foreground: return "前台模拟";
                case EngineKind.Background: return "后台消息";
                default: return "后台保活";
            }
        }

        public static string ButtonName(MouseButtonKind b)
        {
            switch (b)
            {
                case MouseButtonKind.Left: return "左键";
                case MouseButtonKind.Right: return "右键";
                case MouseButtonKind.Middle: return "中键";
                case MouseButtonKind.X1: return "侧键 X1";
                default: return "侧键 X2";
            }
        }

        public static string ActionName(MouseActionKind a)
        {
            switch (a)
            {
                case MouseActionKind.Click: return "单击";
                case MouseActionKind.DoubleClick: return "双击";
                case MouseActionKind.Down: return "按下不松";
                default: return "抬起";
            }
        }

        public static string PosName(PosMode p)
        {
            switch (p)
            {
                case PosMode.Cursor: return "当前光标";
                case PosMode.Screen: return "屏幕坐标";
                default: return "窗口客户区";
            }
        }

        public static string StepKindName(StepKind k)
        {
            switch (k)
            {
                case StepKind.Mouse: return "鼠标";
                case StepKind.Key: return "键盘";
                case StepKind.Text: return "文本";
                case StepKind.Delay: return "延时";
                default: return "滚轮";
            }
        }
    }

    /// <summary>键位名与虚拟键码互转。</summary>
    public static class KeyMap
    {
        public static readonly List<string> All = new List<string>();
        static readonly Dictionary<string, int> _nameToVk = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<int, string> _vkToName = new Dictionary<int, string>();

        static KeyMap()
        {
            for (int c = 'A'; c <= 'Z'; c++)
                Add(((char)c).ToString(), c);
            for (int d = '0'; d <= '9'; d++)
                Add(((char)d).ToString(), d);
            for (int f = 1; f <= 12; f++)
                Add("F" + f.ToString(CultureInfo.InvariantCulture), 0x6F + f);

            Add("空格", 0x20);
            Add("回车", 0x0D);
            Add("Tab", 0x09);
            Add("Esc", 0x1B);
            Add("退格", 0x08);
            Add("Insert", 0x2D);
            Add("Delete", 0x2E);
            Add("Home", 0x24);
            Add("End", 0x23);
            Add("PageUp", 0x21);
            Add("PageDown", 0x22);
            Add("←", 0x25);
            Add("↑", 0x26);
            Add("→", 0x27);
            Add("↓", 0x28);
            Add("小键盘0", 0x60);
            Add("小键盘1", 0x61);
            Add("小键盘2", 0x62);
            Add("小键盘3", 0x63);
            Add("小键盘4", 0x64);
            Add("小键盘5", 0x65);
            Add("小键盘6", 0x66);
            Add("小键盘7", 0x67);
            Add("小键盘8", 0x68);
            Add("小键盘9", 0x69);
            Add("小键盘+", 0x6B);
            Add("小键盘-", 0x6D);
            Add("小键盘*", 0x6A);
            Add("小键盘/", 0x6F);
            Add("小键盘.", 0x6E);
            Add("-", 0xBD);
            Add("=", 0xBB);
            Add(",", 0xBC);
            Add(".", 0xBE);
            Add("/", 0xBF);
            Add(";", 0xBA);
            Add("'", 0xDE);
            Add("[", 0xDB);
            Add("]", 0xDD);
            Add("\\", 0xDC);
            Add("`", 0xC0);
            Add("PrintScreen", 0x2C);
            Add("ScrollLock", 0x91);
            Add("Pause", 0x13);
            Add("CapsLock", 0x14);
            Add("NumLock", 0x90);
        }

        static void Add(string name, int vk)
        {
            if (_nameToVk.ContainsKey(name)) return;
            All.Add(name);
            _nameToVk[name] = vk;
            if (!_vkToName.ContainsKey(vk)) _vkToName[vk] = name;
        }

        public static int VkOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            int vk;
            if (_nameToVk.TryGetValue(name.Trim(), out vk)) return vk;
            return 0;
        }

        public static string NameOf(int vk)
        {
            string n;
            if (_vkToName.TryGetValue(vk, out n)) return n;
            return "VK_" + vk.ToString("X2", CultureInfo.InvariantCulture);
        }
    }

    public sealed class Profile
    {
        public EngineKind Engine = EngineKind.Background;
        public string TargetTitle = string.Empty;
        public string TargetClass = string.Empty;
        public bool IntervalFixed = false;
        public int IntervalMin = 500;
        public int IntervalMax = 1500;
        public int LoopCount = 0;
        public int RunSecondsLimit = 0;
        public string HotkeyStart = "F8";
        public string HotkeyStop = "F9";
        public string HotkeyPause = "F10";
        public bool PostChar = true;
        public bool KeepForeground = true;
        public List<Step> Steps = new List<Step>();

        public Profile Clone()
        {
            Profile p = new Profile();
            p.Engine = Engine;
            p.TargetTitle = TargetTitle;
            p.TargetClass = TargetClass;
            p.IntervalFixed = IntervalFixed;
            p.IntervalMin = IntervalMin;
            p.IntervalMax = IntervalMax;
            p.LoopCount = LoopCount;
            p.RunSecondsLimit = RunSecondsLimit;
            p.HotkeyStart = HotkeyStart;
            p.HotkeyStop = HotkeyStop;
            p.HotkeyPause = HotkeyPause;
            p.PostChar = PostChar;
            p.KeepForeground = KeepForeground;
            p.Steps = new List<Step>();
            for (int i = 0; i < Steps.Count; i++) p.Steps.Add(Steps[i].Clone());
            return p;
        }

        /// <summary>取本次动作执行后的间隔：单步设置优先，其次全局设置。</summary>
        public int NextIntervalMs(Random rnd, Step s)
        {
            int min = IntervalMin;
            int max = IntervalMax;
            if (s != null && (s.IntervalMin > 0 || s.IntervalMax > 0))
            {
                min = s.IntervalMin;
                max = s.IntervalMax;
            }
            if (IntervalFixed && (s == null || (s.IntervalMin <= 0 && s.IntervalMax <= 0)))
                return min < 0 ? 0 : min;
            if (max < min) max = min;
            if (max <= 0) return 0;
            if (max == min) return min;
            return rnd.Next(min, max + 1);
        }

        // ---------------- 存取 ----------------

        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, Serialize(), new UTF8Encoding(true));
        }

        public static Profile Load(string path)
        {
            if (!File.Exists(path)) return new Profile();
            return Deserialize(File.ReadAllText(path, Encoding.UTF8));
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 键鼠宏工坊 方案文件 v1");
            sb.AppendLine("engine=" + ((int)Engine).ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("targetTitle=" + Escape(TargetTitle));
            sb.AppendLine("targetClass=" + Escape(TargetClass));
            sb.AppendLine("intervalFixed=" + (IntervalFixed ? "1" : "0"));
            sb.AppendLine("intervalMin=" + IntervalMin.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("intervalMax=" + IntervalMax.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("loopCount=" + LoopCount.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("runSecondsLimit=" + RunSecondsLimit.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("hotkeyStart=" + HotkeyStart);
            sb.AppendLine("hotkeyStop=" + HotkeyStop);
            sb.AppendLine("hotkeyPause=" + HotkeyPause);
            sb.AppendLine("postChar=" + (PostChar ? "1" : "0"));
            sb.AppendLine("keepForeground=" + (KeepForeground ? "1" : "0"));
            for (int i = 0; i < Steps.Count; i++)
                sb.AppendLine("step=" + StepLine(Steps[i]));
            return sb.ToString();
        }

        static string StepLine(Step s)
        {
            int mods = 0;
            if (s.Ctrl) mods |= 1;
            if (s.Alt) mods |= 2;
            if (s.Shift) mods |= 4;
            if (s.Win) mods |= 8;
            StringBuilder sb = new StringBuilder();
            sb.Append("kind=").Append((int)s.Kind);
            sb.Append(";button=").Append((int)s.Button);
            sb.Append(";action=").Append((int)s.Action);
            sb.Append(";pos=").Append((int)s.Pos);
            sb.Append(";x=").Append(s.X);
            sb.Append(";y=").Append(s.Y);
            sb.Append(";jitter=").Append(s.Jitter);
            sb.Append(";hold=").Append(s.HoldMs);
            sb.Append(";notches=").Append(s.Notches);
            sb.Append(";vk=").Append(s.Vk);
            sb.Append(";mods=").Append(mods);
            sb.Append(";delay=").Append(s.DelayMs);
            sb.Append(";imin=").Append(s.IntervalMin);
            sb.Append(";imax=").Append(s.IntervalMax);
            sb.Append(";text=").Append(Escape(s.Text));
            return sb.ToString();
        }

        public static Profile Deserialize(string content)
        {
            Profile p = new Profile();
            p.Steps.Clear();
            if (content == null) return p;
            string[] lines = content.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1);

                switch (key)
                {
                    case "engine": p.Engine = (EngineKind)Int(val); break;
                    case "targetTitle": p.TargetTitle = Unescape(val); break;
                    case "targetClass": p.TargetClass = Unescape(val); break;
                    case "intervalFixed": p.IntervalFixed = val.Trim() == "1"; break;
                    case "intervalMin": p.IntervalMin = Int(val); break;
                    case "intervalMax": p.IntervalMax = Int(val); break;
                    case "loopCount": p.LoopCount = Int(val); break;
                    case "runSecondsLimit": p.RunSecondsLimit = Int(val); break;
                    case "hotkeyStart": p.HotkeyStart = val.Trim(); break;
                    case "hotkeyStop": p.HotkeyStop = val.Trim(); break;
                    case "hotkeyPause": p.HotkeyPause = val.Trim(); break;
                    case "postChar": p.PostChar = val.Trim() == "1"; break;
                    case "keepForeground": p.KeepForeground = val.Trim() == "1"; break;
                    case "step": p.Steps.Add(ParseStep(val)); break;
                }
            }
            return p;
        }

        /// <summary>按 ; 切分，但跳过被反斜杠转义的 ;（否则文本里的分号会被截断）。</summary>
        static List<string> SplitEscaped(string raw)
        {
            List<string> parts = new List<string>();
            StringBuilder cur = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c == '\\' && i + 1 < raw.Length)
                {
                    cur.Append(c);
                    cur.Append(raw[i + 1]);
                    i++;
                    continue;
                }
                if (c == ';')
                {
                    parts.Add(cur.ToString());
                    cur.Length = 0;
                    continue;
                }
                cur.Append(c);
            }
            parts.Add(cur.ToString());
            return parts;
        }

        static Step ParseStep(string raw)
        {
            Step s = new Step();
            List<string> parts = SplitEscaped(raw);
            for (int i = 0; i < parts.Count; i++)
            {
                string part = parts[i];
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string k = part.Substring(0, eq).Trim();
                string v = part.Substring(eq + 1);
                switch (k)
                {
                    case "kind": s.Kind = (StepKind)Int(v); break;
                    case "button": s.Button = (MouseButtonKind)Int(v); break;
                    case "action": s.Action = (MouseActionKind)Int(v); break;
                    case "pos": s.Pos = (PosMode)Int(v); break;
                    case "x": s.X = Int(v); break;
                    case "y": s.Y = Int(v); break;
                    case "jitter": s.Jitter = Int(v); break;
                    case "hold": s.HoldMs = Int(v); break;
                    case "notches": s.Notches = Int(v); break;
                    case "vk": s.Vk = Int(v); break;
                    case "delay": s.DelayMs = Int(v); break;
                    case "imin": s.IntervalMin = Int(v); break;
                    case "imax": s.IntervalMax = Int(v); break;
                    case "text": s.Text = Unescape(v); break;
                    case "mods":
                        {
                            int mods = Int(v);
                            s.Ctrl = (mods & 1) != 0;
                            s.Alt = (mods & 2) != 0;
                            s.Shift = (mods & 4) != 0;
                            s.Win = (mods & 8) != 0;
                            break;
                        }
                }
            }
            return s;
        }

        static int Int(string v)
        {
            int n;
            if (int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n;
            return 0;
        }

        static string Escape(string v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            StringBuilder sb = new StringBuilder(v.Length + 8);
            for (int i = 0; i < v.Length; i++)
            {
                char c = v[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case ';': sb.Append("\\;"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        static string Unescape(string v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            StringBuilder sb = new StringBuilder(v.Length);
            for (int i = 0; i < v.Length; i++)
            {
                char c = v[i];
                if (c != '\\' || i + 1 >= v.Length)
                {
                    sb.Append(c);
                    continue;
                }
                i++;
                char n = v[i];
                switch (n)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case '\\': sb.Append('\\'); break;
                    case ';': sb.Append(';'); break;
                    default: sb.Append(n); break;
                }
            }
            return sb.ToString();
        }
    }
}
