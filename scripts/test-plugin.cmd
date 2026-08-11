@echo off
setlocal EnableExtensions

cd /d "%~dp0.."

set "PROJECT=tests\plugin\Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests\Community.PowerToys.Run.Plugin.HowLongToBeat.UnitTests.csproj"
set "DEPS=.deps\powertoys\0.100.0\x64\Wox.Plugin.dll"
set "BRIDGE_EXE=%CD%\artifacts\bridge\hltb-bridge\hltb-bridge.exe"

echo.
echo ========================================
echo Testing PowerToys Run plugin
echo ========================================
echo.

if not exist "%DEPS%" (
    echo ERROR: PowerToys dependencies not found.
    echo.
    echo Run:
    echo scripts\sync-powertoys-deps.cmd
    exit /b 1
)

if not exist "%BRIDGE_EXE%" (
    echo ERROR: Packaged HLTB bridge not found.
    echo.
    echo Run:
    echo scripts\build-bridge.cmd
    exit /b 1
)

set "HLTB_BRIDGE_EXE=%BRIDGE_EXE%"

dotnet test "%PROJECT%" ^
    -c Debug ^
    -p:Platform=x64 ^
    -p:PowerToysArchitecture=x64

if errorlevel 1 (
    echo.
    echo ERROR: Plugin tests failed.
    exit /b 1
)

echo.
echo ========================================
echo Plugin tests passed
echo ========================================
echo.

exit /b 0