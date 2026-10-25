# 键鼠宏工坊 v1.0 — 设计规格

日期：2026-10-25
目标用户：需要「对指定窗口连点 / 跑一套键鼠宏，且不抢前台、目标最小化也能跑」的用户。

## 1. 目标

复刻并扩展「点点精灵-键鼠工具」的核心能力：

- 可调键位（鼠标左/右/中/侧键、键盘单键与组合键、文本）
- 可调随机间隔（固定 or min~max）
- 可调按键时长（按下保持 ms）
- 可绑定**指定窗口**执行；支持前台模拟与后台投递两种工作方式
- 交付免安装单文件 exe

## 2. 非目标（v1.0 明确不做）

- 驱动级 / 内核级输入注入（需签名驱动）
- 图像识别找图找色、OCR
- 多窗口并发批量执行
- 云同步、账号体系

## 3. 技术选型

- C# / .NET Framework 4.8 / WinForms，使用系统自带 `csc.exe` 直接编译
- 产物：单文件 `dist/键鼠宏工坊.exe`（约数百 KB），无需运行时、无第三方依赖、不联网
- 输入层全部 P/Invoke：`SendInput`、`PostMessage`、`EnumWindows`、`RegisterHotKey`

否决方案：.NET 8 自包含单文件（体积 60MB+）、Electron（100MB+）、Python + pywin32（需环境、误报率更高）。

## 4. 三引擎

| 引擎 | 实现 | 前台 | 后台 | 最小化 | 适用 |
|---|---|---|---|---|---|
| 前台模拟 Foreground | `SendInput` + 激活目标窗口 | 是 | 需前置 | 否 | 游戏、反作弊客户端、一切程序 |
| 后台消息 Background | `PostMessage` 到 HWND + 客户区坐标 + 子控件投递 | 不影响 | 是 | 多数 | 标准 Win32/WinForms、部分浏览器、模拟器 |
| 后台保活 KeepAlive | `PostMessage` + 目标窗口移出屏幕（不真最小化） | 不影响 | 是 | 是 | 最小化会冻结的程序、Chromium 系 |

### 4.1 前台激活流程（三级兜底）

Windows 有前台锁（前台锁定超时内禁止后台进程抢焦点），因此：

1. `ShowWindow(SW_RESTORE)` → `SetForegroundWindow`
2. 失败则 `AttachThreadInput(自身线程, 目标线程, true)` → `BringWindowToTop` + `SetForegroundWindow` + `SetFocus` → 解挂
3. 仍失败则模拟一次 `Alt` 键抖动绕过前台锁
4. 始终用 `GetForegroundWindow()` 读回校验，结果写入日志

可选「保持目标窗口在前台」：每轮循环前校验并重新激活。

### 4.2 后台投递细节

- 屏幕坐标 → `ScreenToClient` → `ChildWindowFromPointEx` 逐层下钻到最深层子窗口，再 `MapWindowPoints` 换算坐标
- 点击前先补一条 `WM_MOUSEMOVE`（部分控件需要 hover 才响应）
- 键盘按 `MapVirtualKey` 生成 scan code 与扩展键标志，构造合法 `lParam`
- 后台按键可选同时补 `WM_CHAR`（文本输入类控件只认 WM_CHAR）

### 4.3 保活模式

运行开始时：若最小化则先恢复，记录原窗口矩形，`SetWindowPos` 移到 `x = -32000`；停止时还原原始位置。移出屏幕的窗口不会进入最小化挂起状态，渲染与消息循环保持活跃。

## 5. 数据模型

```text
Profile（方案）
  engine, targetTitle, targetClass
  intervalFixed, intervalMin, intervalMax
  loopCount(0=无限), runSecondsLimit(0=不限制)
  hotkeyStart/Stop/Pause, postChar, keepForeground
  Steps: List<Step>

Step（动作）
  Kind: Mouse | Key | Text | Delay | Wheel
  Mouse: Button, Action(单击/双击/按下/抬起), PosMode(光标/屏幕/客户区), X, Y, Jitter, HoldMs
  Key:   Vk, Ctrl, Alt, Shift, Win, HoldMs
  Text:  Text
  Delay: DelayMs
  Wheel: Notches
  IntervalMin/Max（0 = 跟随全局间隔）
```

方案文件为可手改的行式配置（`key=value`，`step=` 行以 `;` 分隔字段，支持转义）。

## 6. 模块结构

```text
src/Program.cs              入口、单实例、高 DPI、全局异常兜底
src/UI/MainForm.cs          界面与交互（键鼠 / 宏序列 / 高级）
src/UI/Theme.cs             配色与控件工厂
src/Engine/Win32.cs         P/Invoke 声明
src/Engine/InputEngine.cs   三引擎输入原语
src/Engine/WindowTarget.cs  窗口枚举/拾取/激活/诊断/移出屏幕
src/Core/MacroModel.cs      方案与动作模型 + 存读
src/Core/Runner.cs          调度线程（随机间隔、按住时长、循环、停止、暂停）
src/Core/Hotkeys.cs         全局热键
tests/TestTarget.cs         验证用目标窗口（把收到的消息写入日志）
tests/EngineTest.cs         自动化验证
```

约束：`Engine/` 与 `Core/` 不引用 WinForms，因此可被 GUI 与测试程序共同编译。

## 7. 验证

`tests/EngineTest.cs` 自动化执行并断言：

1. 后台引擎对非前台窗口投递 100 次点击 → 目标日志收到 100 条 `MOUSEDOWN`
2. 目标最小化后投递 50 次 → 记录命中数（用于说明最小化场景的实际表现）
3. 保活模式（窗口移出屏幕）投递 50 次 → 收到 50 条
4. 后台键盘投递 20 次 `A` → 收到 20 条 `KEYDOWN`
5. 方案文件存取往返一致

前台引擎因会真实占用键鼠，采用手动验证（界面内「开始」后观察），并在日志中回读校验结果。

## 8. 风险

- 反作弊驱动会拦截带 `LLMHF_INJECTED` 的合成输入 → 仅驱动级方案可解，不在范围内
- Chromium/Electron 系窗口对 `PostMessage` 的响应取决于其实现 → 提供「后台可行性诊断」与保活兜底，不承诺 100%
- 目标程序以管理员权限运行时，本工具需同样提权（界面提供“以管理员身份重启”）
- 交付目录 `D:\aapinstall\脚本软件` 对当前账户只读，故构建在 `D:\键鼠宏工坊`
