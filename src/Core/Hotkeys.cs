using System;
using KMMacro.Engine;

namespace KMMacro.Core
{
    /// <summary>全局热键注册。id 约定：1=开始/停止，2=停止，3=暂停，4=急停（固定 F12）。</summary>
    public static class Hotkeys
    {
        public const int IdToggle = 1;
        public const int IdStop = 2;
        public const int IdPause = 3;
        public const int IdPanic = 4;

        public const string PanicKey = "F12";

        public static readonly string[] Selectable = new string[]
        {
            "无", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"
        };

        public static int ToVk(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            string n = name.Trim().ToUpperInvariant();
            if (n == "无" || n == "NONE") return 0;
            if (n.Length >= 2 && n[0] == 'F')
            {
                int num;
                if (int.TryParse(n.Substring(1), out num) && num >= 1 && num <= 24)
                    return 0x70 + (num - 1);
            }
            return 0;
        }

        /// <summary>注册成功返回 true；name 为「无」时视为成功（无需注册）。</summary>
        public static bool Register(IntPtr hwnd, int id, string name)
        {
            int vk = ToVk(name);
            if (vk == 0) return true;
            return Win32.RegisterHotKey(hwnd, id, 0, (uint)vk);
        }

        public static void Unregister(IntPtr hwnd, int id)
        {
            try { Win32.UnregisterHotKey(hwnd, id); }
            catch { }
        }
    }
}
