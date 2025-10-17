@echo off
cls
echo ============================================
echo Creating Wallet Commands ^& Queries Files
echo DevPioneers API Template
echo ============================================
echo.

REM Set base path
set "BASE_PATH=G:\Projects\DevPioneers\src\DevPioneers.Application"

echo 📁 Base Path: %BASE_PATH%
echo.

echo 💰 Creating Wallet Features...
echo.

REM ============================================
REM 1. Features/Wallet/Commands/
REM ============================================
echo [1/2] Creating Features/Wallet/Commands...

mkdir "%BASE_PATH%\Features\Wallet\Commands" 2>nul

REM CreditWalletCommand.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/CreditWalletCommand.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using MediatR;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public record CreditWalletCommand^(
echo     int UserId,
echo     decimal Amount,
echo     string Currency,
echo     string Description,
echo     string? RelatedEntityType = null,
echo     int? RelatedEntityId = null
echo ^) : IRequest^<Result^<TransactionDto^>^>;
) > "%BASE_PATH%\Features\Wallet\Commands\CreditWalletCommand.cs"

REM CreditWalletCommandHandler.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/CreditWalletCommandHandler.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Entities;
echo using DevPioneers.Domain.Enums;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public class CreditWalletCommandHandler : IRequestHandler^<CreditWalletCommand, Result^<TransactionDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<CreditWalletCommandHandler^> _logger;
echo     private readonly IDateTime _dateTime;
echo.
echo     public CreditWalletCommandHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<CreditWalletCommandHandler^> logger,
echo         IDateTime dateTime^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo         _dateTime = dateTime;
echo     }
echo.
echo     public async Task^<Result^<TransactionDto^>^> Handle^(CreditWalletCommand request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             if ^(request.Amount ^<= 0^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Amount must be greater than zero"^);
echo             }
echo.
echo             // Get or create wallet
echo             var wallet = await _context.Wallets
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.UserId, cancellationToken^);
echo.
echo             if ^(wallet == null^)
echo             {
echo                 // Create wallet if it doesn't exist
echo                 wallet = new Domain.Entities.Wallet
echo                 {
echo                     UserId = request.UserId,
echo                     Balance = 0,
echo                     Points = 0,
echo                     Currency = request.Currency,
echo                     CreatedAtUtc = _dateTime.UtcNow
echo                 };
echo                 _context.Wallets.Add^(wallet^);
echo                 await _context.SaveChangesAsync^(cancellationToken^);
echo             }
echo.
echo             // Record balance before transaction
echo             var balanceBefore = wallet.Balance;
echo.
echo             // Credit wallet
echo             wallet.Credit^(request.Amount, request.Description^);
echo.
echo             // Create transaction record
echo             var transaction = new WalletTransaction
echo             {
echo                 WalletId = wallet.Id,
echo                 Type = TransactionType.Credit,
echo                 Amount = request.Amount,
echo                 Currency = request.Currency,
echo                 BalanceBefore = balanceBefore,
echo                 BalanceAfter = wallet.Balance,
echo                 Description = request.Description,
echo                 RelatedEntityType = request.RelatedEntityType,
echo                 RelatedEntityId = request.RelatedEntityId,
echo                 CreatedAtUtc = _dateTime.UtcNow
echo             };
echo.
echo             _context.WalletTransactions.Add^(transaction^);
echo             await _context.SaveChangesAsync^(cancellationToken^);
echo.
echo             var transactionDto = new TransactionDto
echo             {
echo                 Id = transaction.Id,
echo                 WalletId = wallet.Id,
echo                 UserId = request.UserId,
echo                 Type = transaction.Type.ToString^(^),
echo                 Amount = transaction.Amount,
echo                 Currency = transaction.Currency,
echo                 BalanceBefore = transaction.BalanceBefore,
echo                 BalanceAfter = transaction.BalanceAfter,
echo                 Description = transaction.Description,
echo                 RelatedEntityType = transaction.RelatedEntityType,
echo                 RelatedEntityId = transaction.RelatedEntityId,
echo                 CreatedAtUtc = transaction.CreatedAtUtc
echo             };
echo.
echo             _logger.LogInformation^("Wallet credited successfully for user {UserId}, amount {Amount} {Currency}", 
echo                 request.UserId, request.Amount, request.Currency^);
echo.
echo             return Result^<TransactionDto^>.Success^(transactionDto^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to credit wallet for user {UserId}", request.UserId^);
echo             return Result^<TransactionDto^>.Failure^("An error occurred while crediting wallet"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Commands\CreditWalletCommandHandler.cs"

REM DebitWalletCommand.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/DebitWalletCommand.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using MediatR;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public record DebitWalletCommand^(
echo     int UserId,
echo     decimal Amount,
echo     string Currency,
echo     string Description,
echo     string? RelatedEntityType = null,
echo     int? RelatedEntityId = null
echo ^) : IRequest^<Result^<TransactionDto^>^>;
) > "%BASE_PATH%\Features\Wallet\Commands\DebitWalletCommand.cs"

REM DebitWalletCommandHandler.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/DebitWalletCommandHandler.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Entities;
echo using DevPioneers.Domain.Enums;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public class DebitWalletCommandHandler : IRequestHandler^<DebitWalletCommand, Result^<TransactionDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<DebitWalletCommandHandler^> _logger;
echo     private readonly IDateTime _dateTime;
echo.
echo     public DebitWalletCommandHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<DebitWalletCommandHandler^> logger,
echo         IDateTime dateTime^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo         _dateTime = dateTime;
echo     }
echo.
echo     public async Task^<Result^<TransactionDto^>^> Handle^(DebitWalletCommand request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             if ^(request.Amount ^<= 0^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Amount must be greater than zero"^);
echo             }
echo.
echo             // Get wallet
echo             var wallet = await _context.Wallets
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.UserId, cancellationToken^);
echo.
echo             if ^(wallet == null^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Wallet not found"^);
echo             }
echo.
echo             // Check sufficient balance
echo             if ^(wallet.Balance ^< request.Amount^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Insufficient wallet balance"^);
echo             }
echo.
echo             // Record balance before transaction
echo             var balanceBefore = wallet.Balance;
echo.
echo             // Debit wallet
echo             wallet.Debit^(request.Amount, request.Description^);
echo.
echo             // Create transaction record
echo             var transaction = new WalletTransaction
echo             {
echo                 WalletId = wallet.Id,
echo                 Type = TransactionType.Debit,
echo                 Amount = request.Amount,
echo                 Currency = request.Currency,
echo                 BalanceBefore = balanceBefore,
echo                 BalanceAfter = wallet.Balance,
echo                 Description = request.Description,
echo                 RelatedEntityType = request.RelatedEntityType,
echo                 RelatedEntityId = request.RelatedEntityId,
echo                 CreatedAtUtc = _dateTime.UtcNow
echo             };
echo.
echo             _context.WalletTransactions.Add^(transaction^);
echo             await _context.SaveChangesAsync^(cancellationToken^);
echo.
echo             var transactionDto = new TransactionDto
echo             {
echo                 Id = transaction.Id,
echo                 WalletId = wallet.Id,
echo                 UserId = request.UserId,
echo                 Type = transaction.Type.ToString^(^),
echo                 Amount = transaction.Amount,
echo                 Currency = transaction.Currency,
echo                 BalanceBefore = transaction.BalanceBefore,
echo                 BalanceAfter = transaction.BalanceAfter,
echo                 Description = transaction.Description,
echo                 RelatedEntityType = request.RelatedEntityType,
echo                 RelatedEntityId = request.RelatedEntityId,
echo                 CreatedAtUtc = transaction.CreatedAtUtc
echo             };
echo.
echo             _logger.LogInformation^("Wallet debited successfully for user {UserId}, amount {Amount} {Currency}", 
echo                 request.UserId, request.Amount, request.Currency^);
echo.
echo             return Result^<TransactionDto^>.Success^(transactionDto^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to debit wallet for user {UserId}", request.UserId^);
echo             return Result^<TransactionDto^>.Failure^("An error occurred while debiting wallet"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Commands\DebitWalletCommandHandler.cs"

REM TransferPointsCommand.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/TransferPointsCommand.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using MediatR;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public record TransferPointsCommand^(
echo     int FromUserId,
echo     int ToUserId,
echo     int Points,
echo     string Description
echo ^) : IRequest^<Result^<TransferResultDto^>^>;
) > "%BASE_PATH%\Features\Wallet\Commands\TransferPointsCommand.cs"

REM TransferPointsCommandHandler.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/TransferPointsCommandHandler.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Entities;
echo using DevPioneers.Domain.Enums;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public class TransferPointsCommandHandler : IRequestHandler^<TransferPointsCommand, Result^<TransferResultDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<TransferPointsCommandHandler^> _logger;
echo     private readonly IDateTime _dateTime;
echo.
echo     public TransferPointsCommandHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<TransferPointsCommandHandler^> logger,
echo         IDateTime dateTime^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo         _dateTime = dateTime;
echo     }
echo.
echo     public async Task^<Result^<TransferResultDto^>^> Handle^(TransferPointsCommand request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             if ^(request.Points ^<= 0^)
echo             {
echo                 return Result^<TransferResultDto^>.Failure^("Points must be greater than zero"^);
echo             }
echo.
echo             if ^(request.FromUserId == request.ToUserId^)
echo             {
echo                 return Result^<TransferResultDto^>.Failure^("Cannot transfer points to yourself"^);
echo             }
echo.
echo             // Get both wallets
echo             var fromWallet = await _context.Wallets
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.FromUserId, cancellationToken^);
echo.
echo             var toWallet = await _context.Wallets
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.ToUserId, cancellationToken^);
echo.
echo             if ^(fromWallet == null^)
echo             {
echo                 return Result^<TransferResultDto^>.Failure^("Source wallet not found"^);
echo             }
echo.
echo             if ^(toWallet == null^)
echo             {
echo                 return Result^<TransferResultDto^>.Failure^("Destination wallet not found"^);
echo             }
echo.
echo             // Check sufficient points
echo             if ^(fromWallet.Points ^< request.Points^)
echo             {
echo                 return Result^<TransferResultDto^>.Failure^("Insufficient points balance"^);
echo             }
echo.
echo             // Record balances before transfer
echo             var fromPointsBefore = fromWallet.Points;
echo             var toPointsBefore = toWallet.Points;
echo.
echo             // Transfer points
echo             fromWallet.RemovePoints^(request.Points, $"Points transferred to user {request.ToUserId}: {request.Description}"^);
echo             toWallet.AddPoints^(request.Points, $"Points received from user {request.FromUserId}: {request.Description}"^);
echo.
echo             // Create transaction records
echo             var fromTransaction = new WalletTransaction
echo             {
echo                 WalletId = fromWallet.Id,
echo                 Type = TransactionType.PointsDebit,
echo                 Amount = request.Points,
echo                 Currency = "PTS",
echo                 BalanceBefore = fromPointsBefore,
echo                 BalanceAfter = fromWallet.Points,
echo                 Description = $"Points transferred to user {request.ToUserId}: {request.Description}",
echo                 TransferToUserId = request.ToUserId,
echo                 CreatedAtUtc = _dateTime.UtcNow
echo             };
echo.
echo             var toTransaction = new WalletTransaction
echo             {
echo                 WalletId = toWallet.Id,
echo                 Type = TransactionType.PointsCredit,
echo                 Amount = request.Points,
echo                 Currency = "PTS",
echo                 BalanceBefore = toPointsBefore,
echo                 BalanceAfter = toWallet.Points,
echo                 Description = $"Points received from user {request.FromUserId}: {request.Description}",
echo                 CreatedAtUtc = _dateTime.UtcNow
echo             };
echo.
echo             _context.WalletTransactions.AddRange^(fromTransaction, toTransaction^);
echo             await _context.SaveChangesAsync^(cancellationToken^);
echo.
echo             var transferResult = new TransferResultDto
echo             {
echo                 FromUserId = request.FromUserId,
echo                 ToUserId = request.ToUserId,
echo                 Points = request.Points,
echo                 Description = request.Description,
echo                 FromTransactionId = fromTransaction.Id,
echo                 ToTransactionId = toTransaction.Id,
echo                 FromBalanceAfter = fromWallet.Points,
echo                 ToBalanceAfter = toWallet.Points,
echo                 TransferredAt = _dateTime.UtcNow
echo             };
echo.
echo             _logger.LogInformation^("Points transferred successfully from user {FromUserId} to user {ToUserId}, points {Points}", 
echo                 request.FromUserId, request.ToUserId, request.Points^);
echo.
echo             return Result^<TransferResultDto^>.Success^(transferResult^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to transfer points from user {FromUserId} to user {ToUserId}", 
echo                 request.FromUserId, request.ToUserId^);
echo             return Result^<TransferResultDto^>.Failure^("An error occurred while transferring points"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Commands\TransferPointsCommandHandler.cs"

REM AddPointsCommand.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/AddPointsCommand.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using MediatR;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public record AddPointsCommand^(
echo     int UserId,
echo     int Points,
echo     string Description,
echo     string? RelatedEntityType = null,
echo     int? RelatedEntityId = null
echo ^) : IRequest^<Result^<TransactionDto^>^>;
) > "%BASE_PATH%\Features\Wallet\Commands\AddPointsCommand.cs"

REM AddPointsCommandHandler.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/AddPointsCommandHandler.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Entities;
echo using DevPioneers.Domain.Enums;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public class AddPointsCommandHandler : IRequestHandler^<AddPointsCommand, Result^<TransactionDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<AddPointsCommandHandler^> _logger;
echo     private readonly IDateTime _dateTime;
echo.
echo     public AddPointsCommandHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<AddPointsCommandHandler^> logger,
echo         IDateTime dateTime^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo         _dateTime = dateTime;
echo     }
echo.
echo     public async Task^<Result^<TransactionDto^>^> Handle^(AddPointsCommand request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             if ^(request.Points ^<= 0^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Points must be greater than zero"^);
echo             }
echo.
echo             // Get or create wallet
echo             var wallet = await _context.Wallets
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.UserId, cancellationToken^);
echo.
echo             if ^(wallet == null^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Wallet not found"^);
echo             }
echo.
echo             // Record points before transaction
echo             var pointsBefore = wallet.Points;
echo.
echo             // Add points
echo             wallet.AddPoints^(request.Points, request.Description^);
echo.
echo             // Create transaction record
echo             var transaction = new WalletTransaction
echo             {
echo                 WalletId = wallet.Id,
echo                 Type = TransactionType.PointsCredit,
echo                 Amount = request.Points,
echo                 Currency = "PTS",
echo                 BalanceBefore = pointsBefore,
echo                 BalanceAfter = wallet.Points,
echo                 Description = request.Description,
echo                 RelatedEntityType = request.RelatedEntityType,
echo                 RelatedEntityId = request.RelatedEntityId,
echo                 CreatedAtUtc = _dateTime.UtcNow
echo             };
echo.
echo             _context.WalletTransactions.Add^(transaction^);
echo             await _context.SaveChangesAsync^(cancellationToken^);
echo.
echo             var transactionDto = new TransactionDto
echo             {
echo                 Id = transaction.Id,
echo                 WalletId = wallet.Id,
echo                 UserId = request.UserId,
echo                 Type = transaction.Type.ToString^(^),
echo                 Amount = transaction.Amount,
echo                 Currency = transaction.Currency,
echo                 BalanceBefore = transaction.BalanceBefore,
echo                 BalanceAfter = transaction.BalanceAfter,
echo                 Description = transaction.Description,
echo                 RelatedEntityType = transaction.RelatedEntityType,
echo                 RelatedEntityId = transaction.RelatedEntityId,
echo                 CreatedAtUtc = transaction.CreatedAtUtc
echo             };
echo.
echo             _logger.LogInformation^("Points added successfully for user {UserId}, points {Points}", 
echo                 request.UserId, request.Points^);
echo.
echo             return Result^<TransactionDto^>.Success^(transactionDto^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to add points for user {UserId}", request.UserId^);
echo             return Result^<TransactionDto^>.Failure^("An error occurred while adding points"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Commands\AddPointsCommandHandler.cs"

REM DeductPointsCommand.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/DeductPointsCommand.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using MediatR;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public record DeductPointsCommand^(
echo     int UserId,
echo     int Points,
echo     string Description,
echo     string? RelatedEntityType = null,
echo     int? RelatedEntityId = null
echo ^) : IRequest^<Result^<TransactionDto^>^>;
) > "%BASE_PATH%\Features\Wallet\Commands\DeductPointsCommand.cs"

REM DeductPointsCommandHandler.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/Commands/DeductPointsCommandHandler.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Entities;
echo using DevPioneers.Domain.Enums;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Wallet.Commands;
echo.
echo public class DeductPointsCommandHandler : IRequestHandler^<DeductPointsCommand, Result^<TransactionDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<DeductPointsCommandHandler^> _logger;
echo     private readonly IDateTime _dateTime;
echo.
echo     public DeductPointsCommandHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<DeductPointsCommandHandler^> logger,
echo         IDateTime dateTime^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo         _dateTime = dateTime;
echo     }
echo.
echo     public async Task^<Result^<TransactionDto^>^> Handle^(DeductPointsCommand request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             if ^(request.Points ^<= 0^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Points must be greater than zero"^);
echo             }
echo.
echo             // Get wallet
echo             var wallet = await _context.Wallets
echo                 .FirstOrDefaultAsync^(w =^> w.UserId == request.UserId, cancellationToken^);
echo.
echo             if ^(wallet == null^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Wallet not found"^);
echo             }
echo.
echo             // Check sufficient points
echo             if ^(wallet.Points ^< request.Points^)
echo             {
echo                 return Result^<TransactionDto^>.Failure^("Insufficient points balance"^);
echo             }
echo.
echo             // Record points before transaction
echo             var pointsBefore = wallet.Points;
echo.
echo             // Deduct points
echo             wallet.RemovePoints^(request.Points, request.Description^);
echo.
echo             // Create transaction record
echo             var transaction = new WalletTransaction
echo             {
echo                 WalletId = wallet.Id,
echo                 Type = TransactionType.PointsDebit,
echo                 Amount = request.Points,
echo                 Currency = "PTS",
echo                 BalanceBefore = pointsBefore,
echo                 BalanceAfter = wallet.Points,
echo                 Description = request.Description,
echo                 RelatedEntityType = request.RelatedEntityType,
echo                 RelatedEntityId = request.RelatedEntityId,
echo                 CreatedAtUtc = _dateTime.UtcNow
echo             };
echo.
echo             _context.WalletTransactions.Add^(transaction^);
echo             await _context.SaveChangesAsync^(cancellationToken^);
echo.
echo             var transactionDto = new TransactionDto
echo             {
echo                 Id = transaction.Id,
echo                 WalletId = wallet.Id,
echo                 UserId = request.UserId,
echo                 Type = transaction.Type.ToString^(^),
echo                 Amount = transaction.Amount,
echo                 Currency = transaction.Currency,
echo                 BalanceBefore = transaction.BalanceBefore,
echo                 BalanceAfter = transaction.BalanceAfter,
echo                 Description = transaction.Description,
echo                 RelatedEntityType = request.RelatedEntityType,
echo                 RelatedEntityId = request.RelatedEntityId,
echo                 CreatedAtUtc = transaction.CreatedAtUtc
echo             };
echo.
echo             _logger.LogInformation^("Points deducted successfully for user {UserId}, points {Points}", 
echo                 request.UserId, request.Points^);
echo.
echo             return Result^<TransactionDto^>.Success^(transactionDto^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to deduct points for user {UserId}", request.UserId^);
echo             return Result^<TransactionDto^>.Failure^("An error occurred while deducting points"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Wallet\Commands\DeductPointsCommandHandler.cs"

echo ✅ Features/Wallet/Commands (12 files completed)