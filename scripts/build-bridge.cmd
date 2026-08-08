@echo off
setlocal

cd /d "%~dp0.."

echo.
echo ========================================
echo Building HowLongToBeat Python bridge
echo ========================================
echo.

set "PYTHON=%CD%\.venv\Scripts\python.exe"

if not exist "%PYTHON%" (
    echo ERROR: Python virtual environment not found:
    echo %PYTHON%
    echo.
    echo Create it with:
    echo python -m venv .venv
    exit /b 1
)

echo Using:
"%PYTHON%" --version

echo.
echo Checking PyInstaller...
"%PYTHON%" -m PyInstaller --version

if errorlevel 1 (
    echo ERROR: PyInstaller is not installed.
    exit /b 1
)

echo.
echo Cleaning previous bridge artifacts...

if exist "artifacts\bridge" (
    rmdir /s /q "artifacts\bridge"
)

if exist "artifacts\pyinstaller" (
    rmdir /s /q "artifacts\pyinstaller"
)

mkdir "artifacts\bridge" >nul 2>&1
mkdir "artifacts\pyinstaller" >nul 2>&1

echo.
echo Running PyInstaller...

"%PYTHON%" -m PyInstaller ^
    --noconfirm ^
    --clean ^
    --distpath "artifacts\bridge" ^
    --workpath "artifacts\pyinstaller\build" ^
    "src\bridge\hltb-bridge.spec"

if errorlevel 1 (
    echo.
    echo ERROR: PyInstaller build failed.
    exit /b 1
)

set "BRIDGE_EXE=%CD%\artifacts\bridge\hltb-bridge\hltb-bridge.exe"

if not exist "%BRIDGE_EXE%" (
    echo.
    echo ERROR: Expected bridge executable was not produced:
    echo %BRIDGE_EXE%
    exit /b 1
)

echo.
echo ========================================
echo Bridge build succeeded
echo ========================================
echo.
echo Executable:
echo %BRIDGE_EXE%
echo.

exit /b 0