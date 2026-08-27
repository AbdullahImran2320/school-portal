@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "BACKEND_DIR=%ROOT%\Backend"
set "FRONTEND_DIR=%ROOT%\Frontend"
set "OUT=%ROOT%\publish"

if not exist "%BACKEND_DIR%\SchoolPortal.API.csproj" (echo [ERROR] Backend project not found.& exit /b 1)
if not exist "%FRONTEND_DIR%\package.json" (echo [ERROR] Frontend package.json not found.& exit /b 1)
where dotnet >nul 2>&1 || (echo [ERROR] .NET SDK is not installed/in PATH.& exit /b 1)
where node >nul 2>&1 || (echo [ERROR] Node.js is not installed/in PATH.& exit /b 1)
where npm >nul 2>&1 || (echo [ERROR] npm is not installed/in PATH.& exit /b 1)

if exist "%OUT%" rmdir /S /Q "%OUT%"
if exist "%FRONTEND_DIR%\dist" rmdir /S /Q "%FRONTEND_DIR%\dist"
mkdir "%OUT%"

pushd "%FRONTEND_DIR%"
echo.
echo [1/5] Restoring Angular packages...
call npm install
if errorlevel 1 goto :error_frontend

echo.
echo [2/5] Building Bright Grammar School frontend...
call npx ng build --configuration production
if errorlevel 1 goto :error_frontend
popd

echo.
echo [3/5] Publishing Bright Grammar School backend...
dotnet publish "%BACKEND_DIR%\SchoolPortal.API.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%OUT%"
if errorlevel 1 goto :error

set "ANGULAR_DIST=%FRONTEND_DIR%\dist\bright-grammar-school-portal\browser"
if not exist "%ANGULAR_DIST%\index.html" set "ANGULAR_DIST=%FRONTEND_DIR%\dist\bright-grammar-school-portal"
if not exist "%ANGULAR_DIST%\index.html" (echo [ERROR] Angular index.html not found.& goto :error)
if not exist "%OUT%\wwwroot" mkdir "%OUT%\wwwroot"
xcopy /E /I /Y "%ANGULAR_DIST%\*" "%OUT%\wwwroot\" >nul
if not exist "%OUT%\wwwroot\index.html" (echo [ERROR] wwwroot\index.html missing.& goto :error)

if not exist "%OUT%\BrightGrammarSchoolPortal.exe" (echo [ERROR] BrightGrammarSchoolPortal.exe missing.& goto :error)
if not exist "%OUT%\appsettings.json" (echo [ERROR] appsettings.json missing.& goto :error)

echo.
echo ============================================================
echo BUILD SUCCESSFUL - Bright Grammar School Portal
echo Publish folder: "%OUT%"
echo ============================================================
pause
endlocal
exit /b 0

:error_frontend
popd >nul 2>&1
:error
echo.
echo ============================================================
echo BUILD FAILED - fix the error above and run build.bat again.
echo ============================================================
pause
endlocal
exit /b 1
