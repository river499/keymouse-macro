using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace KMMacroTest
{
    /// <summary>
    /// 验证用目标窗口：把收到的鼠标/键盘消息逐条写入日志文件，
    /// 供 EngineTest 断言「消息是否真的到达了目标窗口」。
    /// 用法：KMMacroTestTarget.exe [日志路径]
    /// </summary>
    internal static class TestTarget
    {
        static string _logPath;
        static readonly object Gate = new object();

        [STAThread]
        static void Main(string[] args)
        {
            _logPath = args.Length > 0
                ? args[0]
                : Path.Combine(Path.GetTempPath(), "kmmacro_target.log");
            try { if (File.Exists(_logPath)) File.Delete(_logPath); }
            catch { }

            Application.EnableVisualStyles();

            Form f = new Form();
            f.Text = "KMMacroTestTarget";
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(120, 120);
            f.ClientSize = new Size(480, 320);
            f.KeyPreview = true;   // 键盘焦点可能落在按钮上，需由 Form 先接管
            f.MouseDown += delegate(object s, MouseEventArgs e)
            {
                Record(string.Format("MOUSEDOWN {0} {1},{2}", e.Button, e.X, e.Y));
            };
            f.MouseUp += delegate(object s, MouseEventArgs e)
            {
                Record(string.Format("MOUSEUP {0} {1},{2}", e.Button, e.X, e.Y));
            };
            f.KeyDown += delegate(object s, KeyEventArgs e)
            {
                Record("KEYDOWN " + e.KeyCode);
            };
            f.MouseWheel += delegate(object s, MouseEventArgs e)
            {
                Record("WHEEL " + e.Delta);
            };

            Label tip = new Label();
            tip.Text = "键鼠宏工坊 — 后台投递测试目标窗口";
            tip.SetBounds(20, 20, 300, 24);
            f.Controls.Add(tip);

            Button btn = new Button();
            btn.Text = "一个按钮";
            btn.SetBounds(300, 220, 120, 40);
            btn.MouseDown += delegate(object s, MouseEventArgs e)
            {
                Record("BUTTONDOWN " + e.Button);
            };
            f.Controls.Add(btn);

            Record("READY");
            Application.Run(f);
        }

        static void Record(string line)
        {
            lock (Gate)
            {
                try { File.AppendAllText(_logPath, line + Environment.NewLine); }
                catch { }
            }
        }
    }
}
