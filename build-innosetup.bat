@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

echo ========================================
echo EssayOrganizer Inno Setup Build
echo ========================================
echo.
echo This script automatically performs the following:
echo   1. Automatically bump revision version (e.g. 1.1.0.2 -^> 1.1.0.3)
echo   2. Clean temporary publish folder
echo   3. Publish EssayOrganizer.App in Release mode (self-contained)
echo   4. Create installer using Inno Setup (with versioned filename)
echo   5. Clean temporary publish folder and finalize filename
echo.
pause
echo.

REM Save current directory
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

REM Project file settings
set "PUBLISH_DIR=publish_inno"
set "PROJECT_FILE=EssayOrganizer.App\EssayOrganizer.App.csproj"
set "ISS_FILE=EssayOrganizer.iss"
set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo ========================================
echo [1/5] Automatically bumping version...
echo ========================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File .\bump-version.ps1
if errorlevel 1 (
    echo.
    echo Error: Failed to bump version!
    pause
    exit /b 1
)

for /f %%a in ('powershell -NoProfile -Command "(Select-Xml -Path EssayOrganizer.App\EssayOrganizer.App.csproj -XPath '/Project/PropertyGroup/Version').Node.InnerText"') do set "APP_VERSION=%%a"

echo Target Version: %APP_VERSION%
set "SETUP_EXE=Output\EssayOrganizer_Setup_v%APP_VERSION%.exe"
echo Expected Setup Output: %SETUP_EXE%
echo.

echo ========================================
echo [2/5] Cleaning previous build files...
echo ========================================
echo.
if exist "%PUBLISH_DIR%" (
    echo Removing: %PUBLISH_DIR%\
    rmdir /s /q "%PUBLISH_DIR%"
)
echo Done.
echo.

echo ========================================
echo [3/5] Publishing in Release mode...
echo ========================================
echo.
if not exist "%PROJECT_FILE%" (
    echo Error: Project file not found!
    echo File: %PROJECT_FILE%
    echo.
    pause
    exit /b 1
)

dotnet publish "%PROJECT_FILE%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo.
    echo Error: dotnet publish failed!
    pause
    exit /b 1
)
echo Done.
echo.

echo ========================================
echo [4/5] Compiling Inno Setup script...
echo ========================================
echo.

if not exist "%INNO_PATH%" (
    echo Error: Inno Setup compiler ISCC.exe not found!
    echo Path: "!INNO_PATH!"
    echo.
    echo Please make sure Inno Setup is installed at:
    echo   - "C:\Program Files (x86)\Inno Setup 6\"
    echo.
    pause
    exit /b 1
)

if not exist "%ISS_FILE%" (
    echo Error: ISS file not found!
    echo File: %ISS_FILE%
    echo.
    pause
    exit /b 1
)

echo ISCC: %INNO_PATH%
echo ISS : %ISS_FILE%
echo.

"%INNO_PATH%" "%ISS_FILE%"
if errorlevel 1 (
    echo.
    echo Error: Inno Setup compilation failed!
    pause
    exit /b 1
)
echo Done.
echo.

echo ========================================
echo [5/5] Finalizing installer filename...
echo ========================================
echo.

REM Fallback rename if Inno Setup produced unversioned name
if exist "Output\EssayOrganizer_Setup.exe" (
    echo Renaming Output\EssayOrganizer_Setup.exe -^> %SETUP_EXE%
    move /y "Output\EssayOrganizer_Setup.exe" "%SETUP_EXE%" >nul
)

if exist "%PUBLISH_DIR%" (
    echo Removing temporary publish folder: %PUBLISH_DIR%\
    rmdir /s /q "%PUBLISH_DIR%"
)
echo Done.
echo.

echo ========================================
echo Build complete!
echo ========================================
echo.
if exist "%SETUP_EXE%" (
    echo Successfully generated setup file:
    echo   %SETUP_EXE%
) else (
    echo Output directory contents:
    dir Output
)
echo.
pause
