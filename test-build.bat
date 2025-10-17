@echo off
cls
echo ============================================
echo DevPioneers - Build Test Script
echo ============================================
echo.

echo [1/4] Cleaning solution...
dotnet clean >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Clean failed
    exit /b 1
)
echo ✅ Clean successful
echo.

echo [2/4] Restoring packages...
dotnet restore >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Restore failed
    dotnet restore
    pause
    exit /b 1
)
echo ✅ Restore successful
echo.

echo [3/4] Building solution...
dotnet build --no-restore >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Build failed! Showing detailed errors:
    echo.
    dotnet build --no-restore
    echo.
    echo Common fixes:
    echo 1. Check if all required properties are implemented in MockCurrentUserService
    echo 2. Verify ICurrentUserService interface matches implementation
    echo 3. Make sure all using statements are present
    pause
    exit /b 1
)
echo ✅ Build successful
echo.

echo [4/4] Testing project references...
cd src\DevPioneers.Api
dotnet build --no-restore >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ API project build failed
    dotnet build --no-restore
    pause
    exit /b 1
)
echo ✅ API project builds successfully
echo.

echo ============================================
echo ✅ All tests passed! Ready for migration.
echo ============================================
echo.
echo Next steps:
echo 1. Start SQL Server: docker-compose up -d sqlserver
echo 2. Wait 30 seconds, then run: dotnet ef database update --project ..\DevPioneers.Persistence
echo 3. Start API: dotnet run
echo.
pause