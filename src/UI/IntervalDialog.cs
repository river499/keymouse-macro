using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using KMMacro.Core;

namespace KMMacro.UI
{
    /// <summary>设置单个动作的独立间隔（留空则跟随全局）。</summary>
    internal sealed class IntervalDialog : Form
    {
        public int MinValue;
        public int MaxValue;

        readonly NumericUpDown _min;
        readonly NumericUpDown _max;

        public IntervalDialog(int min, int max)
        {
            Text = "单步间隔";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(340, 170);
            Font = Theme.FontNormal;
            BackColor = Theme.Card;

            Label tip = Theme.Hint("该动作执行后单独等待的时长，单位毫秒。\r\n最小值填 0 且最大值填 0 = 跟随全局间隔。", 16, 10, 310, 40);
            Controls.Add(tip);

            Controls.Add(Theme.Lbl("最小 (ms)", 16, 58, 70, 22));
            _min = Theme.Num(96, 57, 90, 0, 600000, min, 50);
            Controls.Add(_min);

            Controls.Add(Theme.Lbl("最大 (ms)", 16, 92, 70, 22));
            _max = Theme.Num(96, 91, 90, 0, 600000, max, 50);
            Controls.Add(_max);

            Button ok = Theme.Btn("确定", Theme.Accent, 176, 120, 74, 30);
            Button cancel = Theme.Ghost("取消", 254, 120, 70, 30);
            ok.Click += delegate { Commit(); };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(ok);
            Controls.Add(cancel);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        void Commit()
        {
            MinValue = (int)_min.Value;
            MaxValue = (int)_max.Value;
            if (MaxValue < MinValue) MaxValue = MinValue;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
