using System;
using System.Drawing;
using System.Windows.Forms;

namespace KMMacro.UI
{
    /// <summary>统一配色与控件工厂，避免界面代码里散落硬编码样式。</summary>
    internal static class Theme
    {
        public static readonly Color Page = Color.FromArgb(243, 245, 249);
        public static readonly Color Card = Color.White;
        public static readonly Color Header = Color.FromArgb(31, 36, 48);
        public static readonly Color HeaderSub = Color.FromArgb(148, 158, 176);
        public static readonly Color Ink = Color.FromArgb(36, 42, 54);
        public static readonly Color Muted = Color.FromArgb(132, 142, 158);
        public static readonly Color Accent = Color.FromArgb(52, 120, 246);
        public static readonly Color Green = Color.FromArgb(34, 177, 109);
        public static readonly Color Red = Color.FromArgb(232, 74, 74);
        public static readonly Color Purple = Color.FromArgb(120, 86, 240);
        public static readonly Color Gray = Color.FromArgb(150, 158, 170);

        public static readonly Font FontNormal = new Font("Microsoft YaHei UI", 9F);
        public static readonly Font FontBold = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        public static readonly Font FontSmall = new Font("Microsoft YaHei UI", 8.25F);
        public static readonly Font FontTitle = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
        public static readonly Font FontBig = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);

        public static Label Lbl(string text, int x, int y, int w, int h)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = FontNormal;
            l.ForeColor = Ink;
            l.SetBounds(x, y, w, h);
            l.AutoSize = false;
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        public static Label Hint(string text, int x, int y, int w, int h)
        {
            Label l = Lbl(text, x, y, w, h);
            l.Font = FontSmall;
            l.ForeColor = Muted;
            l.TextAlign = ContentAlignment.TopLeft;
            return l;
        }

        public static Button Btn(string text, Color back, int x, int y, int w, int h)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = FontBold;
            b.ForeColor = Color.White;
            b.BackColor = back;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.SetBounds(x, y, w, h);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            return b;
        }

        public static Button Ghost(string text, int x, int y, int w, int h)
        {
            Button b = Btn(text, Color.FromArgb(236, 239, 244), x, y, w, h);
            b.ForeColor = Ink;
            b.Font = FontNormal;
            return b;
        }

        public static GroupBox Card2(string text, int x, int y, int w, int h)
        {
            GroupBox g = new GroupBox();
            g.Text = " " + text;
            g.Font = FontBold;
            g.ForeColor = Ink;
            g.BackColor = Card;
            g.SetBounds(x, y, w, h);
            return g;
        }

        public static NumericUpDown Num(int x, int y, int w, decimal min, decimal max, decimal val, decimal step)
        {
            NumericUpDown n = new NumericUpDown();
            n.SetBounds(x, y, w, 24);
            n.Minimum = min;
            n.Maximum = max;
            n.Value = val;
            n.Increment = step;
            n.Font = FontNormal;
            n.TextAlign = HorizontalAlignment.Center;
            return n;
        }

        public static ComboBox Cbo(int x, int y, int w)
        {
            ComboBox c = new ComboBox();
            c.SetBounds(x, y, w, 24);
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Font = FontNormal;
            return c;
        }

        public static CheckBox Chk(string text, int x, int y, int w, int h)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.Font = FontNormal;
            c.ForeColor = Ink;
            c.SetBounds(x, y, w, h);
            return c;
        }

        public static RadioButton Radio(string text, int x, int y, int w, int h)
        {
            RadioButton r = new RadioButton();
            r.Text = text;
            r.Font = FontNormal;
            r.ForeColor = Ink;
            r.SetBounds(x, y, w, h);
            return r;
        }
    }
}
