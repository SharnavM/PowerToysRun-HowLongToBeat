@echo off
setlocal EnableExtensions

cd /d "%~dp0.."

set "EXPECTED_VERSION=0.100.0"
set "ARCH=x64"
set "LAUNCHER="

echo.
echo ========================================
echo Locating PowerToys Run
echo ========================================
echo.

for %%D in (
    "D:\Program Files\PowerToys"
) do (
    if not defined LAUNCHER (
        if exist "%%~D\Wox.Plugin.dll" (
            set "LAUNCHER=%%~D"
        )
    )
)

if not defined LAUNCHER (
    echo ERROR: Could not locate PowerToys Run.
    echo.
    echo Checked:
    echo   %ProgramFiles%\PowerToys\modules\launcher
    echo   %LOCALAPPDATA%\PowerToys\modules\launcher
    exit /b 1
)

echo Found:
echo %LAUNCHER%
echo.

for /f "usebackq delims=" %%V in (`
    powershell -NoProfile -Command ^
    "(Get-Item '%LAUNCHER%\Wox.Plugin.dll').VersionInfo.FileVersion"
`) do (
    set "FILE_VERSION=%%V"
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
echo Source:
echo %LAUNCHER%
echo.
echo Destination:
echo %DEST%
echo.

exit /b 0