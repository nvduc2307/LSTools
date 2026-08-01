@echo off
setlocal

title LSTools Installer Builder

set "CUSTOMER_NAME=%~1"
set "APP_VERSION=%~2"

if not defined CUSTOMER_NAME (
    set /p "CUSTOMER_NAME=Nhap ten khach hang [A]: "
)
if not defined CUSTOMER_NAME set "CUSTOMER_NAME=A"

if not defined APP_VERSION (
    set /p "APP_VERSION=Nhap phien ban [1.0.0]: "
)
if not defined APP_VERSION set "APP_VERSION=1.0.0"

echo.
echo Dang build va bao ve bo cai LSTools bang ConfuserEx2...
echo Khach hang: %CUSTOMER_NAME%
echo Phien ban:  %APP_VERSION%
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass ^
    -File "%~dp0build-installer.ps1" ^
    -CustomerName "%CUSTOMER_NAME%" ^
    -AppVersion "%APP_VERSION%"

set "BUILD_EXIT_CODE=%ERRORLEVEL%"
echo.

if not "%BUILD_EXIT_CODE%"=="0" (
    echo BUILD THAT BAI. Ma loi: %BUILD_EXIT_CODE%
) else (
    echo BUILD THANH CONG.
    echo Bo cai nam trong: "%~dp0dist"
)

echo.
pause
exit /b %BUILD_EXIT_CODE%
