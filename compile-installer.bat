@echo off
setlocal
set "ROOT=%~dp0"
set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC (echo [ERROR] Inno Setup 6 ISCC.exe was not found.& pause& exit /b 1)
if not exist "%ROOT%publish\BrightGrammarSchoolPortal.exe" (echo [ERROR] Run build.bat first.& pause& exit /b 1)
if not exist "%ROOT%publish\wwwroot\index.html" (echo [ERROR] Angular output missing. Run build.bat first.& pause& exit /b 1)
"%ISCC%" "%ROOT%installer.iss"
if errorlevel 1 (echo [ERROR] Inno Setup compilation failed.& pause& exit /b 1)
echo Installer created in "%ROOT%output"
pause
endlocal
