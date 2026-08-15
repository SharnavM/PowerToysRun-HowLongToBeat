@echo off
setlocal EnableExtensions

cd /d "%~dp0.."

set "SOURCE=%CD%\artifacts\plugin\HowLongToBeat"
set "DEST=%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\HowLongToBeat"
set "POWERTOYS_DIR="
set "POWERTOYS_EXE="

echo.
echo ========================================
echo Deploying HowLongToBeat plugin
echo ========================================
echo.

call scripts\build-plugin.cmd

if errorlevel 1 (
    exit /b 1
)

echo.
echo ========================================
echo Locating PowerToys
echo ========================================
echo.

rem Resolve PowerToys before terminating it so a custom installation can be
rem discovered from the currently running process.
rem Resolution order:
rem   1. POWERTOYS_INSTALL_DIR environment override
rem   2. Currently running PowerToys process
rem   3. Standard Program Files installation
rem   4. Standard LocalAppData installation

if defined POWERTOYS_INSTALL_DIR (
    if exist "%POWERTOYS_INSTALL_DIR%\PowerToys.exe" (
        set "POWERTOYS_DIR=%POWERTOYS_INSTALL_DIR%"
    ) else (
        echo WARNING: POWERTOYS_INSTALL_DIR is set, but PowerToys.exe was not found there:
        echo   %POWERTOYS_INSTALL_DIR%
        echo.
    )
)

if not defined POWERTOYS_DIR (
    for /f "usebackq delims=" %%P in (`
        powershell -NoProfile -Command ^
        "$p = Get-Process PowerToys -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Path; if ($p) { Split-Path -Parent $p }"
    `) do (
        if exist "%%P\PowerToys.exe" (
            set "POWERTOYS_DIR=%%P"
        )
    )
)

if not defined POWERTOYS_DIR (
    if exist "%ProgramFiles%\PowerToys\PowerToys.exe" (
        set "POWERTOYS_DIR=%ProgramFiles%\PowerToys"
    )
)

if not defined POWERTOYS_DIR (
    if exist "%LOCALAPPDATA%\PowerToys\PowerToys.exe" (
        set "POWERTOYS_DIR=%LOCALAPPDATA%\PowerToys"
    )
)

if defined POWERTOYS_DIR (
    set "POWERTOYS_EXE=%POWERTOYS_DIR%\PowerToys.exe"
    echo Found PowerToys:
    echo %POWERTOYS_DIR%
    echo.
) else (
    echo WARNING: Could not locate PowerToys automatically.
    echo.
    echo The plugin will still be deployed, but PowerToys cannot be restarted automatically.
    echo For a custom installation, set POWERTOYS_INSTALL_DIR before running this script.
    echo Example:
    echo   set "POWERTOYS_INSTALL_DIR=C:\Path\To\PowerToys"
    echo.
)

echo Closing PowerToys...

taskkill /IM PowerToys.PowerLauncher.exe /F >nul 2>&1
taskkill /IM PowerToys.exe /F >nul 2>&1

timeout /t 1 /nobreak >nul

if exist "%DEST%" (
    rmdir /s /q "%DEST%"
)

mkdir "%DEST%"

if errorlevel 1 (
    echo ERROR: Failed to create plugin destination:
    echo %DEST%
    exit /b 1
)

xcopy /E /I /Y "%SOURCE%\*" "%DEST%\" >nul

if errorlevel 1 (
    echo ERROR: Failed to copy plugin files.
    exit /b 1
)

echo Plugin copied to:
echo %DEST%
echo.

if defined POWERTOYS_EXE (
    if exist "%POWERTOYS_EXE%" (
        echo Starting PowerToys...
        start "" "%POWERTOYS_EXE%"
    ) else (
        echo WARNING: PowerToys.exe is no longer present at the discovered path:
        echo %POWERTOYS_EXE%
        echo Start PowerToys manually.
    )
) else (
    echo Start PowerToys manually.
)

echo.
echo Deployment complete.
echo.

exit /b 0
