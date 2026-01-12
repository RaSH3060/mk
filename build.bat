@echo off
echo Building MKXL Trainer...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)
echo Build successful!
echo Executable located at: bin\Release\net6.0-windows\MKXLTrainer.exe
pause