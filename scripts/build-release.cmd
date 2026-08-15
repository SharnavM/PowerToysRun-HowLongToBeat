@echo off
setlocal

cd /d "%~dp0.."

echo.
echo ========================================
echo Building HowLongToBeat release
echo ========================================
echo.

if not exist ".venv\Scripts\python.exe" (
    echo ERROR: Python virtual environment not found.
    exit /b 1
)

echo [1/6] Checking versions...
.venv\Scripts\python.exe scripts\sync-version.py --check
if errorlevel 1 exit /b 1

echo.
echo [2/6] Running Python tests...
.venv\Scripts\python.exe -m pytest
if errorlevel 1 exit /b 1

echo.
echo [3/6] Building packaged bridge...
call scripts\build-bridge.cmd
if errorlevel 1 exit /b 1

echo.
echo [4/6] Running plugin tests...
call scripts\test-plugin.cmd
if errorlevel 1 exit /b 1

echo.
echo [5/6] Building Release plugin...
call scripts\build-plugin.cmd Release
if errorlevel 1 exit /b 1

echo.
echo [6/6] Creating release ZIP...
powershell -NoProfile -ExecutionPolicy Bypass ^
    -File scripts\package-release.ps1 ^
    -Architecture x64

if errorlevel 1 exit /b 1

echo.
echo ========================================
echo Release build succeeded
echo ========================================
echo.

dir artifacts\release

endlocal