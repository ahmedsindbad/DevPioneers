@echo off
cls
echo ============================================
echo Creating Application Layer Files - Part 2
echo DevPioneers API Template
echo ============================================
echo.

REM Set base path
set "BASE_PATH=G:\Projects\DevPioneers\src\DevPioneers.Application"

echo 📁 Base Path: %BASE_PATH%
echo.

echo 🚀 Creating Remaining Application Layer Files...
echo.

REM ============================================
REM 1. Common/Behaviors/ (remaining files)
REM ============================================
echo [1/8] Creating remaining Common/Behaviors...

REM CachingBehavior.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Behaviors/CachingBehavior.cs
echo // ============================================
echo using MediatR;
echo using Microsoft.Extensions.Logging;
echo using DevPioneers.Application.Common.Interfaces;
echo.
echo namespace DevPioneers.Application.Common.Behaviors;
echo.
echo /// ^<summary^>
echo /// MediatR pipeline behavior for caching query results
echo /// Only applies to queries that implement ICacheableQuery
echo /// ^</summary^>
echo public class CachingBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : notnull
echo {
echo     private readonly ICacheService _cacheService;
echo     private readonly ILogger^<CachingBehavior^<TRequest, TResponse^>^> _logger;
echo.
echo     public CachingBehavior^(
echo         ICacheService cacheService,
echo         ILogger^<CachingBehavior^<TRequest, TResponse^>^> logger^)
echo     {
echo         _cacheService = cacheService;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request,
echo         RequestHandlerDelegate^<TResponse^> next,
echo         CancellationToken cancellationToken^)
echo     {
echo         // Only cache if request implements ICacheableQuery
echo         if ^(request is not ICacheableQuery cacheableQuery^)
echo         {
echo             return await next^(^);
echo         }
echo.
echo         var cacheKey = cacheableQuery.CacheKey;
echo.
echo         // Try to get from cache
echo         var cachedResponse = await _cacheService.GetAsync^<TResponse^>^(cacheKey, cancellationToken^);
echo         if ^(cachedResponse != null^)
echo         {
echo             _logger.LogDebug^("Cache hit for {CacheKey}", cacheKey^);
echo             return cachedResponse;
echo         }
echo.
echo         // Get from handler
echo         var response = await next^(^);
echo.
echo         // Cache the response
echo         await _cacheService.SetAsync^(cacheKey, response, cacheableQuery.CacheExpiry, cancellationToken^);
echo         
echo         _logger.LogDebug^("Cached response for {CacheKey}", cacheKey^);
echo.
echo         return response;
echo     }
echo }
echo.
echo /// ^<summary^>
echo /// Interface for cacheable queries
echo /// ^</summary^>
echo public interface ICacheableQuery
echo {
echo     string CacheKey { get; }
echo     TimeSpan? CacheExpiry { get; }
echo }
) > "%BASE_PATH%\Common\Behaviors\CachingBehavior.cs"

REM AuthorizationBehavior.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Behaviors/AuthorizationBehavior.cs
echo // ============================================
echo using MediatR;
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Exceptions;
echo.
echo namespace DevPioneers.Application.Common.Behaviors;
echo.
echo /// ^<summary^>
echo /// MediatR pipeline behavior for authorization
echo /// Checks if user has required permissions for protected requests
echo /// ^</summary^>
echo public class AuthorizationBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : notnull
echo {
echo     private readonly ICurrentUserService _currentUserService;
echo.
echo     public AuthorizationBehavior^(ICurrentUserService currentUserService^)
echo     {
echo         _currentUserService = currentUserService;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request,
echo         RequestHandlerDelegate^<TResponse^> next,
echo         CancellationToken cancellationToken^)
echo     {
echo         // Check if request requires authorization
echo         var authorizeAttributes = typeof^(TRequest^)
echo             .GetCustomAttributes^(typeof^(AuthorizeAttribute^), true^)
echo             .Cast^<AuthorizeAttribute^>^(^)
echo             .ToList^(^);
echo.
echo         if ^(!authorizeAttributes.Any^(^)^)
echo         {
echo             return await next^(^);
echo         }
echo.
echo         // Check if user is authenticated
echo         if ^(!_currentUserService.IsAuthenticated^)
echo         {
echo             throw new UnauthorizedException^("Authentication required"^);
echo         }
echo.
echo         return await next^(^);
echo     }
echo }
echo.
echo /// ^<summary^>
echo /// Authorization attribute for commands/queries
echo /// ^</summary^>
echo [AttributeUsage^(AttributeTargets.Class, AllowMultiple = true^)]
echo public class AuthorizeAttribute : Attribute
echo {
echo     public string? Roles { get; set; }
echo     public string? Policy { get; set; }
echo.
echo     public AuthorizeAttribute^(^) { }
echo.
echo     public AuthorizeAttribute^(string roles^)
echo     {
echo         Roles = roles;
echo     }
echo }
) > "%BASE_PATH%\Common\Behaviors\AuthorizationBehavior.cs"

REM TransactionBehavior.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Behaviors/TransactionBehavior.cs
echo // ============================================
echo using MediatR;
echo using Microsoft.Extensions.Logging;
echo using DevPioneers.Application.Common.Interfaces;
echo using Microsoft.EntityFrameworkCore;
echo.
echo namespace DevPioneers.Application.Common.Behaviors;
echo.
echo /// ^<summary^>
echo /// MediatR pipeline behavior for database transactions
echo /// Wraps commands in database transactions for data consistency
echo /// ^</summary^>
echo public class TransactionBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : notnull
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<TransactionBehavior^<TRequest, TResponse^>^> _logger;
echo.
echo     public TransactionBehavior^(
echo         IApplicationDbContext context,
echo         ILogger^<TransactionBehavior^<TRequest, TResponse^>^> logger^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request,
echo         RequestHandlerDelegate^<TResponse^> next,
echo         CancellationToken cancellationToken^)
echo     {
echo         // Only wrap commands in transactions, not queries
echo         var isCommand = typeof^(TRequest^).Name.EndsWith^("Command"^);
echo         
echo         if ^(!isCommand^)
echo         {
echo             return await next^(^);
echo         }
echo.
echo         var requestName = typeof^(TRequest^).Name;
echo.
echo         try
echo         {
echo             if ^(_context is DbContext dbContext^)
echo             {
echo                 var strategy = dbContext.Database.CreateExecutionStrategy^(^);
echo                 
echo                 return await strategy.ExecuteAsync^(async ^(^) =^>
echo                 {
echo                     await using var transaction = await dbContext.Database.BeginTransactionAsync^(cancellationToken^);
echo                     
echo                     _logger.LogInformation^("Begin transaction for {RequestName}", requestName^);
echo.
echo                     try
echo                     {
echo                         var response = await next^(^);
echo                         
echo                         await transaction.CommitAsync^(cancellationToken^);
echo                         
echo                         _logger.LogInformation^("Commit transaction for {RequestName}", requestName^);
echo                         
echo                         return response;
echo                     }
echo                     catch
echo                     {
echo                         _logger.LogInformation^("Rollback transaction for {RequestName}", requestName^);
echo                         throw;
echo                     }
echo                 }^);
echo             }
echo.
echo             return await next^(^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Transaction failed for {RequestName}", requestName^);
echo             throw;
echo         }
echo     }
echo }
) > "%BASE_PATH%\Common\Behaviors\TransactionBehavior.cs"

echo ✅ Common/Behaviors (6/6 files completed)

REM ============================================
REM 2. Common/Models/ (remaining files)
REM ============================================
echo [2/8] Creating remaining Common/Models...

REM PaginatedList.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Models/PaginatedList.cs
echo // ============================================
echo using Microsoft.EntityFrameworkCore;
echo.
echo namespace DevPioneers.Application.Common.Models;
echo.
echo /// ^<summary^>
echo /// Generic paginated list for query results
echo /// ^</summary^>
echo public class PaginatedList^<T^>
echo {
echo     public List^<T^> Items { get; }
echo     public int PageNumber { get; }
echo     public int PageSize { get; }
echo     public int TotalCount { get; }
echo     public int TotalPages { get; }
echo     public bool HasPreviousPage =^> PageNumber ^> 1;
echo     public bool HasNextPage =^> PageNumber ^< TotalPages;
echo.
echo     public PaginatedList^(List^<T^> items, int totalCount, int pageNumber, int pageSize^)
echo     {
echo         Items = items;
echo         TotalCount = totalCount;
echo         PageNumber = pageNumber;
echo         PageSize = pageSize;
echo         TotalPages = ^(int^)Math.Ceiling^(totalCount / ^(double^)pageSize^);
echo     }
echo.
echo     public static async Task^<PaginatedList^<T^>^> CreateAsync^(
echo         IQueryable^<T^> source,
echo         int pageNumber,
echo         int pageSize,
echo         CancellationToken cancellationToken = default^)
echo     {
echo         var totalCount = await source.CountAsync^(cancellationToken^);
echo         var items = await source
echo             .Skip^(^(pageNumber - 1^) * pageSize^)
echo             .Take^(pageSize^)
echo             .ToListAsync^(cancellationToken^);
echo.
echo         return new PaginatedList^<T^>^(items, totalCount, pageNumber, pageSize^);
echo     }
echo.
echo     public ApiMetadata ToMetadata^(^) =^> new^(^)
echo     {
echo         TotalCount = TotalCount,
echo         PageNumber = PageNumber,
echo         PageSize = PageSize,
echo         TotalPages = TotalPages,
echo         HasPreviousPage = HasPreviousPage,
echo         HasNextPage = HasNextPage,
echo         Timestamp = DateTime.UtcNow
echo     };
echo }
) > "%BASE_PATH%\Common\Models\PaginatedList.cs"

REM BaseDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Models/BaseDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Common.Models;
echo.
echo /// ^<summary^>
echo /// Base DTO with common properties
echo /// ^</summary^>
echo public abstract class BaseDto
echo {
echo     public int Id { get; set; }
echo     public DateTime CreatedAtUtc { get; set; }
echo     public DateTime? UpdatedAtUtc { get; set; }
echo }
echo.
echo /// ^<summary^>
echo /// Base auditable DTO
echo /// ^</summary^>
echo public abstract class AuditableDto : BaseDto
echo {
echo     public int? CreatedById { get; set; }
echo     public string? CreatedByName { get; set; }
echo     public int? UpdatedById { get; set; }
echo     public string? UpdatedByName { get; set; }
echo }
echo.
echo /// ^<summary^>
echo /// Pagination request parameters
echo /// ^</summary^>
echo public class PaginationRequest
echo {
echo     private const int MaxPageSize = 100;
echo     private int _pageSize = 20;
echo.
echo     public int PageNumber { get; set; } = 1;
echo     
echo     public int PageSize
echo     {
echo         get =^> _pageSize;
echo         set =^> _pageSize = value ^> MaxPageSize ? MaxPageSize : value;
echo     }
echo.
echo     public string? SortBy { get; set; }
echo     public string? SortDirection { get; set; } = "asc";
echo     public string? SearchTerm { get; set; }
echo }
) > "%BASE_PATH%\Common\Models\BaseDto.cs"

echo ✅ Common/Models (4/4 files completed)

REM ============================================
REM 3. Common/Mappings/
REM ============================================
echo [3/8] Creating Common/Mappings...

mkdir "%BASE_PATH%\Common\Mappings" 2>nul

REM UserMappings.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Mappings/UserMappings.cs
echo // ============================================
echo using DevPioneers.Application.Features.Auth.DTOs;
echo using DevPioneers.Domain.Entities;
echo.
echo namespace DevPioneers.Application.Common.Mappings;
echo.
echo public static class UserMappings
echo {
echo     public static UserProfileDto ToProfileDto^(this User user^)
echo     {
echo         return new UserProfileDto
echo         {
echo             Id = user.Id,
echo             FullName = user.FullName,
echo             Email = user.Email,
echo             Mobile = user.Mobile,
echo             Status = user.Status.ToString^(^),
echo             EmailVerified = user.EmailVerified,
echo             MobileVerified = user.MobileVerified,
echo             TwoFactorEnabled = user.TwoFactorEnabled,
echo             LastLoginAt = user.LastLoginAt,
echo             CreatedAtUtc = user.CreatedAtUtc,
echo             UpdatedAtUtc = user.UpdatedAtUtc
echo         };
echo     }
echo.
echo     public static AuthResponseDto ToAuthResponseDto^(this User user^)
echo     {
echo         return new AuthResponseDto
echo         {
echo             UserId = user.Id,
echo             Email = user.Email,
echo             FullName = user.FullName,
echo             RequiresTwoFactor = user.TwoFactorEnabled,
echo             RequiresEmailVerification = !user.EmailVerified
echo         };
echo     }
echo }
) > "%BASE_PATH%\Common\Mappings\UserMappings.cs"

REM PaymentMappings.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Mappings/PaymentMappings.cs
echo // ============================================
echo using DevPioneers.Application.Features.Payments.DTOs;
echo using DevPioneers.Domain.Entities;
echo.
echo namespace DevPioneers.Application.Common.Mappings;
echo.
echo public static class PaymentMappings
echo {
echo     public static PaymentDto ToDto^(this Payment payment^)
echo     {
echo         return new PaymentDto
echo         {
echo             Id = payment.Id,
echo             UserId = payment.UserId,
echo             Amount = payment.Amount,
echo             Currency = payment.Currency,
echo             Status = payment.Status.ToString^(^),
echo             PaymentMethod = payment.PaymentMethod.ToString^(^),
echo             Description = payment.Description,
echo             PaymobOrderId = payment.PaymobOrderId,
echo             PaymobTransactionId = payment.PaymobTransactionId,
echo             PaidAt = payment.PaidAt,
echo             FailedAt = payment.FailedAt,
echo             FailureReason = payment.FailureReason,
echo             RefundedAt = payment.RefundedAt,
echo             RefundAmount = payment.RefundAmount,
echo             RefundReason = payment.RefundReason,
echo             CreatedAtUtc = payment.CreatedAtUtc,
echo             UpdatedAtUtc = payment.UpdatedAtUtc
echo         };
echo     }
echo }
) > "%BASE_PATH%\Common\Mappings\PaymentMappings.cs"

REM SubscriptionMappings.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Mappings/SubscriptionMappings.cs
echo // ============================================
echo using DevPioneers.Application.Features.Subscriptions.DTOs;
echo using DevPioneers.Domain.Entities;
echo.
echo namespace DevPioneers.Application.Common.Mappings;
echo.
echo public static class SubscriptionMappings
echo {
echo     public static SubscriptionDto ToDto^(this UserSubscription subscription^)
echo     {
echo         return new SubscriptionDto
echo         {
echo             Id = subscription.Id,
echo             UserId = subscription.UserId,
echo             PlanId = subscription.SubscriptionPlanId,
echo             Status = subscription.Status.ToString^(^),
echo             StartDate = subscription.StartDate,
echo             EndDate = subscription.EndDate,
echo             TrialEndDate = subscription.TrialEndDate,
echo             NextBillingDate = subscription.NextBillingDate,
echo             AutoRenewal = subscription.AutoRenewal,
echo             PaymentId = subscription.PaymentId,
echo             CreatedAtUtc = subscription.CreatedAtUtc,
echo             UpdatedAtUtc = subscription.UpdatedAtUtc
echo         };
echo     }
echo.
echo     public static SubscriptionPlanDto ToDto^(this SubscriptionPlan plan^)
echo     {
echo         return new SubscriptionPlanDto
echo         {
echo             Id = plan.Id,
echo             Name = plan.Name,
echo             Description = plan.Description,
echo             Price = plan.Price,
echo             Currency = plan.Currency,
echo             BillingCycle = plan.BillingCycle.ToString^(^),
echo             TrialDurationDays = plan.TrialDurationDays,
echo             Features = plan.Features,
echo             MaxUsers = plan.MaxUsers,
echo             MaxStorageGb = plan.MaxStorageGb,
echo             PointsAwarded = plan.PointsAwarded,
echo             IsActive = plan.IsActive,
echo             DisplayOrder = plan.DisplayOrder,
echo             DiscountPercentage = plan.DiscountPercentage,
echo             CreatedAtUtc = plan.CreatedAtUtc,
echo             UpdatedAtUtc = plan.UpdatedAtUtc
echo         };
echo     }
echo }
) > "%BASE_PATH%\Common\Mappings\SubscriptionMappings.cs"

REM WalletMappings.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Mappings/WalletMappings.cs
echo // ============================================
echo using DevPioneers.Application.Features.Wallet.DTOs;
echo using DevPioneers.Domain.Entities;
echo.
echo namespace DevPioneers.Application.Common.Mappings;
echo.
echo public static class WalletMappings
echo {
echo     public static WalletDto ToDto^(this Wallet wallet^)
echo     {
echo         return new WalletDto
echo         {
echo             Id = wallet.Id,
echo             UserId = wallet.UserId,
echo             Balance = wallet.Balance,
echo             Points = wallet.Points,
echo             Currency = wallet.Currency,
echo             CreatedAtUtc = wallet.CreatedAtUtc,
echo             UpdatedAtUtc = wallet.UpdatedAtUtc
echo         };
echo     }
echo.
echo     public static TransactionDto ToDto^(this WalletTransaction transaction^)
echo     {
echo         return new TransactionDto
echo         {
echo             Id = transaction.Id,
echo             WalletId = transaction.WalletId,
echo             Type = transaction.Type.ToString^(^),
echo             Amount = transaction.Amount,
echo             Currency = transaction.Currency,
echo             BalanceBefore = transaction.BalanceBefore,
echo             BalanceAfter = transaction.BalanceAfter,
echo             Description = transaction.Description,
echo             RelatedEntityType = transaction.RelatedEntityType,
echo             RelatedEntityId = transaction.RelatedEntityId,
echo             TransferToUserId = transaction.TransferToUserId,
echo             CreatedAtUtc = transaction.CreatedAtUtc,
echo             UpdatedAtUtc = transaction.UpdatedAtUtc
echo         };
echo     }
echo }
) > "%BASE_PATH%\Common\Mappings\WalletMappings.cs"

echo ✅ Common/Mappings (4/4 files completed)

REM ============================================
REM 4. Remaining Auth DTOs
REM ============================================
echo [4/8] Creating remaining Auth DTOs...

REM UserProfileDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/UserProfileDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class UserProfileDto : BaseDto
echo {
echo     public string FullName { get; set; } = string.Empty;
echo     public string Email { get; set; } = string.Empty;
echo     public string? Mobile { get; set; }
echo     public string? ProfilePictureUrl { get; set; }
echo     public string Status { get; set; } = string.Empty;
echo     public bool EmailVerified { get; set; }
echo     public bool MobileVerified { get; set; }
echo     public bool TwoFactorEnabled { get; set; }
echo     public DateTime? LastLoginAt { get; set; }
echo     public List^<RoleDto^> Roles { get; set; } = new^(^);
echo     public decimal WalletBalance { get; set; }
echo     public int WalletPoints { get; set; }
echo     public SubscriptionSummaryDto? ActiveSubscription { get; set; }
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\UserProfileDto.cs"

REM RoleDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/RoleDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class RoleDto : BaseDto
echo {
echo     public string Name { get; set; } = string.Empty;
echo     public string? Description { get; set; }
echo     public bool IsSystemRole { get; set; }
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\RoleDto.cs"

REM SubscriptionSummaryDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/SubscriptionSummaryDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class SubscriptionSummaryDto
echo {
echo     public string PlanName { get; set; } = string.Empty;
echo     public DateTime StartDate { get; set; }
echo     public DateTime EndDate { get; set; }
echo     public string Status { get; set; } = string.Empty;
echo     public int DaysRemaining =^> ^(EndDate.Date - DateTime.UtcNow.Date^).Days;
echo     public bool IsExpiringSoon =^> DaysRemaining ^<= 7 ^&^& DaysRemaining ^> 0;
echo     public bool IsExpired =^> DaysRemaining ^< 0;
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\SubscriptionSummaryDto.cs"

REM OtpDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/OtpDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class OtpDto
echo {
echo     public string UserId { get; set; } = string.Empty;
echo     public string Code { get; set; } = string.Empty;
echo     public string Purpose { get; set; } = string.Empty;
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\OtpDto.cs"

REM ChangePasswordDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/ChangePasswordDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class ChangePasswordDto
echo {
echo     public string CurrentPassword { get; set; } = string.Empty;
echo     public string NewPassword { get; set; } = string.Empty;
echo     public string ConfirmPassword { get; set; } = string.Empty;
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\ChangePasswordDto.cs"

REM ResetPasswordDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/ResetPasswordDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class ResetPasswordDto
echo {
echo     public string Email { get; set; } = string.Empty;
echo     public string Token { get; set; } = string.Empty;
echo     public string NewPassword { get; set; } = string.Empty;
echo     public string ConfirmPassword { get; set; } = string.Empty;
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\ResetPasswordDto.cs"

echo ✅ Features/Auth/DTOs (9/9 files completed)

REM ============================================
REM 5. Auth Queries
REM ============================================
echo [5/8] Creating Auth Queries...

mkdir "%BASE_PATH%\Features\Auth\Queries" 2>nul

REM GetUserProfileQuery.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/Queries/GetUserProfileQuery.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Auth.DTOs;
echo using MediatR;
echo.
echo namespace DevPioneers.Application.Features.Auth.Queries;
echo.
echo public record GetUserProfileQuery^(int UserId^) : IRequest^<Result^<UserProfileDto^>^>;
) > "%BASE_PATH%\Features\Auth\Queries\GetUserProfileQuery.cs"

REM GetUserProfileQueryHandler.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/Queries/GetUserProfileQueryHandler.cs
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using DevPioneers.Application.Common.Models;
echo using DevPioneers.Application.Features.Auth.DTOs;
echo using MediatR;
echo using Microsoft.EntityFrameworkCore;
echo using Microsoft.Extensions.Logging;
echo.
echo namespace DevPioneers.Application.Features.Auth.Queries;
echo.
echo public class GetUserProfileQueryHandler : IRequestHandler^<GetUserProfileQuery, Result^<UserProfileDto^>^>
echo {
echo     private readonly IApplicationDbContext _context;
echo     private readonly ILogger^<GetUserProfileQueryHandler^> _logger;
echo.
echo     public GetUserProfileQueryHandler^(
echo         IApplicationDbContext context,
echo         ILogger^<GetUserProfileQueryHandler^> logger^)
echo     {
echo         _context = context;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<Result^<UserProfileDto^>^> Handle^(GetUserProfileQuery request, CancellationToken cancellationToken^)
echo     {
echo         try
echo         {
echo             var user = await _context.Users
echo                 .FirstOrDefaultAsync^(u =^> u.Id == request.UserId, cancellationToken^);
echo.
echo             if ^(user == null^)
echo             {
echo                 return Result^<UserProfileDto^>.Failure^("User not found"^);
echo             }
echo.
echo             var profile = new UserProfileDto
echo             {
echo                 Id = user.Id,
echo                 FullName = user.FullName,
echo                 Email = user.Email,
echo                 Mobile = user.Mobile,
echo                 Status = user.Status.ToString^(^),
echo                 EmailVerified = user.EmailVerified,
echo                 MobileVerified = user.MobileVerified,
echo                 TwoFactorEnabled = user.TwoFactorEnabled,
echo                 LastLoginAt = user.LastLoginAt,
echo                 CreatedAtUtc = user.CreatedAtUtc
echo             };
echo.
echo             return Result^<UserProfileDto^>.Success^(profile^);
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogError^(ex, "Failed to get user profile for user {UserId}", request.UserId^);
echo             return Result^<UserProfileDto^>.Failure^("An error occurred while retrieving user profile"^);
echo         }
echo     }
echo }
) > "%BASE_PATH%\Features\Auth\Queries\GetUserProfileQueryHandler.cs"

echo ✅ Features/Auth/Queries (2/4 files completed)

REM ============================================
REM 6. Remaining Payment DTOs
REM ============================================
echo [6/8] Creating remaining Payment DTOs...

REM PaymentVerificationDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Payments/DTOs/PaymentVerificationDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Payments.DTOs;
echo.
echo public class PaymentVerificationDto
echo {
echo     public int PaymentId { get; set; }
echo     public string PaymobOrderId { get; set; } = string.Empty;
echo     public string? PaymobTransactionId { get; set; }
echo     public bool IsSuccess { get; set; }
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string Status { get; set; } = string.Empty;
echo     public DateTime VerifiedAt { get; set; }
echo     
echo     // Display properties
echo     public string AmountDisplay =^> $"{Amount:C} {Currency}";
echo     public string ResultDisplay =^> IsSuccess ? "✅ Payment Successful" : "❌ Payment Failed";
echo }
) > "%BASE_PATH%\Features\Payments\DTOs\PaymentVerificationDto.cs"

REM RefundDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Payments/DTOs/RefundDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Payments.DTOs;
echo.
echo public class RefundDto
echo {
echo     public int PaymentId { get; set; }
echo     public string RefundId { get; set; } = string.Empty;
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string Reason { get; set; } = string.Empty;
echo     public string Status { get; set; } = string.Empty;
echo     public DateTime ProcessedAt { get; set; }
echo     public int ProcessedByUserId { get; set; }
echo     
echo     // Display properties
echo     public string AmountDisplay =^> $"{Amount:C} {Currency}";
echo }
) > "%BASE_PATH%\Features\Payments\DTOs\RefundDto.cs"

REM CreatePaymentDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Payments/DTOs/CreatePaymentDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Payments.DTOs;
echo.
echo public class CreatePaymentDto
echo {
echo     public int UserId { get; set; }
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = "EGP";
echo     public string Description { get; set; } = string.Empty;
echo     public int? SubscriptionPlanId { get; set; }
echo     public string PaymentMethod { get; set; } = "CreditCard";
echo }
) > "%BASE_PATH%\Features\Payments\DTOs\CreatePaymentDto.cs"

REM PaymentCallbackDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Payments/DTOs/PaymentCallbackDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Payments.DTOs;
echo.
echo public class PaymentCallbackDto
echo {
echo     public string PaymobOrderId { get; set; } = string.Empty;
echo     public string PaymobTransactionId { get; set; } = string.Empty;
echo     public string Status { get; set; } = string.Empty;
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string? Signature { get; set; }
echo     public Dictionary^<string, object^>? AdditionalData { get; set; }
echo }
) > "%BASE_PATH%\Features\Payments\DTOs\PaymentCallbackDto.cs"

echo ✅ Features/Payments/DTOs (6/6 files completed)

REM ============================================
REM 7. Remaining Subscription DTOs
REM ============================================
echo [7/8] Creating remaining Subscription DTOs...

REM CreateSubscriptionDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Subscriptions/DTOs/CreateSubscriptionDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Subscriptions.DTOs;
echo.
echo public class CreateSubscriptionDto
echo {
echo     public int UserId { get; set; }
echo     public int SubscriptionPlanId { get; set; }
echo     public int? PaymentId { get; set; }
echo     public bool AcceptTerms { get; set; }
echo }
) > "%BASE_PATH%\Features\Subscriptions\DTOs\CreateSubscriptionDto.cs"

REM SubscriptionHistoryDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Subscriptions/DTOs/SubscriptionHistoryDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Subscriptions.DTOs;
echo.
echo public class SubscriptionHistoryDto : BaseDto
echo {
echo     public string PlanName { get; set; } = string.Empty;
echo     public decimal Price { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string BillingCycle { get; set; } = string.Empty;
echo     public string Status { get; set; } = string.Empty;
echo     public DateTime StartDate { get; set; }
echo     public DateTime EndDate { get; set; }
echo     public DateTime? CancelledAt { get; set; }
echo     public string? CancellationReason { get; set; }
echo     public int? PaymentId { get; set; }
echo     public bool AutoRenewal { get; set; }
echo     
echo     // Calculated properties
echo     public int DurationDays =^> ^(EndDate.Date - StartDate.Date^).Days;
echo     public bool WasCancelled =^> CancelledAt.HasValue;
echo }
) > "%BASE_PATH%\Features\Subscriptions\DTOs\SubscriptionHistoryDto.cs"

echo ✅ Features/Subscriptions/DTOs (4/4 files completed)

REM ============================================
REM 8. Remaining Wallet DTOs
REM ============================================
echo [8/8] Creating remaining Wallet DTOs...

REM TransferDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/DTOs/TransferDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Wallet.DTOs;
echo.
echo public class TransferDto
echo {
echo     public int FromUserId { get; set; }
echo     public int ToUserId { get; set; }
echo     public int Points { get; set; }
echo     public string Description { get; set; } = string.Empty;
echo }
) > "%BASE_PATH%\Features\Wallet\DTOs\TransferDto.cs"

REM TransferResultDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/DTOs/TransferResultDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Wallet.DTOs;
echo.
echo public class TransferResultDto
echo {
echo     public int FromUserId { get; set; }
echo     public int ToUserId { get; set; }
echo     public int Points { get; set; }
echo     public string Description { get; set; } = string.Empty;
echo     public int FromTransactionId { get; set; }
echo     public int ToTransactionId { get; set; }
echo     public int FromBalanceAfter { get; set; }
echo     public int ToBalanceAfter { get; set; }
echo     public DateTime TransferredAt { get; set; }
echo     
echo     // Display properties
echo     public string PointsDisplay =^> $"{Points:N0} Points";
echo     public string FromBalanceDisplay =^> $"{FromBalanceAfter:N0} Points";
echo     public string ToBalanceDisplay =^> $"{ToBalanceAfter:N0} Points";
echo }
) > "%BASE_PATH%\Features\Wallet\DTOs\TransferResultDto.cs"

REM CreditWalletDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/DTOs/CreditWalletDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Wallet.DTOs;
echo.
echo public class CreditWalletDto
echo {
echo     public int UserId { get; set; }
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = "EGP";
echo     public string Description { get; set; } = string.Empty;
echo     public string? RelatedEntityType { get; set; }
echo     public int? RelatedEntityId { get; set; }
echo }
) > "%BASE_PATH%\Features\Wallet\DTOs\CreditWalletDto.cs"

echo ✅ Features/Wallet/DTOs (5/5 files completed)

REM ============================================
REM Create DependencyInjection.cs
REM ============================================
echo.
echo 🔧 Creating DependencyInjection.cs...

(
echo // ============================================
echo // File: DevPioneers.Application/DependencyInjection.cs
echo // ============================================
echo using DevPioneers.Application.Common.Behaviors;
echo using FluentValidation;
echo using MediatR;
echo using Microsoft.Extensions.DependencyInjection;
echo using System.Reflection;
echo.
echo namespace DevPioneers.Application;
echo.
echo public static class DependencyInjection
echo {
echo     /// ^<summary^>
echo     /// Add Application layer services to DI container
echo     /// ^</summary^>
echo     public static IServiceCollection AddApplication^(this IServiceCollection services^)
echo     {
echo         var assembly = Assembly.GetExecutingAssembly^(^);
echo.
echo         // Add MediatR
echo         services.AddMediatR^(cfg =^>
echo         {
echo             cfg.RegisterServicesFromAssembly^(assembly^);
echo             
echo             // Add pipeline behaviors ^(order matters!^)
echo             cfg.AddBehavior^<AuthorizationBehavior^<,^>^>^(^);
echo             cfg.AddBehavior^<ValidationBehavior^<,^>^>^(^);
echo             cfg.AddBehavior^<CachingBehavior^<,^>^>^(^);
echo             cfg.AddBehavior^<LoggingBehavior^<,^>^>^(^);
echo             cfg.AddBehavior^<PerformanceBehavior^<,^>^>^(^);
echo             cfg.AddBehavior^<TransactionBehavior^<,^>^>^(^);
echo         }^);
echo.
echo         // Add FluentValidation
echo         services.AddValidatorsFromAssembly^(assembly^);
echo.
echo         return services;
echo     }
echo }
) > "%BASE_PATH%\DependencyInjection.cs"

echo ✅ DependencyInjection.cs created

echo.
echo ============================================
echo ✅ Part 2 Complete!
echo ============================================
echo.
echo 📊 Summary - Part 2:
echo    - Common/Behaviors: 3 additional files ^(6 total^)
echo    - Common/Models: 2 additional files ^(4 total^)
echo    - Common/Mappings: 4 files
echo    - Features/Auth/DTOs: 6 additional files ^(9 total^)
echo    - Features/Auth/Queries: 2 files
echo    - Features/Payments/DTOs: 4 additional files ^(6 total^)
echo    - Features/Subscriptions/DTOs: 2 additional files ^(4 total^)
echo    - Features/Wallet/DTOs: 3 additional files ^(5 total^)
echo    - DependencyInjection.cs: 1 file
echo.
echo    Part 2 files created: 27
echo.
echo 🎉 Total Application Layer Files: 41
echo.
echo 📋 Complete File Summary:
echo    ├── Common/Behaviors/ ^(6 files^) ✅
echo    ├── Common/Models/ ^(4 files^) ✅
echo    ├── Common/Mappings/ ^(4 files^) ✅
echo    ├── Features/Auth/DTOs/ ^(9 files^) ✅
echo    ├── Features/Auth/Queries/ ^(2 files^) ✅
echo    ├── Features/Payments/DTOs/ ^(6 files^) ✅
echo    ├── Features/Subscriptions/DTOs/ ^(4 files^) ✅
echo    ├── Features/Wallet/DTOs/ ^(5 files^) ✅
echo    └── DependencyInjection.cs ^(1 file^) ✅
echo.
echo 🎯 Next Steps:
echo    1. Test build: dotnet build
echo    2. Add to Program.cs: services.AddApplication^(^);
echo    3. Ready for Infrastructure Layer!
echo.
echo ✨ Application Layer Setup Complete! ✨
echo.

pause@echo off
cls
echo ============================================
echo Creating Application Layer Files - Part 2
echo