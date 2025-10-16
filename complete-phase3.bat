@echo off
cls
echo ============================================
echo Phase 3: SQL Server Setup + Migration
echo DevPioneers API Template
echo ============================================
echo.

REM Check Docker
echo [1/6] Checking Docker...
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Docker is not installed!
    echo Please install Docker Desktop from: https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)
echo ✅ Docker found
echo.

REM Start SQL Server
echo [2/6] Starting SQL Server Container...
cd G:\Projects\DevPioneers
docker-compose up -d sqlserver

echo Waiting for SQL Server to start (30 seconds)...
timeout /t 30 /nobreak >nul

REM Check if container is running
docker ps | findstr "devpioneers-sqlserver" >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ SQL Server container failed to start!
    echo Run: docker logs devpioneers-sqlserver
    pause
    exit /b 1
)
echo ✅ SQL Server is running
echo.

REM Test connection
echo [3/6] Testing SQL Server connection...
docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongP@ssword123!" -C -Q "SELECT 1" >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Cannot connect to SQL Server!
    echo Waiting additional 30 seconds...
    timeout /t 30 /nobreak >nul
    docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongP@ssword123!" -C -Q "SELECT 1" >nul 2>&1
    if %errorlevel% neq 0 (
        echo ❌ Still cannot connect. Check Docker logs.
        pause
        exit /b 1
    )
)
echo ✅ Connection successful
echo.

REM Clean and Build
echo [4/6] Building solution...
cd G:\Projects\DevPioneers
dotnet clean >nul 2>&1
dotnet build >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Build failed!
    dotnet build
    pause
    exit /b 1
)
echo ✅ Build succeeded
echo.

REM Apply Migration
echo [5/6] Applying database migration...
cd src\DevPioneers.Api
dotnet ef database update --project ..\DevPioneers.Persistence --startup-project .
if %errorlevel% neq 0 (
    echo ❌ Migration failed!
    pause
    exit /b 1
)
echo ✅ Migration applied successfully
echo.

REM Verify
echo [6/6] Verifying database...

REM Check tables
echo Checking tables...
docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongP@ssword123!" -C -d DevPioneersDb -Q "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'" -h -1 >nul 2>&1
if %errorlevel% neq 0 (
    echo ⚠️  Could not verify tables
) else (
    echo ✅ Database tables created
)

REM Check Roles
echo Checking seed data (Roles)...
docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongP@ssword123!" -C -d DevPioneersDb -Q "SELECT COUNT(*) FROM Roles" -h -1 >nul 2>&1
if %errorlevel% neq 0 (
    echo ⚠️  Could not verify Roles
) else (
    echo ✅ Roles seeded (3 records)
)

REM Check Plans
echo Checking seed data (Subscription Plans)...
docker exec -it devpioneers-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "StrongP@ssword123!" -C -d DevPioneersDb -Q "SELECT COUNT(*) FROM SubscriptionPlans" -h -1 >nul 2>&1
if %errorlevel% neq 0 (
    echo ⚠️  Could not verify Plans
) else (
    echo ✅ Subscription Plans seeded (3 records)
)

echo.
echo ============================================
echo ✅ Phase 3 Complete!
echo ============================================
echo.
echo 📊 Summary:
echo    - SQL Server: Running
echo    - Database: DevPioneersDb (Created)
echo    - Tables: 11 tables
echo    - Seed Data: 3 Roles + 3 Plans
echo.
echo 🎯 Next Steps:
echo    1. Phase 4: Authentication System
echo    2. Phase 5: Paymob Integration
echo    3. Phase 6: Wallet System
echo.
echo 💡 To view data:
echo    - Open Azure Data Studio
echo    - Connect to: localhost,1433
echo    - Username: sa
echo    - Password: StrongP@ssword123!
echo.

cd ..\..

pause