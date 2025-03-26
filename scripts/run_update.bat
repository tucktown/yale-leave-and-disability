@echo off
echo ESL Scenario Update Launcher
echo ---------------------------
echo.

:: Check if PowerShell is available
where powershell >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo ERROR: PowerShell not found. Please install PowerShell to run this script.
    pause
    exit /b 1
)

echo Running scenario update script...
echo.

:: Run the PowerShell script with the execution policy bypassed
powershell -ExecutionPolicy Bypass -File "%~dp0UpdateScenarios.ps1"

echo.
if %ERRORLEVEL% neq 0 (
    echo Script execution failed with error code %ERRORLEVEL%
) else (
    echo Script execution completed successfully!
)

echo.
pause 