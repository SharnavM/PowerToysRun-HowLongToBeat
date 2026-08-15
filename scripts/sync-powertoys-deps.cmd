@echo off
setlocal EnableExtensions

cd /d "%~dp0.."

set "ARCH=x64"
set "POWERTOYS_DIR="
set "LAUNCHER="
set "FILE_VERSION="

set "POWERTOYS_VERSION_FILE=%CD%\POWERTOYS_VERSION"

if not exist "%POWERTOYS_VERSION_FILE%" (
    echo ERROR: POWERTOYS_VERSION file not found:
    echo %POWERTOYS_VERSION_FILE%
    exit /b 1
)

set /p EXPECTED_VERSION=<"%POWERTOYS_VERSION_FILE%"

if not defined EXPECTED_VERSION (
    echo ERROR: POWERTOYS_VERSION is empty.
    exit /b 1
)

for /f "tokens=* delims= " %%V in ("%EXPECTED_VERSION%") do (
    set "EXPECTED_VERSION=%%V"
)

echo.
echo ========================================
echo Locating PowerToys
echo ========================================
echo.

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

if not defined POWERTOYS_DIR (
    echo ERROR: Could not locate PowerToys.
    echo.
    echo Set POWERTOYS_INSTALL_DIR to your PowerToys installation directory and retry.
    echo Example:
    echo   set "POWERTOYS_INSTALL_DIR=C:\Path\To\PowerToys"
    exit /b 1
)

echo Found PowerToys:
echo %POWERTOYS_DIR%
echo.

rem PowerToys versions/install layouts may place Wox.Plugin.dll either
rem in the installation root or under modules\launcher.
if exist "%POWERTOYS_DIR%\Wox.Plugin.dll" (
    set "LAUNCHER=%POWERTOYS_DIR%"
)

if not defined LAUNCHER (
    if exist "%POWERTOYS_DIR%\modules\launcher\Wox.Plugin.dll" (
        set "LAUNCHER=%POWERTOYS_DIR%\modules\launcher"
    )
)

if not defined LAUNCHER (
    echo ERROR: PowerToys was found, but Wox.Plugin.dll could not be located.
    echo.
    echo Checked:
    echo   %POWERTOYS_DIR%\Wox.Plugin.dll
    echo   %POWERTOYS_DIR%\modules\launcher\Wox.Plugin.dll
    exit /b 1
)

echo PowerToys Run dependencies:
echo %LAUNCHER%
echo.

for /f "usebackq delims=" %%V in (`
    powershell -NoProfile -Command ^
    "(Get-Item '%LAUNCHER%\Wox.Plugin.dll').VersionInfo.FileVersion"
`) do (
    set "FILE_VERSION=%%V"
)

if not defined FILE_VERSION (
    echo ERROR: Could not read Wox.Plugin.dll version.
    exit /b 1
)

echo Wox.Plugin.dll version:
echo %FILE_VERSION%
echo.

echo %FILE_VERSION% | findstr /b /c:"%EXPECTED_VERSION%" >nul

if errorlevel 1 (
    echo ERROR: Expected PowerToys %EXPECTED_VERSION%.
    echo Found Wox.Plugin.dll version %FILE_VERSION%.
    exit /b 1
)

set "DEST=%CD%\.deps\powertoys\%EXPECTED_VERSION%\%ARCH%"

if exist "%DEST%" (
    rmdir /s /q "%DEST%"
)

mkdir "%DEST%"

if errorlevel 1 (
    echo ERROR: Failed to create dependency destination:
    echo %DEST%
    exit /b 1
)

echo Copying launcher dependencies...
copy /y "%LAUNCHER%\*.dll" "%DEST%\" >nul

if errorlevel 1 (
    echo ERROR: Failed to copy PowerToys dependencies.
    exit /b 1
)

if not exist "%DEST%\Wox.Plugin.dll" (
    echo ERROR: Wox.Plugin.dll was not copied.
    exit /b 1
)

echo.
echo ========================================
echo PowerToys dependencies synced
echo ========================================
echo.
echo PowerToys installation:
echo %POWERTOYS_DIR%
echo.
echo Source:
echo %LAUNCHER%
echo.
echo Destination:
echo %DEST%
echo.

exit /b 0
