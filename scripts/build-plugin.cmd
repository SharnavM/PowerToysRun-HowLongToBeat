@echo off
setlocal EnableExtensions

cd /d "%~dp0.."

set "PROJECT=src\plugin\Community.PowerToys.Run.Plugin.HowLongToBeat\Community.PowerToys.Run.Plugin.HowLongToBeat.csproj"
set "DEPS=.deps\powertoys\0.100.0\x64\Wox.Plugin.dll"
set "OUTPUT=artifacts\plugin\HowLongToBeat"
set "BRIDGE_SOURCE=artifacts\bridge\hltb-bridge"
set "BRIDGE_EXE=%BRIDGE_SOURCE%\hltb-bridge.exe"
set "CONFIGURATION=%~1"

echo.
echo ========================================
echo Building PowerToys Run plugin
echo ========================================
echo.

if not exist "%DEPS%" (
    echo ERROR: PowerToys dependencies not found.
    echo.
    echo Run:
    echo scripts\sync-powertoys-deps.cmd
    exit /b 1
)

if exist "%OUTPUT%" (
    rmdir /s /q "%OUTPUT%"
)

if not exist "%BRIDGE_EXE%" (
    echo ERROR: Packaged HLTB bridge not found.
    echo.
    echo Run:
    echo scripts\build-bridge.cmd
    exit /b 1
)

dotnet build "%PROJECT%" ^
    -p:Platform=x64 ^
    -p:PowerToysArchitecture=x64 ^
    --output "%OUTPUT%" ^
    -c "%CONFIGURATION%"

if errorlevel 1 (
    echo.
    echo ERROR: Plugin build failed.
    exit /b 1
)

if not exist "%OUTPUT%\Community.PowerToys.Run.Plugin.HowLongToBeat.dll" (
    echo.
    echo ERROR: Plugin DLL was not produced.
    exit /b 1
)

if not exist "%OUTPUT%\plugin.json" (
    echo.
    echo ERROR: plugin.json was not produced.
    exit /b 1
)

if "%CONFIGURATION%"=="" (
    set "CONFIGURATION=Debug"
)

echo.
echo Copying packaged HLTB bridge...

xcopy /E /I /Y ^
    "%BRIDGE_SOURCE%\*" ^
    "%OUTPUT%\Bridge\" >nul

if errorlevel 1 (
    echo.
    echo ERROR: Failed to copy HLTB bridge.
    exit /b 1
)

if not exist "%OUTPUT%\Bridge\hltb-bridge.exe" (
    echo.
    echo ERROR: Bridge executable missing from plugin output.
    exit /b 1
)

echo.
echo ========================================
echo Plugin build succeeded
echo ========================================
echo.
echo Output:
echo %CD%\%OUTPUT%
echo.

exit /b 0