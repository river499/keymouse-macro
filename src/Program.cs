using System;
using System.Threading;
using System.Windows.Forms;
using KMMacro.Engine;
using KMMacro.UI;

namespace KMMacro
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "KeyMouseMacroWorkshop_v1", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("键鼠宏工坊已经在运行了。\r\n请使用已有的窗口（或托盘图标）。",
                        "键鼠宏工坊", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try { Win32.SetProcessDPIAware(); }
                catch { }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
                {
                    Fatal(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    Fatal(e.ExceptionObject as Exception);
                };

                Application.Run(new MainForm());
            }
        }

        static void Fatal(Exception ex)
        {
            try
            {
                MessageBox.Show(ex == null ? "发生未知错误" : ex.ToString(),
                    "键鼠宏工坊 — 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
