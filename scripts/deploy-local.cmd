@echo off
setlocal EnableExtensions

cd /d "%~dp0.."

set "SOURCE=%CD%\artifacts\plugin\HowLongToBeat"

set "DEST=%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\HowLongToBeat"

echo.
echo ========================================
echo Deploying HowLongToBeat plugin
echo ========================================
echo.

call scripts\build-plugin.cmd

if errorlevel 1 (
    exit /b 1
)

echo Closing PowerToys...

taskkill /IM PowerToys.PowerLauncher.exe /F >nul 2>&1
taskkill /IM PowerToys.exe /F >nul 2>&1

timeout /t 1 /nobreak >nul

if exist "%DEST%" (
    rmdir /s /q "%DEST%"
)

mkdir "%DEST%"

xcopy /E /I /Y "%SOURCE%\*" "%DEST%\" >nul

if errorlevel 1 (
    echo ERROR: Failed to copy plugin files.
    exit /b 1
)

echo Plugin copied to:
echo %DEST%
echo.

set "POWERTOYS_EXE=D:\Program Files\PowerToys\PowerToys.exe"

if exist "%ProgramFiles%\PowerToys\PowerToys.exe" (
    set "POWERTOYS_EXE=%ProgramFiles%\PowerToys\PowerToys.exe"
)

if exist "%LOCALAPPDATA%\PowerToys\PowerToys.exe" (
    set "POWERTOYS_EXE=%LOCALAPPDATA%\PowerToys\PowerToys.exe"
)

if defined POWERTOYS_EXE (
    echo Starting PowerToys...
    start "" "%POWERTOYS_EXE%"
) else (
    echo Could not locate PowerToys.exe automatically.
    echo Start PowerToys manually.
)

echo.
echo Deployment complete.
echo.

exit /b 0