@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK 10 is required to build Instant Work Supply.
    set "BUILD_EXIT_CODE=1"
    goto :finish
)

dotnet build "%~dp0Source\InstantWorkSupply.csproj" --configuration Release --nologo
set "BUILD_EXIT_CODE=%ERRORLEVEL%"

if "%BUILD_EXIT_CODE%"=="0" (
    echo.
    echo Build succeeded: "%~dp01.7\Assemblies\InstantWorkSupply.dll"
)

:finish
if not "%BUILD_EXIT_CODE%"=="0" echo Build failed with exit code %BUILD_EXIT_CODE%.
if /I not "%~1"=="/nopause" pause
exit /b %BUILD_EXIT_CODE%
