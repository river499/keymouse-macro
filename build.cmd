@echo off
setlocal
cd /d "%~dp0"

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo [ERROR] csc.exe not found
  exit /b 1
)

if not exist build mkdir build
if not exist dist mkdir dist

set ENG=src\Engine\Win32.cs src\Engine\InputEngine.cs src\Engine\WindowTarget.cs src\Engine\ProcessInfo.cs
set CORE=src\Core\MacroModel.cs src\Core\Runner.cs src\Core\Hotkeys.cs
set UI=src\UI\Theme.cs src\UI\IntervalDialog.cs src\UI\MainForm.cs

echo [1/3] building main exe ...
"%CSC%" /nologo /utf8output /target:winexe /optimize+ /out:dist\KMMacro.exe /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /win32manifest:src\app.manifest /win32icon:assets\app.ico src\Program.cs %ENG% %CORE% %UI%
if errorlevel 1 goto fail

echo [2/3] building test target ...
"%CSC%" /nologo /utf8output /target:winexe /out:tests\KMMacroTestTarget.exe /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll tests\TestTarget.cs %ENG% %CORE%
if errorlevel 1 goto fail

echo [3/3] building automation test ...
"%CSC%" /nologo /utf8output /target:exe /out:tests\EngineTest.exe /r:System.dll /r:System.Core.dll tests\EngineTest.cs %ENG% %CORE%
if errorlevel 1 goto fail

echo.
echo BUILD OK: dist\KMMacro.exe
exit /b 0

:fail
echo.
echo [FAILED] see errors above
exit /b 1
