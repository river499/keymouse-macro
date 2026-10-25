# 进度交接（2026-10-25 22:05 收工）

## 当前状态

核心输入层已写完，**尚未编译**（下一步第一件事就是编译验证）。

已完成：

- `docs/superpowers/specs/2026-10-25-keymouse-macro-design.md` — 设计规格（三引擎、数据模型、模块结构、验证方案）
- `src/Engine/Win32.cs` — 全部 P/Invoke 声明（含 AttachThreadInput / BringWindowToTop / SendMessageTimeout / ChildWindowFromPointEx / MapWindowPoints / keybd_event）
- `src/Engine/InputEngine.cs` — 前台 `SendInput`（鼠标/按键/UNICODE 文本/滚轮）+ 后台 `PostMessage`（含子控件下钻、scan code lParam 构造、WM_CHAR）
- `src/Engine/WindowTarget.cs` — 窗口枚举/拾取/按标题找回/响应性探测/**三级兜底前台激活**/移出屏幕保活 `WindowMover`

未开始：

- `src/Core/MacroModel.cs` — 方案与动作模型 + 行式配置存读（设计已定，见 spec 第 5 节）
- `src/Core/Runner.cs` — 调度线程（随机间隔、按住时长、循环、暂停、停止、进度上报）
- `src/Core/Hotkeys.cs` — 全局热键（F8/F9/F10 + F12 固定急停）
- `src/UI/Theme.cs`、`src/UI/MainForm.cs`、`src/Program.cs`、`src/app.manifest`
- `build.cmd`
- `tests/TestTarget.cs`、`tests/EngineTest.cs` — 自动化验证（spec 第 7 节有 5 条断言）
- `dist/` 出包、README、图标

## 明天开局顺序

1. 编译现有 3 个 Engine 文件，先消掉语法错误
2. 写 `MacroModel.cs` + `Runner.cs` + `Hotkeys.cs`
3. 写测试 `TestTarget.cs` + `EngineTest.cs`，跑通 spec 第 7 节的 5 条断言（后台点击/最小化命中/保活/键盘/配置往返）
4. 最后做 UI（`Theme.cs` → `MainForm.cs` → `Program.cs`）
5. 出单文件 exe 到 `dist/`，写 README，present 交付

## 编译方式（已验证可用的工具链）

```bat
:: 用系统自带 .NET Framework 4.8 编译器，产物无运行时依赖
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
%CSC% /nologo /target:winexe /optimize+ /out:dist\键鼠宏工坊.exe ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  /win32manifest:src\app.manifest src\*.cs src\Engine\*.cs src\Core\*.cs src\UI\*.cs

:: 测试程序（不引 WinForms，只编 Engine + Core）
%CSC% /nologo /target:exe /out:tests\EngineTest.exe /r:System.dll /r:System.Core.dll ^
  tests\EngineTest.cs src\Engine\*.cs src\Core\MacroModel.cs src\Core\Runner.cs
```

## 关键约束（写代码前必看）

1. **这个 csc 是 C# 5 编译器**：不能用字符串插值 `$"..."`、`?.`、`out var`、表达式体成员 `=>`、自动属性初始化器。全部用 `string.Format` / `delegate` / 显式 `out` 变量。写了 C# 6+ 语法会直接编译失败。
2. `Engine/` 与 `Core/` 不得引用 WinForms —— 测试程序要和主程序共用这两个目录的源码。
3. `Marshal.SizeOf(typeof(INPUT))` 必须在运行时取（x64 下是 40 字节），不能写死。
4. 后台点击必须遵守顺序：`ScreenToClient` → `DeepestChild` 下钻 → 先 `WM_MOUSEMOVE` 再 down/up。

## 环境事实

- 交互目录 `D:\aapinstall\脚本软件` 对当前账户 `sbxg` **只读**（ACL 只给 SYSTEM/Administrators 全控，当前是非提权令牌），所以构建在 `D:\键鼠宏工坊`。
- 若要让成品落到原目录，需先给该目录写权限，或我用管理员权限执行一次复制。
- 交付默认放 `D:\键鼠宏工坊\dist\`。
