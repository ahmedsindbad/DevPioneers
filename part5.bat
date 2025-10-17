@echo off
REM ============================================
REM إنشاء WalletStatisticsDto المطلوب للـ Queries
REM ============================================
chcp 65001 >nul

echo.
echo 🔧 إنشاء WalletStatisticsDto.cs...

REM Set base path - غير هذا المسار حسب مجلدك
set "BASE_PATH=G:\Projects\DevPioneers\src\DevPioneers.Application"

REM Create DTOs directory if not exists
mkdir "%BASE_PATH%\Features\Wallet\DTOs" 2>nul

(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/DTOs/WalletStatisticsDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Wallet.DTOs;
echo.
echo /// ^<summary^>
echo /// DTO for wallet statistics
echo /// ^</summary^>
echo public class WalletStatisticsDto
echo {
echo     /// ^<summary^>
echo     /// User ID
echo     /// ^</summary^>
echo     public int UserId { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Wallet ID
echo     /// ^</summary^>
echo     public int WalletId { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Current wallet balance
echo     /// ^</summary^>
echo     public decimal CurrentBalance { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Current points
echo     /// ^</summary^>
echo     public int CurrentPoints { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Currency
echo     /// ^</summary^>
echo     public string Currency { get; set; } = "EGP";
echo.
echo     /// ^<summary^>
echo     /// Statistics period start date
echo     /// ^</summary^>
echo     public DateTime PeriodFromDate { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Statistics period end date
echo     /// ^</summary^>
echo     public DateTime PeriodToDate { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Total credits in period
echo     /// ^</summary^>
echo     public decimal TotalCredits { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Total debits in period
echo     /// ^</summary^>
echo     public decimal TotalDebits { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Net amount ^(credits - debits^)
echo     /// ^</summary^>
echo     public decimal NetAmount { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Number of transactions in period
echo     /// ^</summary^>
echo     public int TransactionCount { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Points earned in period
echo     /// ^</summary^>
echo     public int PointsEarned { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Points spent in period
echo     /// ^</summary^>
echo     public int PointsSpent { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Net points ^(earned - spent^)
echo     /// ^</summary^>
echo     public int NetPoints { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Lifetime total earned
echo     /// ^</summary^>
echo     public decimal LifetimeEarned { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Lifetime total spent
echo     /// ^</summary^>
echo     public decimal LifetimeSpent { get; set; }
echo.
echo     /// ^<summary^>
echo     /// Transactions breakdown by type
echo     /// ^</summary^>
echo     public Dictionary^<string, object^> TransactionsByType { get; set; } = new^(^);
echo.
echo     // Display properties
echo     public string CurrentBalanceDisplay =^> $"{CurrentBalance:N2} {Currency}";
echo     public string CurrentPointsDisplay =^> $"{CurrentPoints:N0} Points";
echo     public string TotalCreditsDisplay =^> $"{TotalCredits:N2} {Currency}";
echo     public string TotalDebitsDisplay =^> $"{TotalDebits:N2} {Currency}";
echo     public string NetAmountDisplay =^> $"{NetAmount:N2} {Currency}";
echo     public string PointsEarnedDisplay =^> $"{PointsEarned:N0} Points";
echo     public string PointsSpentDisplay =^> $"{PointsSpent:N0} Points";
echo     public string NetPointsDisplay =^> $"{NetPoints:N0} Points";
echo     public string LifetimeEarnedDisplay =^> $"{LifetimeEarned:N2} {Currency}";
echo     public string LifetimeSpentDisplay =^> $"{LifetimeSpent:N2} {Currency}";
echo     public string PeriodDisplay =^> $"{PeriodFromDate:yyyy-MM-dd} to {PeriodToDate:yyyy-MM-dd}";
echo     public bool IsPositiveBalance =^> CurrentBalance ^> 0;
echo     public bool IsPositiveNet =^> NetAmount ^> 0;
echo     public bool HasTransactions =^> TransactionCount ^> 0;
echo }
) > "%BASE_PATH%\Features\Wallet\DTOs\WalletStatisticsDto.cs"

echo ✅ WalletStatisticsDto.cs تم إنشاؤه بنجاح!
echo.

pause