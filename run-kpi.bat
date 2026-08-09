@echo off
setlocal EnableExtensions

set "REPO_ROOT=%~dp0"
if "%REPO_ROOT:~-1%"=="\" set "REPO_ROOT=%REPO_ROOT:~0,-1%"
set "KPI_URL=http://localhost:5080"

pushd "%REPO_ROOT%" >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Cannot open repository directory: "%REPO_ROOT%"
    pause
    exit /b 1
)

where powershell.exe >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Windows PowerShell is required to run the repository harness.
    popd
    pause
    exit /b 1
)

if not exist "%REPO_ROOT%\harness.cmd" (
    echo [ERROR] harness.cmd was not found. Run this file from the repository root.
    popd
    pause
    exit /b 1
)

echo ============================================================
echo   KPI Management - local launcher
echo ============================================================
echo.
echo [1/3] Bootstrapping the repository with the canonical harness...
call "%REPO_ROOT%\harness.cmd" bootstrap
if errorlevel 1 (
    echo.
    echo [ERROR] Bootstrap failed. The web application was not started.
    popd
    pause
    exit /b 1
)

set "DOTNET_EXE="
for /f "delims=" %%D in ('where dotnet 2^>nul') do if not defined DOTNET_EXE set "DOTNET_EXE=%%D"
if not defined DOTNET_EXE if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET_EXE=%ProgramFiles%\dotnet\dotnet.exe"
if not defined DOTNET_EXE if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" set "DOTNET_EXE=%ProgramFiles(x86)%\dotnet\dotnet.exe"
if not defined DOTNET_EXE (
    echo [ERROR] .NET SDK was not found after bootstrap.
    popd
    pause
    exit /b 1
)

echo.
echo [2/3] Starting KPI Web at %KPI_URL% ...
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=%KPI_URL%"
start "KPI Web" /D "%REPO_ROOT%" "%DOTNET_EXE%" run --project "%REPO_ROOT%\src\Kpi.Web\Kpi.Web.csproj" --configuration Release --no-restore

echo [3/3] Waiting for the local web host...
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "$url = '%KPI_URL%'; for ($i = 0; $i -lt 60; $i++) { try { $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2; if ($response.StatusCode -ge 200) { exit 0 } } catch { }; Start-Sleep -Milliseconds 500 }; exit 1"
if errorlevel 1 (
    echo.
    echo [ERROR] The web host did not become ready within 30 seconds.
    echo Check the separate KPI Web console for the startup error.
    popd
    pause
    exit /b 1
)

echo Opening %KPI_URL% in the default browser...
start "" "%KPI_URL%/"
echo.
echo KPI Management is running. Close the separate KPI Web window or press Ctrl+C there to stop it.
popd
endlocal
exit /b 0
