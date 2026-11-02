@echo off
chcp 65001 >nul
setlocal
rem ============================================================
rem 本机环境专用的推送脚本，规避两个坑：
rem   1) Git Credential Manager 在非交互环境会挂起 -> 清空凭据助手链，改用 gh
rem   2) 本机网络访问不到证书吊销列表 -> schannel 报 CRYPT_E_NO_REVOCATION_CHECK
rem 用法：tools\push.cmd [分支名]   默认 main
rem ============================================================
cd /d "%~dp0.."

set "BRANCH=%~1"
if "%BRANCH%"=="" set "BRANCH=main"

set "GIT=C:\Program Files\Git\cmd\git.exe"
if not exist "%GIT%" set "GIT=git"

"%GIT%" -c credential.helper= -c credential.helper="!gh auth git-credential" -c http.schannelCheckRevoke=false push origin %BRANCH%
if errorlevel 1 (
  echo.
  echo [失败] 先确认已登录：gh auth status
  exit /b 1
)
echo.
echo 推送完成：%BRANCH%
