@echo off
REM ============================================
REM DevPioneers API Template - Wallet Queries Creator
REM ============================================
chcp 65001 >nul

echo.
echo 🚀 إنشاء ملفات Features/Wallet/Queries/
echo ==========================================

REM Set base path - غير هذا المسار حسب مجلدك
set "BASE_PATH=G:\Projects\DevPioneers\src\DevPioneers.Application"

REM Create Queries directory
mkdir "%BASE_PATH%\Features\Wallet\Queries" 2>nul

echo 📁 المجلد: Features\Wallet\Queries\
echo.

REM ============================================
REM GetWalletBalanceQuery.cs
REM ============================================
echo 🔧 إنشاء GetWalletBalanceQuery.cs...

(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Queries/GetWalletBalanceQuery.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Queries;
echo.
echo /// ^<summary^>
echo /// Query to get wallet balance for a specific user
echo /// ^</summary^>
echo public record GetWalletBalanceQuery^(int UserId^) : IRequest^<Result^<WalletDto^>^>;
echo.
echo public class GetWalletBalanceQueryHandler : IRequestHandler^<GetWalletBalanceQuery, Result^<WalletDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<GetWalletBalanceQueryHandler^> _logger;
echo.
echo     public GetWalletBalanceQueryHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<GetWalletBalanceQueryHandler^> logger^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<Result^<WalletDto^>^> Handle^(GetWalletBalanceQuery request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             var wallet = await _context.Wallets
echo                 .AsNoTracking^(^)
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.UserId, cancellationToken^);
echo.
echo             if ^(wallet == null^)
echo             {
echo                 _logger.LogWarning^("Wallet not found for user {UserId}", request.UserId^);
echo                 return Result^<WalletDto^>.Failure^("Wallet not found"^);
echo             }
echo.
echo             var walletDto = new WalletDto
echo             {
echo                 Id = wallet.Id,
echo                 UserId = wallet.UserId,
echo                 Balance = wallet.Balance,
echo                 Points = wallet.Points,
echo                 Currency = wallet.Currency,
echo                 TotalEarned = wallet.TotalEarned,
echo                 TotalSpent = wallet.TotalSpent,
echo                 IsActive = wallet.IsActive,
echo                 CreatedAtUtc = wallet.CreatedAtUtc,
echo                 UpdatedAtUtc = wallet.UpdatedAtUtc
echo             };
echo.
echo             _logger.LogInformation^("Retrieved wallet balance for user {UserId}: {Balance} {Currency}", 
echo                 request.UserId, wallet.Balance, wallet.Currency^);
echo.
echo             return Result^<WalletDto^>.Success^(walletDto^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to get wallet balance for user {UserId}", request.UserId^);
echo             return Result^<WalletDto^>.Failure^("An error occurred while retrieving wallet balance"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Queries\GetWalletBalanceQuery.cs"

echo ✅ GetWalletBalanceQuery.cs تم إنشاؤه

REM ============================================
REM GetTransactionHistoryQuery.cs
REM ============================================
echo 🔧 إنشاء GetTransactionHistoryQuery.cs...

(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Queries/GetTransactionHistoryQuery.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Enums;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Queries;
echo.
echo /// ^<summary^>
echo /// Query to get paginated transaction history for a user's wallet
echo /// ^</summary^>
echo public record GetTransactionHistoryQuery^(
echo     int UserId,
echo     int PageNumber = 1,
echo     int PageSize = 20,
echo     TransactionType? Type = null,
echo     DateTime? FromDate = null,
echo     DateTime? ToDate = null,
echo     string? SearchTerm = null
echo ^) : IRequest^<Result^<PaginatedList^<TransactionDto^>^>^>;
echo.
echo public class GetTransactionHistoryQueryHandler : IRequestHandler^<GetTransactionHistoryQuery, Result^<PaginatedList^<TransactionDto^>^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<GetTransactionHistoryQueryHandler^> _logger;
echo.
echo     public GetTransactionHistoryQueryHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<GetTransactionHistoryQueryHandler^> logger^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<Result^<PaginatedList^<TransactionDto^>^>^> Handle^(GetTransactionHistoryQuery request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             // First, verify wallet exists
echo             var walletExists = await _context.Wallets
echo                 .AsNoTracking^(^)
echo                 .AnyAsync^(w =^> w.UserId == request.UserId, cancellationToken^);
echo.
echo             if ^(!walletExists^)
echo             {
echo                 _logger.LogWarning^("Wallet not found for user {UserId}", request.UserId^);
echo                 return Result^<PaginatedList^<TransactionDto^>^>.Failure^("Wallet not found"^);
echo             }
echo.
echo             // Build query
echo             var query = _context.WalletTransactions
echo                 .AsNoTracking^(^)
echo                 .Include^(t =^> t.Wallet^)
echo                 .Where^(t =^> t.Wallet.UserId == request.UserId^);
echo.
echo             // Apply filters
echo             if ^(request.Type.HasValue^)
echo             {
echo                 query = query.Where^(t =^> t.Type == request.Type.Value^);
echo             }
echo.
echo             if ^(request.FromDate.HasValue^)
echo             {
echo                 query = query.Where^(t =^> t.CreatedAtUtc ^>= request.FromDate.Value^);
echo             }
echo.
echo             if ^(request.ToDate.HasValue^)
echo             {
echo                 query = query.Where^(t =^> t.CreatedAtUtc ^<= request.ToDate.Value^);
echo             }
echo.
echo             if ^(!string.IsNullOrWhiteSpace^(request.SearchTerm^)^)
echo             {
echo                 var searchTerm = request.SearchTerm.Trim^(^).ToLower^(^);
echo                 query = query.Where^(t =^> t.Description.ToLower^(^).Contains^(searchTerm^) ^|^|
echo                                          ^(t.RelatedEntityType != null ^&^& t.RelatedEntityType.ToLower^(^).Contains^(searchTerm^)^)^);
echo             }
echo.
echo             // Order by created date descending ^(newest first^)
echo             query = query.OrderByDescending^(t =^> t.CreatedAtUtc^);
echo.
echo             // Get total count for pagination
echo             var totalCount = await query.CountAsync^(cancellationToken^);
echo.
echo             // Apply pagination
echo             var transactions = await query
echo                 .Skip^(^(request.PageNumber - 1^) * request.PageSize^)
echo                 .Take^(request.PageSize^)
echo                 .Select^(t =^> new TransactionDto
echo                 {
echo                     Id = t.Id,
echo                     WalletId = t.WalletId,
echo                     Type = t.Type,
echo                     Amount = t.Amount,
echo                     Currency = t.Currency,
echo                     Points = t.Points,
echo                     BalanceBefore = t.BalanceBefore,
echo                     BalanceAfter = t.BalanceAfter,
echo                     Description = t.Description,
echo                     RelatedEntityId = t.RelatedEntityId,
echo                     RelatedEntityType = t.RelatedEntityType,
echo                     TransferToUserId = t.TransferToUserId,
echo                     Metadata = t.Metadata,
echo                     CreatedAtUtc = t.CreatedAtUtc
echo                 }^)
echo                 .ToListAsync^(cancellationToken^);
echo.
echo             var paginatedResult = new PaginatedList^<TransactionDto^>^(
echo                 transactions,
echo                 totalCount,
echo                 request.PageNumber,
echo                 request.PageSize^);
echo.
echo             _logger.LogInformation^("Retrieved {Count} transactions for user {UserId} ^(page {PageNumber}^)", 
echo                 transactions.Count, request.UserId, request.PageNumber^);
echo.
echo             return Result^<PaginatedList^<TransactionDto^>^>.Success^(paginatedResult^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to get transaction history for user {UserId}", request.UserId^);
echo             return Result^<PaginatedList^<TransactionDto^>^>.Failure^("An error occurred while retrieving transaction history"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Queries\GetTransactionHistoryQuery.cs"

echo ✅ GetTransactionHistoryQuery.cs تم إنشاؤه

REM ============================================
REM GetWalletStatisticsQuery.cs
REM ============================================
echo 🔧 إنشاء GetWalletStatisticsQuery.cs...

(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Queries/GetWalletStatisticsQuery.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Enums;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Queries;
echo.
echo /// ^<summary^>
echo /// Query to get wallet statistics for a specific period
echo /// ^</summary^>
echo public record GetWalletStatisticsQuery^(
echo     int UserId,
echo     DateTime? FromDate = null,
echo     DateTime? ToDate = null
echo ^) : IRequest^<Result^<WalletStatisticsDto^>^>;
echo.
echo public class GetWalletStatisticsQueryHandler : IRequestHandler^<GetWalletStatisticsQuery, Result^<WalletStatisticsDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<GetWalletStatisticsQueryHandler^> _logger;
echo.
echo     public GetWalletStatisticsQueryHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<GetWalletStatisticsQueryHandler^> logger^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<Result^<WalletStatisticsDto^>^> Handle^(GetWalletStatisticsQuery request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             // Get wallet
echo             var wallet = await _context.Wallets
echo                 .AsNoTracking^(^)
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.UserId, cancellationToken^);
echo.
echo             if ^(wallet == null^)
echo             {
echo                 _logger.LogWarning^("Wallet not found for user {UserId}", request.UserId^);
echo                 return Result^<WalletStatisticsDto^>.Failure^("Wallet not found"^);
echo             }
echo.
echo             // Default date range if not provided ^(last 30 days^)
echo             var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays^(-30^);
echo             var toDate = request.ToDate ?? DateTime.UtcNow;
echo.
echo             // Build transactions query for the period
echo             var transactionsQuery = _context.WalletTransactions
echo                 .AsNoTracking^(^)
echo                 .Where^(t =^> t.WalletId == wallet.Id ^&^& 
echo                             t.CreatedAtUtc ^>= fromDate ^&^& 
echo                             t.CreatedAtUtc ^<= toDate^);
echo.
echo             // Calculate statistics
echo             var totalCredits = await transactionsQuery
echo                 .Where^(t =^> t.Type == TransactionType.Credit ^|^| 
echo                             t.Type == TransactionType.Refund ^|^| 
echo                             t.Type == TransactionType.PointsReward^)
echo                 .SumAsync^(t =^> t.Amount, cancellationToken^);
echo.
echo             var totalDebits = await transactionsQuery
echo                 .Where^(t =^> t.Type == TransactionType.Debit ^|^| 
echo                             t.Type == TransactionType.Transfer ^|^| 
echo                             t.Type == TransactionType.SubscriptionPayment^)
echo                 .SumAsync^(t =^> t.Amount, cancellationToken^);
echo.
echo             var transactionCount = await transactionsQuery.CountAsync^(cancellationToken^);
echo.
echo             var pointsEarned = await transactionsQuery
echo                 .Where^(t =^> t.Points.HasValue ^&^& t.Points ^> 0^)
echo                 .SumAsync^(t =^> t.Points ?? 0, cancellationToken^);
echo.
echo             var pointsSpent = await transactionsQuery
echo                 .Where^(t =^> t.Points.HasValue ^&^& t.Points ^< 0^)
echo                 .SumAsync^(t =^> Math.Abs^(t.Points ?? 0^), cancellationToken^);
echo.
echo             // Get transaction type breakdown
echo             var transactionsByType = await transactionsQuery
echo                 .GroupBy^(t =^> t.Type^)
echo                 .Select^(g =^> new { Type = g.Key, Count = g.Count^(^), Total = g.Sum^(x =^> x.Amount^) }^)
echo                 .ToListAsync^(cancellationToken^);
echo.
echo             var statisticsDto = new WalletStatisticsDto
echo             {
echo                 UserId = request.UserId,
echo                 WalletId = wallet.Id,
echo                 CurrentBalance = wallet.Balance,
echo                 CurrentPoints = wallet.Points,
echo                 Currency = wallet.Currency,
echo                 PeriodFromDate = fromDate,
echo                 PeriodToDate = toDate,
echo                 TotalCredits = totalCredits,
echo                 TotalDebits = totalDebits,
echo                 NetAmount = totalCredits - totalDebits,
echo                 TransactionCount = transactionCount,
echo                 PointsEarned = pointsEarned,
echo                 PointsSpent = pointsSpent,
echo                 NetPoints = pointsEarned - pointsSpent,
echo                 LifetimeEarned = wallet.TotalEarned,
echo                 LifetimeSpent = wallet.TotalSpent,
echo                 TransactionsByType = transactionsByType.ToDictionary^(
echo                     x =^> x.Type.ToString^(^), 
echo                     x =^> new { Count = x.Count, Total = x.Total }^)
echo             };
echo.
echo             _logger.LogInformation^("Retrieved wallet statistics for user {UserId} for period {FromDate} to {ToDate}", 
echo                 request.UserId, fromDate, toDate^);
echo.
echo             return Result^<WalletStatisticsDto^>.Success^(statisticsDto^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to get wallet statistics for user {UserId}", request.UserId^);
echo             return Result^<WalletStatisticsDto^>.Failure^("An error occurred while retrieving wallet statistics"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Queries\GetWalletStatisticsQuery.cs"

echo ✅ GetWalletStatisticsQuery.cs تم إنشاؤه

REM ============================================
REM GetUserWalletsQuery.cs (for admin use)
REM ============================================
echo 🔧 إنشاء GetUserWalletsQuery.cs...

(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Queries/GetUserWalletsQuery.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Queries;
echo.
echo /// ^<summary^>
echo /// Query to get paginated list of user wallets ^(Admin only^)
echo /// ^</summary^>
echo public record GetUserWalletsQuery^(
echo     int PageNumber = 1,
echo     int PageSize = 20,
echo     string? SearchTerm = null,
echo     bool? IsActive = null,
echo     decimal? MinBalance = null,
echo     decimal? MaxBalance = null
echo ^) : IRequest^<Result^<PaginatedList^<WalletDto^>^>^>;
echo.
echo public class GetUserWalletsQueryHandler : IRequestHandler^<GetUserWalletsQuery, Result^<PaginatedList^<WalletDto^>^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<GetUserWalletsQueryHandler^> _logger;
echo.
echo     public GetUserWalletsQueryHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<GetUserWalletsQueryHandler^> logger^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<Result^<PaginatedList^<WalletDto^>^>^> Handle^(GetUserWalletsQuery request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             // Build query
echo             var query = _context.Wallets
echo                 .AsNoTracking^(^)
echo                 .Include^(w =^> w.User^);
echo.
echo             // Apply filters
echo             if ^(request.IsActive.HasValue^)
echo             {
echo                 query = query.Where^(w =^> w.IsActive == request.IsActive.Value^);
echo             }
echo.
echo             if ^(request.MinBalance.HasValue^)
echo             {
echo                 query = query.Where^(w =^> w.Balance ^>= request.MinBalance.Value^);
echo             }
echo.
echo             if ^(request.MaxBalance.HasValue^)
echo             {
echo                 query = query.Where^(w =^> w.Balance ^<= request.MaxBalance.Value^);
echo             }
echo.
echo             if ^(!string.IsNullOrWhiteSpace^(request.SearchTerm^)^)
echo             {
echo                 var searchTerm = request.SearchTerm.Trim^(^).ToLower^(^);
echo                 query = query.Where^(w =^> w.User.Email.ToLower^(^).Contains^(searchTerm^) ^|^|
echo                                          w.User.FirstName.ToLower^(^).Contains^(searchTerm^) ^|^|
echo                                          w.User.LastName.ToLower^(^).Contains^(searchTerm^) ^|^|
echo                                          ^(w.User.Mobile != null ^&^& w.User.Mobile.Contains^(searchTerm^)^)^);
echo             }
echo.
echo             // Order by balance descending
echo             query = query.OrderByDescending^(w =^> w.Balance^)
echo                         .ThenByDescending^(w =^> w.UpdatedAtUtc^);
echo.
echo             // Get total count for pagination
echo             var totalCount = await query.CountAsync^(cancellationToken^);
echo.
echo             // Apply pagination and select
echo             var wallets = await query
echo                 .Skip^(^(request.PageNumber - 1^) * request.PageSize^)
echo                 .Take^(request.PageSize^)
echo                 .Select^(w =^> new WalletDto
echo                 {
echo                     Id = w.Id,
echo                     UserId = w.UserId,
echo                     Balance = w.Balance,
echo                     Points = w.Points,
echo                     Currency = w.Currency,
echo                     TotalEarned = w.TotalEarned,
echo                     TotalSpent = w.TotalSpent,
echo                     IsActive = w.IsActive,
echo                     CreatedAtUtc = w.CreatedAtUtc,
echo                     UpdatedAtUtc = w.UpdatedAtUtc,
echo                     // Include user info for admin
echo                     UserEmail = w.User.Email,
echo                     UserFullName = $"{w.User.FirstName} {w.User.LastName}".Trim^(^),
echo                     UserMobile = w.User.Mobile
echo                 }^)
echo                 .ToListAsync^(cancellationToken^);
echo.
echo             var paginatedResult = new PaginatedList^<WalletDto^>^(
echo                 wallets,
echo                 totalCount,
echo                 request.PageNumber,
echo                 request.PageSize^);
echo.
echo             _logger.LogInformation^("Retrieved {Count} wallets for admin ^(page {PageNumber}^)", 
echo                 wallets.Count, request.PageNumber^);
echo.
echo             return Result^<PaginatedList^<WalletDto^>^>.Success^(paginatedResult^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to get user wallets list"^);
echo             return Result^<PaginatedList^<WalletDto^>^>.Failure^("An error occurred while retrieving user wallets"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Queries\GetUserWalletsQuery.cs"

echo ✅ GetUserWalletsQuery.cs تم إنشاؤه

echo.
echo ✅ تم إنشاء جميع ملفات Wallet Queries بنجاح!
echo.
echo 📊 ملخص الملفات المُنشأة:
echo ================================
echo    ✅ GetWalletBalanceQuery.cs
echo    ✅ GetTransactionHistoryQuery.cs  
echo    ✅ GetWalletStatisticsQuery.cs
echo    ✅ GetUserWalletsQuery.cs
echo.
echo 🎯 المجموع: 4 ملفات Query مكتملة
echo.
echo 💡 الخطوة التالية:
echo    1. اختبر البناء: dotnet build
echo    2. تأكد من تسجيل MediatR في DI Container
echo    3. انتقل لإنشاء Controllers
echo.
echo 🎉 Features/Wallet/Queries مكتملة!
echo.

pause