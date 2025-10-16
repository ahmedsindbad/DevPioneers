@echo off
cls
echo ============================================
echo Test SQL Server Connection Strings
echo DevPioneers API Template
echo ============================================
echo.

REM Set variables
set SQL_PASSWORD=StrongP@ssword123!
set DATABASE=DevPioneersDb

echo [Step 1] Testing connection inside Docker container...
docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "%SQL_PASSWORD%" -C -Q "SELECT @@VERSION" >nul 2>&1

if %errorlevel% neq 0 (
    echo ❌ Cannot connect inside container!
    echo Container may not be running or password is wrong.
    echo.
    echo Checking container status:
    docker ps | findstr "devpioneers-sqlserver"
    pause
    exit /b 1
) else (
    echo ✅ Connection inside container works!
)
echo.

echo [Step 2] Testing connection from host (127.0.0.1)...
sqlcmd -S 127.0.0.1,1433 -U sa -P "%SQL_PASSWORD%" -Q "SELECT 1" -C >nul 2>&1

if %errorlevel% neq 0 (
    echo ❌ Cannot connect from host to 127.0.0.1,1433
    echo This might be a port mapping issue.
) else (
    echo ✅ Connection from host to 127.0.0.1,1433 works!
)
echo.

echo [Step 3] Testing connection from host (localhost)...
sqlcmd -S localhost,1433 -U sa -P "%SQL_PASSWORD%" -Q "SELECT 1" -C >nul 2>&1

if %errorlevel% neq 0 (
    echo ❌ Cannot connect from host to localhost,1433
) else (
    echo ✅ Connection from host to localhost,1433 works!
)
echo.

echo [Step 4] Checking if database exists...
docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "%SQL_PASSWORD%" -C -Q "SELECT name FROM sys.databases WHERE name='%DATABASE%'" -h -1 | findstr "%DATABASE%" >nul 2>&1

if %errorlevel% neq 0 (
    echo ⚠️  Database '%DATABASE%' does NOT exist!
    echo.
    echo Creating database...
    docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "%SQL_PASSWORD%" -C -Q "CREATE DATABASE %DATABASE%"
    echo ✅ Database created
) else (
    echo ✅ Database '%DATABASE%' exists!
)
echo.

echo [Step 5] Checking port mapping...
docker port devpioneers-sqlserver
echo.

echo [Step 6] Getting container IP address...
for /f "tokens=*" %%i in ('docker inspect -f "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}" devpioneers-sqlserver') do set CONTAINER_IP=%%i
echo Container IP: %CONTAINER_IP%
echo.

echo ============================================
echo Recommended Connection Strings:
echo ============================================
echo.
echo Option 1 (Recommended):
echo Server=127.0.0.1,1433;Database=%DATABASE%;User Id=sa;Password=%SQL_PASSWORD%;Encrypt=false;TrustServerCertificate=true;MultipleActiveResultSets=true
echo.
echo Option 2 (Alternative):
echo Server=localhost,1433;Database=%DATABASE%;User Id=sa;Password=%SQL_PASSWORD%;Encrypt=false;TrustServerCertificate=true;MultipleActiveResultSets=true
echo.
echo Option 3 (Using Container IP):
echo Server=%CONTAINER_IP%;Database=%DATABASE%;User Id=sa;Password=%SQL_PASSWORD%;Encrypt=false;TrustServerCertificate=true;MultipleActiveResultSets=true
echo.

echo ============================================
echo Next Steps:
echo ============================================
echo 1. Update appsettings.Development.json with recommended connection string
echo 2. Run: cd G:\Projects\DevPioneers
echo 3. Run: dotnet clean
echo 4. Run: dotnet build
echo 5. Run: cd src\DevPioneers.Api
echo 6. Run: dotnet ef database update --project ..\DevPioneers.Persistence --startup-project .
echo.

pause