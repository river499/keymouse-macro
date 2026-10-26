using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using KMMacro.Core;
using KMMacro.Engine;
using WTarget = KMMacro.Engine.WindowTarget;

namespace KMMacro.UI
{
    public sealed class MainForm : Form
    {
        // ==================== 数据 ====================
        readonly Runner _runner = new Runner();
        readonly List<Step> _steps = new List<Step>();
        IntPtr _target = IntPtr.Zero;
        WindowInfo _targetInfo;
        string _configPath;
        string _profilesDir;
        bool _syncing;

        Timer _pickTimer;
        int _pickCountdown;
        bool _pickIsCoords;
        Button _pickButton;
        string _pickLabel;
        NotifyIcon _tray;

        // ==================== 页1 控件 ====================
        RadioButton rbForeground;
        RadioButton rbBackground;
        RadioButton rbKeepAlive;
        Label lblEngineHint;

        ComboBox cbWindows;
        Button btnRefresh;
        Button btnPickWindow;
        Button btnUnbind;
        Button btnDiagnose;
        Label lblTarget;
        Label lblTargetHint;

        RadioButton rbFixed;
        RadioButton rbRandom;
        NumericUpDown numFixed;
        NumericUpDown numMin;
        NumericUpDown numMax;
        NumericUpDown numHold;
        NumericUpDown numLoop;
        NumericUpDown numLimitSec;
        Label lblRunHint;

        ComboBox cbMouseButton;
        ComboBox cbMouseAction;
        ComboBox cbPosMode;
        Button btnPickPos;
        NumericUpDown numX;
        NumericUpDown numY;
        NumericUpDown numJitter;
        NumericUpDown numNotches;
        Label lblMouseHint;
        Button btnAddMouse;

        CheckBox chkCtrl;
        CheckBox chkAlt;
        CheckBox chkShift;
        CheckBox chkWin;
        ComboBox cbKey;
        NumericUpDown numKeyHold;
        TextBox txtText;
        Button btnAddKey;
        Button btnAddText;
        Label lblKeyHint;

        Button btnQuickMouse;
        Button btnQuickKey;
        Label lblQuickHint;

        // ==================== 页2 控件 ====================
        ListView lvSteps;
        Button btnUp;
        Button btnDown;
        Button btnDel;
        Button btnClear;
        Button btnDup;
        Button btnSetInterval;
        Label lblMacroHint;

        // ==================== 页3 控件 ====================
        ComboBox cboHotStart;
        ComboBox cboHotStop;
        ComboBox cboHotPause;
        Label lblHotkeyHint;
        CheckBox chkPostChar;
        CheckBox chkKeepFg;
        CheckBox chkTopMost;
        Button btnElevate;
        TextBox txtProfileName;
        Button btnSaveProfile;
        Button btnLoadProfile;
        Button btnOpenProfiles;
        Label lblProfilesPath;
        Label lblAbout;

        // ==================== 底部与状态栏 ====================
        RichTextBox rtbLog;
        Button btnStart;
        Button btnStop;
        Button btnPause;
        Button btnClearLog;
        Label lblState;
        Label lblEngineStatus;
        Label lblTargetStatus;
        Label lblCount;
        Label lblElapsed;

        // 需要按 DPI 手工缩放的容器（表格行高/列宽、Tab 尺寸等不参与自动缩放）
        TableLayoutPanel _root;
        TableLayoutPanel _footerTable;
        TableLayoutPanel _statusTable;
        Panel _footerPad;
        Panel _logBody;
        Label _headerRight;
        TabControl _tabs;
        float _scale = 1f;

        // ==================== 构造 ====================

        public MainForm()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KeyMouseMacro", "last.ini");
            _profilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles");

            BuildUi();
            ApplyDpiScaling();
            HookEvents();
            HookRunner();

            ApplyDefaults();
            LoadLastProfile();
            RefreshWindows();
            RegisterHotkeys();
            UpdateEngineHint();
            SyncIntervalEnabled();
            UpdateStatus();

            Log("键鼠宏工坊 v1.0 已就绪   F8 开始/停止 · F9 停止 · F10 暂停 · F12 急停");
            Log("流程：① 选输入引擎  ② 绑定目标窗口  ③ 配置动作并「添加」或用快捷运行。");
        }

        // ==================== 界面构建 ====================

        void BuildUi()
        {
            Text = "键鼠宏工坊 v1.0 — 指定窗口的后台/最小化 键鼠宏与连点";
            // 设计坐标一律按 96 DPI 写，构建完成后由 ApplyDpiScaling() 统一按实际 DPI 缩放。
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(1060, 820);
            MinimumSize = new Size(1090, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Page;
            Font = Theme.FontNormal;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildTabs(), 0, 1);
            root.Controls.Add(BuildFooter(), 0, 2);
            root.Controls.Add(BuildStatusBar(), 0, 3);
            _root = root;
        }

        Control BuildHeader()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Theme.Header;

            Label title = new Label();
            title.Text = "键鼠宏工坊";
            title.Font = Theme.FontTitle;
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(22, 10);
            p.Controls.Add(title);

            Label sub = new Label();
            sub.Text = "指定窗口 · 后台/最小化可用 · 随机间隔 · 按键时长 · 宏序列";
            sub.Font = Theme.FontSmall;
            sub.ForeColor = Theme.HeaderSub;
            sub.AutoSize = true;
            sub.Location = new Point(24, 44);
            p.Controls.Add(sub);

            Label right = new Label();
            right.Text = "v1.0    前台 / 后台 / 保活  三引擎";
            right.Font = Theme.FontSmall;
            right.ForeColor = Theme.HeaderSub;
            right.Dock = DockStyle.Right;
            right.Width = 300;
            right.TextAlign = ContentAlignment.MiddleRight;
            right.Padding = new Padding(0, 0, 22, 0);
            p.Controls.Add(right);
            _headerRight = right;

            return p;
        }

        Control BuildTabs()
        {
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = Theme.FontBold;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(140, 30);
            tabs.Padding = new Point(16, 6);

            TabPage p1 = NewPage("键鼠");
            BuildPageKeys(p1);
            tabs.TabPages.Add(p1);

            TabPage p2 = NewPage("宏序列");
            BuildPageMacro(p2);
            tabs.TabPages.Add(p2);

            TabPage p3 = NewPage("高级");
            BuildPageAdvanced(p3);
            tabs.TabPages.Add(p3);

            _tabs = tabs;
            return tabs;
        }

        static TabPage NewPage(string text)
        {
            TabPage p = new TabPage(text);
            p.BackColor = Theme.Page;
            p.AutoScroll = true;
            return p;
        }

        void BuildPageKeys(TabPage p)
        {
            // ---------- ① 输入引擎 ----------
            GroupBox g1 = Theme.Card2("① 输入引擎", 14, 10, 500, 112);
            p.Controls.Add(g1);
            rbForeground = Theme.Radio("前台模拟", 16, 24, 110, 22);
            rbBackground = Theme.Radio("后台消息", 130, 24, 110, 22);
            rbKeepAlive = Theme.Radio("后台保活", 244, 24, 110, 22);
            g1.Controls.Add(rbForeground);
            g1.Controls.Add(rbBackground);
            g1.Controls.Add(rbKeepAlive);
            lblEngineHint = Theme.Hint("", 16, 50, 468, 56);
            g1.Controls.Add(lblEngineHint);

            // ---------- ② 目标窗口 ----------
            GroupBox g2 = Theme.Card2("② 目标窗口", 14, 132, 500, 172);
            p.Controls.Add(g2);
            cbWindows = Theme.Cbo(16, 26, 326);
            g2.Controls.Add(cbWindows);
            btnRefresh = Theme.Ghost("刷新列表", 348, 25, 132, 26);
            g2.Controls.Add(btnRefresh);
            btnPickWindow = Theme.Btn("从光标拾取", Theme.Accent, 16, 60, 118, 26);
            g2.Controls.Add(btnPickWindow);
            btnUnbind = Theme.Ghost("取消绑定", 140, 60, 100, 26);
            g2.Controls.Add(btnUnbind);
            btnDiagnose = Theme.Btn("后台诊断", Theme.Purple, 348, 59, 132, 26);
            g2.Controls.Add(btnDiagnose);
            lblTarget = Theme.Lbl("未绑定窗口", 16, 94, 464, 22);
            lblTarget.Font = Theme.FontBold;
            g2.Controls.Add(lblTarget);
            lblTargetHint = Theme.Hint("前台模拟可不绑定；后台消息 / 后台保活必须先绑定。", 16, 120, 464, 44);
            g2.Controls.Add(lblTargetHint);

            // ---------- ③ 运行节奏 ----------
            GroupBox g3 = Theme.Card2("③ 运行节奏", 14, 314, 500, 152);
            p.Controls.Add(g3);
            rbFixed = Theme.Radio("固定", 16, 26, 60, 22);
            g3.Controls.Add(rbFixed);
            numFixed = Theme.Num(78, 25, 86, 0, 600000, 1000, 50);
            g3.Controls.Add(numFixed);
            g3.Controls.Add(Theme.Lbl("毫秒", 170, 27, 40, 20));
            rbRandom = Theme.Radio("随机", 218, 26, 60, 22);
            g3.Controls.Add(rbRandom);
            numMin = Theme.Num(282, 25, 76, 0, 600000, 500, 50);
            g3.Controls.Add(numMin);
            g3.Controls.Add(Theme.Lbl("到", 362, 27, 20, 20));
            numMax = Theme.Num(384, 25, 76, 0, 600000, 1500, 50);
            g3.Controls.Add(numMax);

            g3.Controls.Add(Theme.Lbl("按键时长", 16, 60, 70, 20));
            numHold = Theme.Num(88, 59, 86, 0, 60000, 50, 10);
            g3.Controls.Add(numHold);
            g3.Controls.Add(Theme.Lbl("毫秒", 178, 61, 44, 20));
            g3.Controls.Add(Theme.Lbl("循环次数", 232, 60, 74, 20));
            numLoop = Theme.Num(308, 59, 76, 0, 1000000, 0, 1);
            g3.Controls.Add(numLoop);
            g3.Controls.Add(Theme.Lbl("0 = 无限", 392, 61, 100, 20));

            g3.Controls.Add(Theme.Lbl("运行上限", 16, 94, 70, 20));
            numLimitSec = Theme.Num(88, 93, 86, 0, 86400, 0, 10);
            g3.Controls.Add(numLimitSec);
            g3.Controls.Add(Theme.Lbl("秒，0 = 不限", 180, 95, 150, 20));
            lblRunHint = Theme.Hint("间隔是「每个动作执行完之后再等多久」，随机间隔在做防检测的软件里更自然。", 16, 122, 466, 24);
            g3.Controls.Add(lblRunHint);

            // ---------- 底部快捷 ----------
            btnQuickMouse = Theme.Btn("▶  运行鼠标动作", Theme.Green, 14, 474, 220, 38);
            p.Controls.Add(btnQuickMouse);
            btnQuickKey = Theme.Btn("▶  运行键盘动作", Theme.Accent, 244, 474, 220, 38);
            p.Controls.Add(btnQuickKey);
            lblQuickHint = Theme.Hint("底部「开始」按钮：宏列表非空则跑整个序列，为空则跑当前鼠标动作。", 478, 476, 540, 36);
            p.Controls.Add(lblQuickHint);

            // ---------- ④ 鼠标动作 ----------
            GroupBox g4 = Theme.Card2("④ 鼠标动作", 524, 10, 496, 236);
            p.Controls.Add(g4);
            g4.Controls.Add(Theme.Lbl("按键", 16, 26, 40, 20));
            cbMouseButton = Theme.Cbo(58, 25, 110);
            cbMouseButton.Items.AddRange(new object[] { "左键", "右键", "中键", "侧键 X1", "侧键 X2" });
            g4.Controls.Add(cbMouseButton);
            g4.Controls.Add(Theme.Lbl("动作", 180, 26, 44, 20));
            cbMouseAction = Theme.Cbo(226, 25, 116);
            cbMouseAction.Items.AddRange(new object[] { "单击", "双击", "按下不松", "抬起", "滚轮" });
            g4.Controls.Add(cbMouseAction);
            g4.Controls.Add(Theme.Lbl("滚轮格数", 354, 26, 70, 20));
            numNotches = Theme.Num(428, 25, 52, -50, 50, 1, 1);
            g4.Controls.Add(numNotches);

            g4.Controls.Add(Theme.Lbl("位置", 16, 58, 40, 20));
            cbPosMode = Theme.Cbo(58, 57, 150);
            cbPosMode.Items.AddRange(new object[] { "当前光标", "屏幕坐标", "窗口客户区" });
            g4.Controls.Add(cbPosMode);
            btnPickPos = Theme.Btn("拾取坐标", Theme.Accent, 216, 56, 110, 26);
            g4.Controls.Add(btnPickPos);

            g4.Controls.Add(Theme.Lbl("X", 16, 90, 16, 20));
            numX = Theme.Num(34, 89, 80, -32000, 32000, 0, 10);
            g4.Controls.Add(numX);
            g4.Controls.Add(Theme.Lbl("Y", 120, 90, 16, 20));
            numY = Theme.Num(138, 89, 80, -32000, 32000, 0, 10);
            g4.Controls.Add(numY);
            g4.Controls.Add(Theme.Lbl("随机偏移±", 226, 90, 76, 20));
            numJitter = Theme.Num(306, 89, 56, 0, 500, 0, 1);
            g4.Controls.Add(numJitter);
            g4.Controls.Add(Theme.Lbl("px", 366, 90, 24, 20));

            lblMouseHint = Theme.Hint("「窗口客户区」坐标不随窗口移动变化，是绑定窗口场景的首选。", 16, 120, 462, 24);
            g4.Controls.Add(lblMouseHint);
            btnAddMouse = Theme.Ghost("＋ 添加鼠标动作", 16, 176, 200, 34);
            g4.Controls.Add(btnAddMouse);

            // ---------- ⑤ 键盘动作 ----------
            GroupBox g5 = Theme.Card2("⑤ 键盘动作", 524, 256, 496, 208);
            p.Controls.Add(g5);
            g5.Controls.Add(Theme.Lbl("修饰键", 16, 26, 50, 20));
            chkCtrl = Theme.Chk("Ctrl", 74, 24, 56, 22);
            chkAlt = Theme.Chk("Alt", 132, 24, 52, 22);
            chkShift = Theme.Chk("Shift", 186, 24, 64, 22);
            chkWin = Theme.Chk("Win", 252, 24, 52, 22);
            g5.Controls.Add(chkCtrl);
            g5.Controls.Add(chkAlt);
            g5.Controls.Add(chkShift);
            g5.Controls.Add(chkWin);

            g5.Controls.Add(Theme.Lbl("主键", 16, 58, 40, 20));
            cbKey = Theme.Cbo(58, 57, 150);
            g5.Controls.Add(cbKey);
            g5.Controls.Add(Theme.Lbl("按住", 220, 58, 40, 20));
            numKeyHold = Theme.Num(262, 57, 76, 0, 60000, 50, 10);
            g5.Controls.Add(numKeyHold);
            g5.Controls.Add(Theme.Lbl("毫秒", 344, 59, 40, 20));

            g5.Controls.Add(Theme.Lbl("文本", 16, 90, 40, 20));
            txtText = new TextBox();
            txtText.SetBounds(58, 89, 420, 24);
            txtText.Font = Theme.FontNormal;
            g5.Controls.Add(txtText);

            btnAddKey = Theme.Ghost("＋ 添加按键动作", 16, 122, 200, 34);
            g5.Controls.Add(btnAddKey);
            btnAddText = Theme.Ghost("＋ 添加文本动作", 222, 122, 256, 34);
            g5.Controls.Add(btnAddText);

            lblKeyHint = Theme.Hint("后台文本建议把「位置」设为窗口客户区并对准输入框，再添加文本动作。", 16, 164, 462, 36);
            g5.Controls.Add(lblKeyHint);
        }

        void BuildPageMacro(TabPage p)
        {
            GroupBox g = Theme.Card2("动作序列（按顺序循环执行）", 14, 10, 720, 500);
            p.Controls.Add(g);

            lvSteps = new ListView();
            lvSteps.SetBounds(16, 24, 688, 462);
            lvSteps.View = View.Details;
            lvSteps.FullRowSelect = true;
            lvSteps.GridLines = false;
            lvSteps.MultiSelect = true;
            lvSteps.HideSelection = false;
            lvSteps.Font = Theme.FontNormal;
            lvSteps.Columns.Add("#", 44, HorizontalAlignment.Center);
            lvSteps.Columns.Add("动作", 86, HorizontalAlignment.Left);
            lvSteps.Columns.Add("参数", 414, HorizontalAlignment.Left);
            lvSteps.Columns.Add("间隔(ms)", 118, HorizontalAlignment.Left);
            g.Controls.Add(lvSteps);

            btnUp = Theme.Ghost("↑ 上移", 744, 26, 130, 34);
            p.Controls.Add(btnUp);
            btnDown = Theme.Ghost("↓ 下移", 884, 26, 130, 34);
            p.Controls.Add(btnDown);
            btnDel = Theme.Btn("删除", Theme.Red, 744, 68, 130, 34);
            p.Controls.Add(btnDel);
            btnClear = Theme.Ghost("清空", 884, 68, 130, 34);
            p.Controls.Add(btnClear);
            btnDup = Theme.Ghost("复制一份", 744, 110, 130, 34);
            p.Controls.Add(btnDup);
            btnSetInterval = Theme.Ghost("单步间隔", 884, 110, 130, 34);
            p.Controls.Add(btnSetInterval);

            GroupBox g7 = Theme.Card2("说明", 744, 156, 270, 354);
            p.Controls.Add(g7);
            lblMacroHint = Theme.Hint(
                "· 列表为空时，底部「开始」会循环执行「鼠标动作」区的当前配置。\r\n\r\n" +
                "· 每个动作执行完后按「间隔」等待；单步可设独立间隔（0/0 = 跟随全局）。\r\n\r\n" +
                "· 循环次数 0 = 无限循环。\r\n\r\n" +
                "· 双击列表项可设置单步间隔。\r\n\r\n" +
                "· F12 任何情况下都能急停。",
                14, 26, 242, 316);
            g7.Controls.Add(lblMacroHint);
        }

        void BuildPageAdvanced(TabPage p)
        {
            GroupBox g8 = Theme.Card2("全局热键", 14, 10, 520, 180);
            p.Controls.Add(g8);
            g8.Controls.Add(Theme.Lbl("开始/停止", 16, 28, 76, 22));
            cboHotStart = Theme.Cbo(96, 27, 120);
            cboHotStart.Items.AddRange(Hotkeys.Selectable);
            g8.Controls.Add(cboHotStart);
            g8.Controls.Add(Theme.Lbl("强制停止", 16, 62, 76, 22));
            cboHotStop = Theme.Cbo(96, 61, 120);
            cboHotStop.Items.AddRange(Hotkeys.Selectable);
            g8.Controls.Add(cboHotStop);
            g8.Controls.Add(Theme.Lbl("暂停/继续", 16, 96, 76, 22));
            cboHotPause = Theme.Cbo(96, 95, 120);
            cboHotPause.Items.AddRange(Hotkeys.Selectable);
            g8.Controls.Add(cboHotPause);
            lblHotkeyHint = Theme.Hint("F12 是固定急停键，始终生效。若热键注册失败，通常是已被别的程序占用。", 16, 126, 486, 44);
            g8.Controls.Add(lblHotkeyHint);

            GroupBox g9 = Theme.Card2("运行选项", 14, 200, 520, 152);
            p.Controls.Add(g9);
            chkPostChar = Theme.Chk("后台按键时补发 WM_CHAR（文本类输入框需要，勾选成功率更高）", 16, 26, 486, 22);
            g9.Controls.Add(chkPostChar);
            chkKeepFg = Theme.Chk("前台模式：每轮检查并保持目标窗口在前台", 16, 54, 486, 22);
            g9.Controls.Add(chkKeepFg);
            chkTopMost = Theme.Chk("本工具窗口置顶", 16, 82, 486, 22);
            g9.Controls.Add(chkTopMost);
            btnElevate = Theme.Ghost("以管理员身份重启", 16, 110, 180, 28);
            g9.Controls.Add(btnElevate);

            GroupBox g10 = Theme.Card2("方案文件（可存多套）", 548, 10, 472, 180);
            p.Controls.Add(g10);
            g10.Controls.Add(Theme.Lbl("方案名", 16, 28, 54, 22));
            txtProfileName = new TextBox();
            txtProfileName.SetBounds(74, 27, 180, 24);
            txtProfileName.Text = "默认方案";
            txtProfileName.Font = Theme.FontNormal;
            g10.Controls.Add(txtProfileName);
            btnSaveProfile = Theme.Btn("保存", Theme.Accent, 262, 26, 90, 26);
            g10.Controls.Add(btnSaveProfile);
            btnLoadProfile = Theme.Ghost("加载", 358, 26, 90, 26);
            g10.Controls.Add(btnLoadProfile);
            btnOpenProfiles = Theme.Ghost("打开方案目录", 16, 62, 140, 26);
            g10.Controls.Add(btnOpenProfiles);
            lblProfilesPath = Theme.Hint("", 16, 96, 444, 66);
            g10.Controls.Add(lblProfilesPath);

            GroupBox g11 = Theme.Card2("能力与边界（如实说明）", 548, 200, 472, 300);
            p.Controls.Add(g11);
            lblAbout = Theme.Hint(
                "✓ 前台模拟：真实系统输入，任何程序都认（含游戏）；运行时激活并占用你的键鼠。\r\n\r\n" +
                "✓ 后台消息：不抢焦点，直接向目标窗口投递消息，可最小化。\r\n\r\n" +
                "✓ 后台保活：把窗口移出屏幕（不最小化），适合最小化会冻结的程序。\r\n\r\n" +
                "✗ 反作弊驱动会拦截合成输入，这类只能驱动级方案，本工具不含。\r\n\r\n" +
                "✗ Chromium 系、部分模拟器的后台投递成功率取决于其实现，无效请改用前台。\r\n\r\n" +
                "· 目标以管理员运行时，本工具也需管理员权限。",
                14, 26, 444, 266);
            g11.Controls.Add(lblAbout);
        }

        Control BuildFooter()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Theme.Page;
            p.Padding = new Padding(14, 8, 14, 8);

            Panel right = new Panel();
            right.Dock = DockStyle.Right;
            right.Width = 320;

            Panel left = new Panel();
            left.Dock = DockStyle.Fill;
            left.Padding = new Padding(0, 0, 14, 0);

            p.Controls.Add(left);
            p.Controls.Add(right);
            _footerPad = p;

            GroupBox logCard = Theme.Card2("运行日志", 0, 0, 10, 10);
            logCard.Dock = DockStyle.Fill;
            Panel logBody = new Panel();
            logBody.Dock = DockStyle.Fill;
            logBody.Padding = new Padding(10, 2, 10, 8);
            rtbLog = new RichTextBox();
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.BackColor = Color.FromArgb(250, 251, 253);
            rtbLog.ForeColor = Theme.Ink;
            rtbLog.Font = new Font("Consolas", 9F);
            rtbLog.ReadOnly = true;
            rtbLog.WordWrap = true;
            rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            logBody.Controls.Add(rtbLog);
            _logBody = logBody;
            logCard.Controls.Add(logBody);
            left.Controls.Add(logCard);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 2;
            t.RowCount = 3;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            right.Controls.Add(t);
            _footerTable = t;

            btnStart = Theme.Btn("▶   开始  (F8)", Theme.Green, 0, 0, 100, 50);
            btnStart.Dock = DockStyle.Fill;
            btnStart.Font = Theme.FontBig;
            btnStart.Margin = new Padding(0, 0, 0, 6);
            t.Controls.Add(btnStart, 0, 0);
            t.SetColumnSpan(btnStart, 2);

            btnStop = Theme.Btn("■  停止 (F9)", Theme.Red, 0, 0, 100, 46);
            btnStop.Dock = DockStyle.Fill;
            btnStop.Margin = new Padding(0, 0, 4, 6);
            t.Controls.Add(btnStop, 0, 1);

            btnPause = Theme.Btn("‖  暂停 (F10)", Theme.Purple, 0, 0, 100, 46);
            btnPause.Dock = DockStyle.Fill;
            btnPause.Margin = new Padding(4, 0, 0, 6);
            t.Controls.Add(btnPause, 1, 1);

            btnClearLog = Theme.Ghost("清空日志", 0, 0, 100, 26);
            btnClearLog.Dock = DockStyle.Top;
            btnClearLog.Height = 26;
            btnClearLog.Margin = new Padding(0, 2, 0, 0);
            t.Controls.Add(btnClearLog, 0, 2);
            t.SetColumnSpan(btnClearLog, 2);

            return p;
        }

        Control BuildStatusBar()
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Color.White;
            t.ColumnCount = 5;
            t.RowCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));

            lblState = new Label();
            lblState.Text = "● 已停止";
            lblState.Font = Theme.FontBold;
            lblState.ForeColor = Theme.Muted;
            lblState.Dock = DockStyle.Fill;
            lblState.TextAlign = ContentAlignment.MiddleLeft;
            lblState.Padding = new Padding(14, 0, 0, 0);
            t.Controls.Add(lblState, 0, 0);

            lblEngineStatus = new Label();
            lblEngineStatus.Text = "引擎：—";
            lblEngineStatus.Font = Theme.FontSmall;
            lblEngineStatus.ForeColor = Theme.Muted;
            lblEngineStatus.Dock = DockStyle.Fill;
            lblEngineStatus.TextAlign = ContentAlignment.MiddleLeft;
            t.Controls.Add(lblEngineStatus, 1, 0);

            lblTargetStatus = new Label();
            lblTargetStatus.Text = "目标：未绑定";
            lblTargetStatus.Font = Theme.FontSmall;
            lblTargetStatus.ForeColor = Theme.Muted;
            lblTargetStatus.Dock = DockStyle.Fill;
            lblTargetStatus.TextAlign = ContentAlignment.MiddleLeft;
            t.Controls.Add(lblTargetStatus, 2, 0);

            lblCount = new Label();
            lblCount.Text = "操作 0";
            lblCount.Font = Theme.FontSmall;
            lblCount.ForeColor = Theme.Muted;
            lblCount.Dock = DockStyle.Fill;
            lblCount.TextAlign = ContentAlignment.MiddleRight;
            t.Controls.Add(lblCount, 3, 0);

            lblElapsed = new Label();
            lblElapsed.Text = "时长 00:00:00";
            lblElapsed.Font = Theme.FontSmall;
            lblElapsed.ForeColor = Theme.Muted;
            lblElapsed.Dock = DockStyle.Fill;
            lblElapsed.TextAlign = ContentAlignment.MiddleRight;
            lblElapsed.Padding = new Padding(0, 0, 14, 0);
            t.Controls.Add(lblElapsed, 4, 0);

            _statusTable = t;
            return t;
        }

        // ==================== 事件挂钩 ====================

        void HookEvents()
        {
            rbForeground.CheckedChanged += delegate { UpdateEngineHint(); UpdateStatus(); };
            rbBackground.CheckedChanged += delegate { UpdateEngineHint(); UpdateStatus(); };
            rbKeepAlive.CheckedChanged += delegate { UpdateEngineHint(); UpdateStatus(); };
            rbFixed.CheckedChanged += delegate { SyncIntervalEnabled(); };
            rbRandom.CheckedChanged += delegate { SyncIntervalEnabled(); };

            btnRefresh.Click += delegate { RefreshWindows(); };
            btnPickWindow.Click += delegate { StartPick(false); };
            btnUnbind.Click += delegate { UnbindTarget(); };
            btnDiagnose.Click += delegate { Diagnose(); };
            cbWindows.SelectedIndexChanged += delegate { OnWindowSelected(); };

            btnPickPos.Click += delegate { StartPick(true); };
            btnAddMouse.Click += delegate { AddStep(BuildMouseStep()); };
            btnAddKey.Click += delegate { AddStep(BuildKeyStep()); };
            btnAddText.Click += delegate { AddStep(BuildTextStep()); };
            btnQuickMouse.Click += delegate { BeginRun(BuildMouseStep(), "鼠标动作"); };
            btnQuickKey.Click += delegate { BeginRun(BuildKeyStep(), "键盘动作"); };

            btnUp.Click += delegate { MoveStep(-1); };
            btnDown.Click += delegate { MoveStep(1); };
            btnDel.Click += delegate { DeleteSelectedSteps(); };
            btnClear.Click += delegate { ClearSteps(); };
            btnDup.Click += delegate { DuplicateStep(); };
            btnSetInterval.Click += delegate { SetSelectedInterval(); };
            lvSteps.DoubleClick += delegate { SetSelectedInterval(); };

            btnStart.Click += delegate { BeginRun(null, "宏序列"); };
            btnStop.Click += delegate { StopRun("按钮"); };
            btnPause.Click += delegate { TogglePause(); };
            btnClearLog.Click += delegate { rtbLog.Clear(); };

            chkTopMost.CheckedChanged += delegate { TopMost = chkTopMost.Checked; };
            btnElevate.Click += delegate { RestartAsAdmin(); };
            btnSaveProfile.Click += delegate { SaveProfileFile(); };
            btnLoadProfile.Click += delegate { LoadProfileFile(); };
            btnOpenProfiles.Click += delegate { OpenProfilesFolder(); };

            cboHotStart.SelectedIndexChanged += delegate { RegisterHotkeys(); };
            cboHotStop.SelectedIndexChanged += delegate { RegisterHotkeys(); };
            cboHotPause.SelectedIndexChanged += delegate { RegisterHotkeys(); };

            FormClosing += OnFormClosing;
        }

        void HookRunner()
        {
            _runner.Log += Log;
            _runner.StateChanged += OnState;
            _runner.Progress += OnProgress;
        }

        void ApplyDefaults()
        {
            _syncing = true;
            try
            {
                rbBackground.Checked = true;
                rbRandom.Checked = true;
                cbMouseButton.SelectedIndex = 0;
                cbMouseAction.SelectedIndex = 0;
                cbPosMode.SelectedIndex = 2;
                foreach (string k in KeyMap.All) cbKey.Items.Add(k);
                cbKey.SelectedIndex = 0;
                cboHotStart.SelectedIndex = IndexOfHotkey("F8");
                cboHotStop.SelectedIndex = IndexOfHotkey("F9");
                cboHotPause.SelectedIndex = IndexOfHotkey("F10");
                chkPostChar.Checked = true;
                chkKeepFg.Checked = true;
            }
            finally
            {
                _syncing = false;
            }
            lblProfilesPath.Text = "保存目录：" + _profilesDir;
        }

        static int IndexOfHotkey(string name)
        {
            for (int i = 0; i < Hotkeys.Selectable.Length; i++)
                if (string.Equals(Hotkeys.Selectable[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }

        // ==================== 窗口与目标 ====================

        void RefreshWindows()
        {
            if (cbWindows == null) return;
            _syncing = true;
            try
            {
                cbWindows.Items.Clear();
                List<WindowInfo> list = WTarget.Enumerate();
                int self = Process.GetCurrentProcess().Id;
                int selected = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].ProcessId == self) continue;
                    cbWindows.Items.Add(list[i]);
                    if (_target != IntPtr.Zero && list[i].Handle == _target)
                        selected = cbWindows.Items.Count - 1;
                }
                if (selected >= 0) cbWindows.SelectedIndex = selected;
            }
            finally
            {
                _syncing = false;
            }
            Log(string.Format("窗口列表已刷新，共 {0} 个候选窗口", cbWindows.Items.Count));
        }

        void OnWindowSelected()
        {
            if (_syncing) return;
            WindowInfo wi = cbWindows.SelectedItem as WindowInfo;
            if (wi == null) return;
            BindTarget(wi.Handle);
        }

        void BindTarget(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                UnbindTarget();
                return;
            }
            _target = hwnd;
            _targetInfo = WTarget.FromHandle(hwnd);
            lblTarget.Text = "已绑定：" + Trim(WTarget.Describe(hwnd), 60);
            Log("已绑定目标窗口：" + WTarget.Describe(hwnd));
            UpdateStatus();
        }

        void UnbindTarget()
        {
            _target = IntPtr.Zero;
            _targetInfo = null;
            lblTarget.Text = "未绑定窗口";
            Log("已取消绑定");
            UpdateStatus();
        }

        void StartPick(bool coords)
        {
            if (_pickTimer != null)
            {
                Log("拾取已在进行中，请稍候");
                return;
            }
            if (coords && cbPosMode.SelectedIndex == 2 && _target == IntPtr.Zero)
            {
                Log("拾取「窗口客户区」坐标前请先绑定目标窗口");
                return;
            }
            _pickIsCoords = coords;
            _pickCountdown = 3;
            _pickButton = coords ? btnPickPos : btnPickWindow;
            _pickLabel = _pickButton.Text;
            _pickButton.Text = "3 秒后拾取";
            _pickTimer = new Timer();
            _pickTimer.Interval = 1000;
            _pickTimer.Tick += OnPickTick;
            _pickTimer.Start();
            Log(coords ? "请在 3 秒内把鼠标移到要点击的位置…" : "请在 3 秒内把鼠标移到目标窗口上…");
        }

        void OnPickTick(object sender, EventArgs e)
        {
            _pickCountdown--;
            if (_pickCountdown > 0)
            {
                if (_pickButton != null) _pickButton.Text = _pickCountdown + " 秒后拾取";
                return;
            }
            _pickTimer.Stop();
            _pickTimer.Dispose();
            _pickTimer = null;
            if (_pickButton != null)
            {
                _pickButton.Text = _pickLabel;
                _pickButton = null;
            }
            if (_pickIsCoords) DoPickCoords();
            else DoPickWindow();
        }

        void DoPickWindow()
        {
            WindowInfo wi = WTarget.FromCursor();
            if (wi == null)
            {
                Log("拾取失败：光标下没有可用窗口");
                return;
            }
            if (wi.ProcessId == Process.GetCurrentProcess().Id)
            {
                Log("拾取到的是本工具自己的窗口，请换一个目标");
                return;
            }
            BindTarget(wi.Handle);
        }

        void DoPickCoords()
        {
            Win32.POINT pt;
            if (!Win32.GetCursorPos(out pt))
            {
                Log("拾取坐标失败：读不到光标位置");
                return;
            }
            int mode = cbPosMode.SelectedIndex;
            if (mode == 2)
            {
                if (_target == IntPtr.Zero)
                {
                    Log("未绑定窗口，无法换算客户区坐标");
                    return;
                }
                Win32.POINT c = pt;
                Win32.ScreenToClient(_target, ref c);
                SetNum(numX, c.X);
                SetNum(numY, c.Y);
                Log(string.Format("已拾取窗口客户区坐标：({0}, {1})", c.X, c.Y));
            }
            else
            {
                SetNum(numX, pt.X);
                SetNum(numY, pt.Y);
                if (mode == 0)
                {
                    _syncing = true;
                    cbPosMode.SelectedIndex = 1;
                    _syncing = false;
                }
                Log(string.Format("已拾取屏幕坐标：({0}, {1})", pt.X, pt.Y));
            }
        }

        void Diagnose()
        {
            if (_target == IntPtr.Zero)
            {
                Log("诊断：请先绑定目标窗口");
                return;
            }
            Log("──────── 后台可行性诊断 ────────");
            Log("窗口：" + WTarget.Describe(_target));
            int ms;
            bool alive = WTarget.IsResponsive(_target, out ms);
            Log(alive
                ? string.Format("消息队列：响应正常（{0} ms）", ms)
                : "消息队列：无响应（可能已卡死，或权限隔离）");

            string cls = _targetInfo != null ? _targetInfo.ClassName : Win32.GetWindowClass(_target);
            if (cls.StartsWith("Chrome_WidgetWin", StringComparison.OrdinalIgnoreCase)
                || cls.IndexOf("Electron", StringComparison.OrdinalIgnoreCase) >= 0)
                Log("类型判断：Chromium/Electron 系 → 建议「后台保活」；仍无效请用「前台模拟」");
            else if (cls.IndexOf("Unity", StringComparison.OrdinalIgnoreCase) >= 0
                || cls.IndexOf("Unreal", StringComparison.OrdinalIgnoreCase) >= 0)
                Log("类型判断：Unity/Unreal 引擎 → 后台消息通常无效，请用「前台模拟」");
            else if (cls.StartsWith("WindowsForms10", StringComparison.OrdinalIgnoreCase) || cls == "Notepad")
                Log("类型判断：标准 Win32/WinForms → 「后台消息」通常可靠");
            else
                Log("类型判断：" + cls + " → 先试「后台消息」，无效再试「后台保活」，最后「前台模拟」");

            int cx;
            int cy;
            IntPtr child = InputEngine.DeepestChild(_target, 100, 100, out cx, out cy);
            Log("坐标(100,100)处命中：" + (child == _target ? "主窗口自身" : Win32.GetWindowClass(child)));
            Log("结论：点「运行鼠标动作」看目标有无反应，这是唯一可靠判据。");
            if (!alive) Log("注意：目标无响应时任何投递都必然失败，先确认程序没卡死。");
        }

        // ==================== 动作构造 ====================

        Step BuildMouseStep()
        {
            int ai = Math.Max(0, cbMouseAction.SelectedIndex);
            Step s = new Step();
            s.Pos = (PosMode)Math.Max(0, cbPosMode.SelectedIndex);
            s.X = (int)numX.Value;
            s.Y = (int)numY.Value;
            s.Jitter = (int)numJitter.Value;
            s.HoldMs = (int)numHold.Value;
            s.Notches = (int)numNotches.Value;
            if (ai == 4)
            {
                s.Kind = StepKind.Wheel;
            }
            else
            {
                s.Kind = StepKind.Mouse;
                s.Button = (MouseButtonKind)Math.Max(0, cbMouseButton.SelectedIndex);
                s.Action = (MouseActionKind)ai;
            }
            return s;
        }

        Step BuildKeyStep()
        {
            Step s = new Step();
            s.Kind = StepKind.Key;
            s.Vk = KeyMap.VkOf(cbKey.SelectedItem as string);
            s.Ctrl = chkCtrl.Checked;
            s.Alt = chkAlt.Checked;
            s.Shift = chkShift.Checked;
            s.Win = chkWin.Checked;
            s.HoldMs = (int)numKeyHold.Value;
            return s;
        }

        Step BuildTextStep()
        {
            Step s = new Step();
            s.Kind = StepKind.Text;
            s.Text = txtText.Text;
            s.Pos = (PosMode)Math.Max(0, cbPosMode.SelectedIndex);
            s.X = (int)numX.Value;
            s.Y = (int)numY.Value;
            return s;
        }

        void AddStep(Step s)
        {
            if (s == null) return;
            if (s.Kind == StepKind.Key && s.Vk == 0)
            {
                Log("请先选择主键，再添加键盘动作");
                return;
            }
            if (s.Kind == StepKind.Text && string.IsNullOrEmpty(s.Text))
            {
                Log("请先在「文本」里输入内容，再添加文本动作");
                return;
            }
            _steps.Add(s);
            RefreshStepList();
            Log("已添加动作：" + s.KindText() + " — " + s.ParamText());
        }

        void RefreshStepList()
        {
            lvSteps.BeginUpdate();
            lvSteps.Items.Clear();
            for (int i = 0; i < _steps.Count; i++)
            {
                ListViewItem it = new ListViewItem((i + 1).ToString());
                it.SubItems.Add(_steps[i].KindText());
                it.SubItems.Add(_steps[i].ParamText());
                it.SubItems.Add(_steps[i].IntervalText());
                lvSteps.Items.Add(it);
            }
            lvSteps.EndUpdate();
        }

        void MoveStep(int delta)
        {
            if (lvSteps.SelectedIndices.Count == 0) return;
            int i = lvSteps.SelectedIndices[0];
            int j = i + delta;
            if (j < 0 || j >= _steps.Count) return;
            Step t = _steps[i];
            _steps[i] = _steps[j];
            _steps[j] = t;
            RefreshStepList();
            lvSteps.Items[j].Selected = true;
            lvSteps.Items[j].Focused = true;
        }

        void DeleteSelectedSteps()
        {
            if (lvSteps.SelectedIndices.Count == 0) return;
            List<int> idx = new List<int>();
            for (int i = 0; i < lvSteps.SelectedIndices.Count; i++) idx.Add(lvSteps.SelectedIndices[i]);
            idx.Sort();
            for (int i = idx.Count - 1; i >= 0; i--) _steps.RemoveAt(idx[i]);
            RefreshStepList();
            Log(string.Format("已删除 {0} 个动作", idx.Count));
        }

        void ClearSteps()
        {
            if (_steps.Count == 0) return;
            if (MessageBox.Show(this, "确定清空全部动作？", "键鼠宏工坊",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            _steps.Clear();
            RefreshStepList();
            Log("已清空动作列表");
        }

        void DuplicateStep()
        {
            if (lvSteps.SelectedIndices.Count == 0) return;
            int i = lvSteps.SelectedIndices[0];
            _steps.Insert(i + 1, _steps[i].Clone());
            RefreshStepList();
            lvSteps.Items[i + 1].Selected = true;
        }

        void SetSelectedInterval()
        {
            if (lvSteps.SelectedIndices.Count == 0)
            {
                Log("请先在列表里选中一个动作");
                return;
            }
            int i = lvSteps.SelectedIndices[0];
            using (IntervalDialog d = new IntervalDialog(_steps[i].IntervalMin, _steps[i].IntervalMax))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                _steps[i].IntervalMin = d.MinValue;
                _steps[i].IntervalMax = d.MaxValue;
                RefreshStepList();
                Log("已设置单步间隔：" + _steps[i].IntervalText());
            }
        }

        // ==================== 运行控制 ====================

        Profile UiToProfile()
        {
            Profile p = new Profile();
            p.Engine = rbKeepAlive.Checked
                ? EngineKind.KeepAlive
                : (rbForeground.Checked ? EngineKind.Foreground : EngineKind.Background);
            if (_targetInfo != null)
            {
                p.TargetTitle = _targetInfo.Title;
                p.TargetClass = _targetInfo.ClassName;
            }
            p.IntervalFixed = rbFixed.Checked;
            if (rbFixed.Checked)
            {
                p.IntervalMin = (int)numFixed.Value;
                p.IntervalMax = p.IntervalMin;
            }
            else
            {
                p.IntervalMin = (int)numMin.Value;
                p.IntervalMax = (int)numMax.Value;
            }
            p.LoopCount = (int)numLoop.Value;
            p.RunSecondsLimit = (int)numLimitSec.Value;
            p.HotkeyStart = HotkeyText(cboHotStart);
            p.HotkeyStop = HotkeyText(cboHotStop);
            p.HotkeyPause = HotkeyText(cboHotPause);
            p.PostChar = chkPostChar.Checked;
            p.KeepForeground = chkKeepFg.Checked;
            p.Steps = new List<Step>();
            for (int i = 0; i < _steps.Count; i++) p.Steps.Add(_steps[i].Clone());
            return p;
        }

        static string HotkeyText(ComboBox c)
        {
            if (c == null || c.SelectedIndex < 0) return "无";
            return Convert.ToString(c.SelectedItem);
        }

        void BeginRun(Step single, string label)
        {
            if (_runner.IsRunning)
            {
                Log("已在运行中，先停止再开始");
                return;
            }
            Profile p = UiToProfile();

            bool needTarget = p.Engine != EngineKind.Foreground;
            if (needTarget && _target == IntPtr.Zero)
            {
                MessageBox.Show(this,
                    "后台消息 / 后台保活必须先绑定目标窗口。\r\n\r\n请用「刷新列表」选择窗口，或点「从光标拾取」。\r\n如果目标窗口必须在前台，请改用「前台模拟」。",
                    "无法开始", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (needTarget && !Win32.IsWindow(_target))
            {
                MessageBox.Show(this, "目标窗口已关闭，请重新绑定。", "无法开始",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Step fallback = single;
            if (fallback == null && p.Steps.Count == 0) fallback = BuildMouseStep();
            if (p.Steps.Count == 0 && fallback == null)
            {
                MessageBox.Show(this, "没有可执行的动作。", "无法开始",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (single != null) p.Steps.Clear();

            if (NeedsTargetForClientCoords(p, fallback) && _target == IntPtr.Zero)
            {
                MessageBox.Show(this,
                    "有动作使用「窗口客户区」坐标，但还没绑定目标窗口。\r\n请先绑定窗口，或把位置改成「屏幕坐标 / 当前光标」。",
                    "无法开始", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string what = single != null ? label
                : (p.Steps.Count > 0 ? string.Format("宏序列（{0} 个动作）", p.Steps.Count) : "当前鼠标动作");
            Log(string.Format("准备运行：{0} · 引擎 {1}", what, Names.EngineName(p.Engine)));
            lblCount.Text = "操作 0";
            lblElapsed.Text = "时长 00:00:00";
            _runner.Start(p, fallback, _target);
        }

        static bool NeedsTargetForClientCoords(Profile p, Step fallback)
        {
            List<Step> all = new List<Step>();
            if (p.Steps != null) all.AddRange(p.Steps);
            if (fallback != null) all.Add(fallback);
            for (int i = 0; i < all.Count; i++)
                if (all[i].Pos == PosMode.Client) return true;
            return false;
        }

        void StopRun(string who)
        {
            if (!_runner.IsRunning) return;
            Log("收到停止指令（" + who + "）");
            _runner.RequestStop();
        }

        void TogglePause()
        {
            if (!_runner.IsRunning) return;
            bool paused = _runner.TogglePause();
            Log(paused ? "已暂停" : "已继续");
        }

        void OnState(string state)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnState), new object[] { state });
                return;
            }
            if (state == "running")
            {
                lblState.Text = "● 运行中";
                lblState.ForeColor = Theme.Green;
                btnStart.Enabled = false;
                btnPause.Text = "‖  暂停 (F10)";
                ShowTray("开始运行");
            }
            else if (state == "paused")
            {
                lblState.Text = "● 已暂停";
                lblState.ForeColor = Theme.Purple;
                btnPause.Text = "▶  继续 (F10)";
            }
            else
            {
                lblState.Text = "● 已停止";
                lblState.ForeColor = Theme.Muted;
                btnStart.Enabled = true;
                btnPause.Text = "‖  暂停 (F10)";
                lblCount.Text = "操作 " + _runner.Count;
                if (_tray != null) _tray.Visible = false;
            }
        }

        void OnProgress(long count, TimeSpan elapsed)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<long, TimeSpan>(OnProgress), new object[] { count, elapsed });
                return;
            }
            lblCount.Text = "操作 " + count;
            lblElapsed.Text = "时长 " + elapsed.ToString(@"hh\:mm\:ss");
        }

        // ==================== 托盘 ====================

        void ShowTray(string tip)
        {
            if (_tray == null)
            {
                _tray = new NotifyIcon();
                _tray.Icon = SystemIcons.Application;
                _tray.Text = "键鼠宏工坊";
                ContextMenu cm = new ContextMenu();
                cm.MenuItems.Add("显示窗口", delegate { ShowFromTray(); });
                cm.MenuItems.Add("停止运行", delegate { _runner.RequestStop(); });
                _tray.ContextMenu = cm;
                _tray.DoubleClick += delegate { ShowFromTray(); };
            }
            _tray.Visible = true;
            _tray.BalloonTipTitle = "键鼠宏工坊";
            _tray.BalloonTipText = tip + "，F12 可急停";
            _tray.ShowBalloonTip(1500);
        }

        void ShowFromTray()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        // ==================== 热键 ====================

        void RegisterHotkeys()
        {
            if (Handle == IntPtr.Zero) return;
            UnregisterHotkeys();
            string start = HotkeyText(cboHotStart);
            string stop = HotkeyText(cboHotStop);
            string pause = HotkeyText(cboHotPause);

            if (!Hotkeys.Register(Handle, Hotkeys.IdToggle, start))
                Log("热键注册失败（可能被占用）：" + start);
            if (stop != start)
            {
                if (!Hotkeys.Register(Handle, Hotkeys.IdStop, stop))
                    Log("热键注册失败（可能被占用）：" + stop);
            }
            if (pause != start && pause != stop)
            {
                if (!Hotkeys.Register(Handle, Hotkeys.IdPause, pause))
                    Log("热键注册失败（可能被占用）：" + pause);
            }
            Hotkeys.Register(Handle, Hotkeys.IdPanic, Hotkeys.PanicKey);
        }

        void UnregisterHotkeys()
        {
            if (Handle == IntPtr.Zero) return;
            Hotkeys.Unregister(Handle, Hotkeys.IdToggle);
            Hotkeys.Unregister(Handle, Hotkeys.IdStop);
            Hotkeys.Unregister(Handle, Hotkeys.IdPause);
            Hotkeys.Unregister(Handle, Hotkeys.IdPanic);
        }

        // ==================== DPI 修正 ====================

        /// <summary>
        /// 手写绝对布局在 150% 缩放下会「字大框小」：字号以 pt 计已随 DPI 放大，
        /// 而像素坐标没变，文字就会溢出。这里按实际 DPI 统一放大整棵控件树，
        /// 字号保持 pt 不动，从而让框和字一起变大。
        /// </summary>
        void ApplyDpiScaling()
        {
            float dpi = 96f;
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpi = g.DpiX;
                }
            }
            catch { }

            _scale = dpi / 96f;
            if (_scale <= 1.001f) return;

            ClientSize = new Size((int)Math.Round(1060 * _scale), (int)Math.Round(820 * _scale));
            MinimumSize = new Size((int)Math.Round(1090 * _scale), (int)Math.Round(800 * _scale));
            ScaleTree(this, _scale);
            ScaleFixedLayout();
        }

        static void ScaleTree(Control parent, float s)
        {
            for (int i = 0; i < parent.Controls.Count; i++)
            {
                Control c = parent.Controls[i];
                Rectangle b = c.Bounds;
                try
                {
                    c.Bounds = new Rectangle(
                        (int)Math.Round(b.X * s),
                        (int)Math.Round(b.Y * s),
                        (int)Math.Round(b.Width * s),
                        (int)Math.Round(b.Height * s));
                }
                catch { }
                ScaleTree(c, s);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == Hotkeys.IdToggle)
                {
                    if (_runner.IsRunning) StopRun("热键");
                    else BeginRun(null, "宏序列");
                }
                else if (id == Hotkeys.IdStop) StopRun("热键");
                else if (id == Hotkeys.IdPause) TogglePause();
                else if (id == Hotkeys.IdPanic) StopRun("F12 急停");
                return;
            }
            base.WndProc(ref m);
        }

        // ==================== 方案文件 ====================

        void SaveProfileFile()
        {
            try
            {
                string name = txtProfileName.Text.Trim();
                if (name.Length == 0) name = "默认方案";
                foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
                if (!Directory.Exists(_profilesDir)) Directory.CreateDirectory(_profilesDir);
                string path = Path.Combine(_profilesDir, name + ".ini");
                UiToProfile().Save(path);
                lblProfilesPath.Text = "已保存：" + path;
                Log("方案已保存：" + path);
            }
            catch (Exception ex)
            {
                Log("保存失败：" + ex.Message);
            }
        }

        void LoadProfileFile()
        {
            try
            {
                if (!Directory.Exists(_profilesDir))
                {
                    Log("还没有保存过方案：" + _profilesDir);
                    return;
                }
                using (OpenFileDialog d = new OpenFileDialog())
                {
                    d.InitialDirectory = _profilesDir;
                    d.Filter = "方案文件 (*.ini)|*.ini|所有文件 (*.*)|*.*";
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    ApplyProfile(Profile.Load(d.FileName));
                    txtProfileName.Text = Path.GetFileNameWithoutExtension(d.FileName);
                    Log("已加载方案：" + d.FileName);
                }
            }
            catch (Exception ex)
            {
                Log("加载失败：" + ex.Message);
            }
        }

        void OpenProfilesFolder()
        {
            try
            {
                if (!Directory.Exists(_profilesDir)) Directory.CreateDirectory(_profilesDir);
                Process.Start("explorer.exe", "\"" + _profilesDir + "\"");
            }
            catch (Exception ex)
            {
                Log("打开目录失败：" + ex.Message);
            }
        }

        void SaveLastProfile()
        {
            try
            {
                if (Directory.Exists(Path.GetDirectoryName(_configPath)) == false)
                    Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
                UiToProfile().Save(_configPath);
            }
            catch { }
        }

        void LoadLastProfile()
        {
            try
            {
                if (!File.Exists(_configPath)) return;
                ApplyProfile(Profile.Load(_configPath));
            }
            catch (Exception ex)
            {
                Log("读取上次配置失败：" + ex.Message);
            }
        }

        void ApplyProfile(Profile p)
        {
            if (p == null) return;
            _syncing = true;
            try
            {
                rbForeground.Checked = p.Engine == EngineKind.Foreground;
                rbBackground.Checked = p.Engine == EngineKind.Background;
                rbKeepAlive.Checked = p.Engine == EngineKind.KeepAlive;

                if (p.IntervalFixed)
                {
                    rbFixed.Checked = true;
                    SetNum(numFixed, p.IntervalMin);
                }
                else
                {
                    rbRandom.Checked = true;
                    SetNum(numMin, p.IntervalMin);
                    SetNum(numMax, p.IntervalMax);
                }
                SetNum(numLoop, p.LoopCount);
                SetNum(numLimitSec, p.RunSecondsLimit);

                SelectHotkey(cboHotStart, p.HotkeyStart);
                SelectHotkey(cboHotStop, p.HotkeyStop);
                SelectHotkey(cboHotPause, p.HotkeyPause);

                chkPostChar.Checked = p.PostChar;
                chkKeepFg.Checked = p.KeepForeground;

                _steps.Clear();
                for (int i = 0; i < p.Steps.Count; i++) _steps.Add(p.Steps[i].Clone());
                RefreshStepList();

                IntPtr h = WTarget.FindByTitle(p.TargetTitle, p.TargetClass);
                if (h != IntPtr.Zero) BindTarget(h);
                else if (!string.IsNullOrEmpty(p.TargetTitle))
                {
                    _target = IntPtr.Zero;
                    _targetInfo = null;
                    lblTarget.Text = "上次绑定的窗口未找到：" + Trim(p.TargetTitle, 40);
                }
            }
            finally
            {
                _syncing = false;
            }
            UpdateEngineHint();
            SyncIntervalEnabled();
            UpdateStatus();
        }

        static void SelectHotkey(ComboBox c, string name)
        {
            if (c == null) return;
            for (int i = 0; i < c.Items.Count; i++)
            {
                if (string.Equals(Convert.ToString(c.Items[i]), name, StringComparison.OrdinalIgnoreCase))
                {
                    c.SelectedIndex = i;
                    return;
                }
            }
            c.SelectedIndex = 0;
        }

        // ==================== 杂项 ====================

        void UpdateEngineHint()
        {
            if (lblEngineHint == null) return;
            if (rbForeground.Checked)
                lblEngineHint.Text = "真实系统输入（SendInput）：任何程序都认，包括游戏。\r\n运行时会激活并独占你的鼠标键盘。";
            else if (rbKeepAlive.Checked)
                lblEngineHint.Text = "保活：把目标窗口移出屏幕（不是最小化），渲染与消息循环保持活跃。\r\n适合「最小化就冻结」的程序；停止后自动还原位置。";
            else
                lblEngineHint.Text = "后台消息（PostMessage）：不抢焦点、可最小化，不影响你干别的。\r\n标准 Win32/WinForms 可靠；Chromium/游戏可能无效。";
        }

        void SyncIntervalEnabled()
        {
            if (numFixed == null) return;
            bool fixedMode = rbFixed.Checked;
            numFixed.Enabled = fixedMode;
            numMin.Enabled = !fixedMode;
            numMax.Enabled = !fixedMode;
        }

        void UpdateStatus()
        {
            if (lblEngineStatus == null) return;
            string eng = rbKeepAlive.Checked ? "后台保活" : (rbForeground.Checked ? "前台模拟" : "后台消息");
            lblEngineStatus.Text = "引擎：" + eng;
            if (_target == IntPtr.Zero) lblTargetStatus.Text = "目标：未绑定";
            else
            {
                string t = _targetInfo != null ? _targetInfo.Title : Win32.GetWindowTitle(_target);
                if (t.Length > 34) t = t.Substring(0, 34) + "…";
                lblTargetStatus.Text = "目标：" + t;
            }
        }

        void Log(string msg)
        {
            if (rtbLog == null) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(Log), new object[] { msg }); }
                catch { }
                return;
            }
            if (rtbLog.TextLength > 160000) rtbLog.Clear();
            rtbLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, msg, Environment.NewLine));
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }

        static void SetNum(NumericUpDown n, int v)
        {
            if (n == null) return;
            decimal d = v;
            if (d < n.Minimum) d = n.Minimum;
            if (d > n.Maximum) d = n.Maximum;
            n.Value = d;
        }

        static string Trim(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        void RestartAsAdmin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(
                    Application.ExecutablePath, string.Empty);
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                Process.Start(psi);
                Close();
            }
            catch (Exception ex)
            {
                Log("提权重启失败（可能点了取消）：" + ex.Message);
            }
        }

        // ==================== DPI 修正 ====================

        /// <summary>
        /// 自动缩放只覆盖控件的 Bounds；表格行高/列宽、Tab 项尺寸、列头宽度需要自己按 DPI 补。
        /// 在 OnShown 里执行，避免和自动缩放叠加。
        /// </summary>
        void ScaleFixedLayout()
        {
            float s = _scale;
            if (s <= 1.001f) return;

            if (_root != null)
            {
                _root.RowStyles[0].Height = 70f * s;
                _root.RowStyles[2].Height = 168f * s;
                _root.RowStyles[3].Height = 32f * s;
            }
            if (_footerTable != null)
            {
                _footerTable.RowStyles[0].Height = 58f * s;
                _footerTable.RowStyles[1].Height = 52f * s;
            }
            if (_statusTable != null)
            {
                _statusTable.ColumnStyles[0].Width = 130f * s;
                _statusTable.ColumnStyles[1].Width = 150f * s;
                _statusTable.ColumnStyles[3].Width = 130f * s;
                _statusTable.ColumnStyles[4].Width = 160f * s;
            }
            if (_tabs != null)
            {
                _tabs.ItemSize = new Size((int)(140 * s), (int)(30 * s));
                _tabs.Padding = new Point((int)(16 * s), (int)(6 * s));
            }
            if (lvSteps != null && lvSteps.Columns.Count == 4)
            {
                lvSteps.Columns[0].Width = (int)(44 * s);
                lvSteps.Columns[1].Width = (int)(86 * s);
                lvSteps.Columns[2].Width = (int)(414 * s);
                lvSteps.Columns[3].Width = (int)(118 * s);
            }
            if (_headerRight != null) _headerRight.Width = (int)(300 * s);
            if (_footerPad != null)
            {
                int h = (int)(14 * s);
                int v = (int)(8 * s);
                _footerPad.Padding = new Padding(h, v, h, v);
            }
            if (_logBody != null)
            {
                _logBody.Padding = new Padding((int)(10 * s), (int)(2 * s), (int)(10 * s), (int)(8 * s));
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try { ScaleFixedLayout(); }
            catch { }
        }

        void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _runner.RequestStop();
            SaveLastProfile();
            UnregisterHotkeys();
            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }
            _runner.Dispose();
        }
    }
}
