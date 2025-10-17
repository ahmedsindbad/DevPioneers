@echo off
cls
echo ============================================
echo Creating Application Layer Files - Part 1
echo DevPioneers API Template
echo ============================================
echo.

REM Set base path
set "BASE_PATH=G:\Projects\DevPioneers\src\DevPioneers.Application"

echo 📁 Base Path: %BASE_PATH%
echo.

REM Check if base directory exists
if not exist "%BASE_PATH%" (
    mkdir "%BASE_PATH%"
    echo ✅ Created base directory
)

echo 🚀 Creating Common Layer Files...
echo.

REM ============================================
REM 1. Common/Behaviors/
REM ============================================
echo [1/6] Creating Common/Behaviors...

mkdir "%BASE_PATH%\Common\Behaviors" 2>nul

REM ValidationBehavior.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Behaviors/ValidationBehavior.cs
echo // ============================================
echo using DevPioneers.Application.Common.Exceptions;
echo using FluentValidation;
echo using MediatR;
echo.
echo namespace DevPioneers.Application.Common.Behaviors;
echo.
echo /// ^<summary^>
echo /// MediatR pipeline behavior for automatic validation
echo /// Validates all requests that have validators registered
echo /// ^</summary^>
echo public class ValidationBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : notnull
echo {
echo     private readonly IEnumerable^<IValidator^<TRequest^>^> _validators;
echo.
echo     public ValidationBehavior^(IEnumerable^<IValidator^<TRequest^>^> validators^)
echo     {
echo         _validators = validators;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request,
echo         RequestHandlerDelegate^<TResponse^> next,
echo         CancellationToken cancellationToken^)
echo     {
echo         if ^(!_validators.Any^(^)^)
echo         {
echo             return await next^(^);
echo         }
echo.
echo         var context = new ValidationContext^<TRequest^>^(request^);
echo.
echo         var validationResults = await Task.WhenAll^(
echo             _validators.Select^(v =^> v.ValidateAsync^(context, cancellationToken^)^)^);
echo.
echo         var failures = validationResults
echo             .SelectMany^(r =^> r.Errors^)
echo             .Where^(f =^> f != null^)
echo             .ToList^(^);
echo.
echo         if ^(failures.Any^(^)^)
echo         {
echo             throw new ValidationException^(failures^);
echo         }
echo.
echo         return await next^(^);
echo     }
echo }
) > "%BASE_PATH%\Common\Behaviors\ValidationBehavior.cs"

REM LoggingBehavior.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Behaviors/LoggingBehavior.cs
echo // ============================================
echo using MediatR;
echo using Microsoft.Extensions.Logging;
echo using DevPioneers.Application.Common.Interfaces;
echo.
echo namespace DevPioneers.Application.Common.Behaviors;
echo.
echo /// ^<summary^>
echo /// MediatR pipeline behavior for request/response logging
echo /// Logs all requests with user context and execution details
echo /// ^</summary^>
echo public class LoggingBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : notnull
echo {
echo     private readonly ILogger^<LoggingBehavior^<TRequest, TResponse^>^> _logger;
echo     private readonly ICurrentUserService _currentUserService;
echo.
echo     public LoggingBehavior^(
echo         ILogger^<LoggingBehavior^<TRequest, TResponse^>^> logger,
echo         ICurrentUserService currentUserService^)
echo     {
echo         _logger = logger;
echo         _currentUserService = currentUserService;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request,
echo         RequestHandlerDelegate^<TResponse^> next,
echo         CancellationToken cancellationToken^)
echo     {
echo         var requestName = typeof^(TRequest^).Name;
echo         var userId = _currentUserService.UserId;
echo         var userName = _currentUserService.UserFullName;
echo.
echo         // Log request start
echo         _logger.LogInformation^(
echo             "DevPioneers Request: {Name} {@UserId} {@UserName} {@Request}",
echo             requestName, userId, userName, request^);
echo.
echo         try
echo         {
echo             var response = await next^(^);
echo.
echo             // Log successful response
echo             _logger.LogInformation^(
echo                 "DevPioneers Request Completed: {Name} {@UserId} {@Response}",
echo                 requestName, userId, response^);
echo.
echo             return response;
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             // Log failed request
echo             _logger.LogError^(ex,
echo                 "DevPioneers Request Failed: {Name} {@UserId} {@UserName} {@Request}",
echo                 requestName, userId, userName, request^);
echo.
echo             throw;
echo         }
echo     }
echo }
) > "%BASE_PATH%\Common\Behaviors\LoggingBehavior.cs"

REM PerformanceBehavior.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Behaviors/PerformanceBehavior.cs
echo // ============================================
echo using MediatR;
echo using Microsoft.Extensions.Logging;
echo using DevPioneers.Application.Common.Interfaces;
echo using System.Diagnostics;
echo.
echo namespace DevPioneers.Application.Common.Behaviors;
echo.
echo /// ^<summary^>
echo /// MediatR pipeline behavior for performance monitoring
echo /// Logs slow queries and tracks execution time
echo /// ^</summary^>
echo public class PerformanceBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : notnull
echo {
echo     private readonly Stopwatch _timer;
echo     private readonly ILogger^<PerformanceBehavior^<TRequest, TResponse^>^> _logger;
echo     private readonly ICurrentUserService _currentUserService;
echo.
echo     public PerformanceBehavior^(
echo         ILogger^<PerformanceBehavior^<TRequest, TResponse^>^> logger,
echo         ICurrentUserService currentUserService^)
echo     {
echo         _timer = new Stopwatch^(^);
echo         _logger = logger;
echo         _currentUserService = currentUserService;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request,
echo         RequestHandlerDelegate^<TResponse^> next,
echo         CancellationToken cancellationToken^)
echo     {
echo         _timer.Start^(^);
echo.
echo         var response = await next^(^);
echo.
echo         _timer.Stop^(^);
echo.
echo         var elapsedMilliseconds = _timer.ElapsedMilliseconds;
echo.
echo         // Log if request took longer than 500ms
echo         if ^(elapsedMilliseconds ^> 500^)
echo         {
echo             var requestName = typeof^(TRequest^).Name;
echo             var userId = _currentUserService.UserId;
echo             var userName = _currentUserService.UserFullName;
echo.
echo             _logger.LogWarning^(
echo                 "DevPioneers Slow Request: {Name} ^({ElapsedMilliseconds} ms^) {@UserId} {@UserName} {@Request}",
echo                 requestName, elapsedMilliseconds, userId, userName, request^);
echo         }
echo.
echo         return response;
echo     }
echo }
) > "%BASE_PATH%\Common\Behaviors\PerformanceBehavior.cs"

echo ✅ Common/Behaviors (3/6 files created)

REM ============================================
REM 2. Common/Models/
REM ============================================
echo [2/6] Creating Common/Models...

mkdir "%BASE_PATH%\Common\Models" 2>nul

REM Result.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Models/Result.cs
echo // ============================================
echo namespace DevPioneers.Application.Common.Models;
echo.
echo /// ^<summary^>
echo /// Generic result wrapper for operation outcomes
echo /// Implements Result pattern for better error handling
echo /// ^</summary^>
echo public class Result
echo {
echo     protected Result^(bool succeeded, IEnumerable^<string^> errors^)
echo     {
echo         Succeeded = succeeded;
echo         Errors = errors.ToArray^(^);
echo     }
echo.
echo     public bool Succeeded { get; }
echo     public string[] Errors { get; }
echo     public bool Failed =^> !Succeeded;
echo.
echo     public static Result Success^(^) =^> new^(true, Array.Empty^<string^>^(^)^);
echo     public static Result Failure^(IEnumerable^<string^> errors^) =^> new^(false, errors^);
echo     public static Result Failure^(params string[] errors^) =^> new^(false, errors^);
echo     public static Result Failure^(string error^) =^> new^(false, new[] { error }^);
echo.
echo     public static implicit operator Result^(string error^) =^> Failure^(error^);
echo }
echo.
echo /// ^<summary^>
echo /// Generic result with data
echo /// ^</summary^>
echo public class Result^<T^> : Result
echo {
echo     protected internal Result^(bool succeeded, T? data, IEnumerable^<string^> errors^)
echo         : base^(succeeded, errors^)
echo     {
echo         Data = data;
echo     }
echo.
echo     public T? Data { get; }
echo.
echo     public static Result^<T^> Success^(T data^) =^> new^(true, data, Array.Empty^<string^>^(^)^);
echo     public static new Result^<T^> Failure^(IEnumerable^<string^> errors^) =^> new^(false, default, errors^);
echo     public static new Result^<T^> Failure^(params string[] errors^) =^> new^(false, default, errors^);
echo     public static new Result^<T^> Failure^(string error^) =^> new^(false, default, new[] { error }^);
echo.
echo     public static implicit operator Result^<T^>^(T data^) =^> Success^(data^);
echo     public static implicit operator Result^<T^>^(string error^) =^> Failure^(error^);
echo }
) > "%BASE_PATH%\Common\Models\Result.cs"

REM ApiResponse.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Models/ApiResponse.cs
echo // ============================================
echo using System.Text.Json.Serialization;
echo.
echo namespace DevPioneers.Application.Common.Models;
echo.
echo /// ^<summary^>
echo /// Standard API response wrapper
echo /// Provides consistent response format across all endpoints
echo /// ^</summary^>
echo public class ApiResponse
echo {
echo     public bool Success { get; set; }
echo     public string? Message { get; set; }
echo     public object? Data { get; set; }
echo     public string[]? Errors { get; set; }
echo     public ApiMetadata? Metadata { get; set; }
echo.
echo     [JsonIgnore]
echo     public int StatusCode { get; set; } = 200;
echo.
echo     public static ApiResponse Ok^(object? data = null, string? message = null^)
echo         =^> new^(^)
echo         {
echo             Success = true,
echo             Data = data,
echo             Message = message ?? "Success",
echo             StatusCode = 200
echo         };
echo.
echo     public static ApiResponse Created^(object? data = null, string? message = null^)
echo         =^> new^(^)
echo         {
echo             Success = true,
echo             Data = data,
echo             Message = message ?? "Created successfully",
echo             StatusCode = 201
echo         };
echo.
echo     public static ApiResponse BadRequest^(string message = "Bad request", string[]? errors = null^)
echo         =^> new^(^)
echo         {
echo             Success = false,
echo             Message = message,
echo             Errors = errors,
echo             StatusCode = 400
echo         };
echo.
echo     public static ApiResponse FromResult^(Result result, string? successMessage = null^)
echo     {
echo         if ^(result.Succeeded^)
echo         {
echo             return Ok^(message: successMessage^);
echo         }
echo.
echo         return BadRequest^("Operation failed", result.Errors^);
echo     }
echo.
echo     public static ApiResponse FromResult^<T^>^(Result^<T^> result, string? successMessage = null^)
echo     {
echo         if ^(result.Succeeded^)
echo         {
echo             return Ok^(result.Data, successMessage^);
echo         }
echo.
echo         return BadRequest^("Operation failed", result.Errors^);
echo     }
echo }
echo.
echo /// ^<summary^>
echo /// API response metadata for pagination, etc.
echo /// ^</summary^>
echo public class ApiMetadata
echo {
echo     public int? TotalCount { get; set; }
echo     public int? PageNumber { get; set; }
echo     public int? PageSize { get; set; }
echo     public int? TotalPages { get; set; }
echo     public bool? HasPreviousPage { get; set; }
echo     public bool? HasNextPage { get; set; }
echo     public DateTime? Timestamp { get; set; }
echo     public string? RequestId { get; set; }
echo     public Dictionary^<string, object^>? AdditionalData { get; set; }
echo }
) > "%BASE_PATH%\Common\Models\ApiResponse.cs"

echo ✅ Common/Models (2/4 files created)

REM ============================================
REM 3. Features/Auth/DTOs/
REM ============================================
echo [3/6] Creating Features/Auth/DTOs...

mkdir "%BASE_PATH%\Features\Auth\DTOs" 2>nul

REM AuthResponseDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/AuthResponseDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class AuthResponseDto
echo {
echo     public int UserId { get; set; }
echo     public string Email { get; set; } = string.Empty;
echo     public string FullName { get; set; } = string.Empty;
echo     public List^<string^> Roles { get; set; } = new^(^);
echo     public bool RequiresTwoFactor { get; set; }
echo     public bool RequiresEmailVerification { get; set; }
echo     public string? TwoFactorUserId { get; set; }
echo     
echo     // JWT tokens ^(will be set by Infrastructure layer^)
echo     public string? AccessToken { get; set; }
echo     public string? RefreshToken { get; set; }
echo     public DateTime? AccessTokenExpires { get; set; }
echo     public DateTime? RefreshTokenExpires { get; set; }
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\AuthResponseDto.cs"

REM LoginDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/LoginDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class LoginDto
echo {
echo     public string EmailOrMobile { get; set; } = string.Empty;
echo     public string Password { get; set; } = string.Empty;
echo     public bool RememberMe { get; set; }
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\LoginDto.cs"

REM RegisterDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Auth/DTOs/RegisterDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Auth.DTOs;
echo.
echo public class RegisterDto
echo {
echo     public string FullName { get; set; } = string.Empty;
echo     public string Email { get; set; } = string.Empty;
echo     public string? Mobile { get; set; }
echo     public string Password { get; set; } = string.Empty;
echo     public string ConfirmPassword { get; set; } = string.Empty;
echo     public bool AcceptTerms { get; set; }
echo }
) > "%BASE_PATH%\Features\Auth\DTOs\RegisterDto.cs"

echo ✅ Features/Auth/DTOs (3/9 files created)

REM ============================================
REM 4. Features/Payments/DTOs/
REM ============================================
echo [4/6] Creating Features/Payments/DTOs...

mkdir "%BASE_PATH%\Features\Payments\DTOs" 2>nul

REM PaymentDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Payments/DTOs/PaymentDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Payments.DTOs;
echo.
echo public class PaymentDto : BaseDto
echo {
echo     public int UserId { get; set; }
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string Status { get; set; } = string.Empty;
echo     public string PaymentMethod { get; set; } = string.Empty;
echo     public string? Description { get; set; }
echo     public string? PaymobOrderId { get; set; }
echo     public string? PaymobTransactionId { get; set; }
echo     public DateTime? PaidAt { get; set; }
echo     public DateTime? FailedAt { get; set; }
echo     public string? FailureReason { get; set; }
echo     public DateTime? RefundedAt { get; set; }
echo     public decimal? RefundAmount { get; set; }
echo     public string? RefundReason { get; set; }
echo     public int? SubscriptionPlanId { get; set; }
echo     public string? SubscriptionPlanName { get; set; }
echo     
echo     // Calculated properties
echo     public bool IsCompleted =^> Status == "Completed";
echo     public bool IsPending =^> Status == "Pending" ^|^| Status == "Processing";
echo     public bool IsFailed =^> Status == "Failed" ^|^| Status == "Cancelled" ^|^| Status == "Expired";
echo     public bool IsRefunded =^> Status == "Refunded" ^|^| Status == "PartiallyRefunded";
echo     public bool HasRefund =^> RefundedAt.HasValue ^&^& RefundAmount.HasValue;
echo     
echo     // Display properties
echo     public string AmountDisplay =^> $"{Amount:C} {Currency}";
echo     public string RefundDisplay =^> HasRefund ? $"{RefundAmount:C} {Currency}" : string.Empty;
echo }
) > "%BASE_PATH%\Features\Payments\DTOs\PaymentDto.cs"

REM PaymobOrderDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Payments/DTOs/PaymobOrderDto.cs
echo // ============================================
echo namespace DevPioneers.Application.Features.Payments.DTOs;
echo.
echo public class PaymobOrderDto
echo {
echo     public int PaymentId { get; set; }
echo     public string PaymobOrderId { get; set; } = string.Empty;
echo     public string PaymentUrl { get; set; } = string.Empty;
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string? Description { get; set; }
echo     public string Status { get; set; } = string.Empty;
echo     public DateTime ExpiresAt { get; set; }
echo     public DateTime CreatedAt { get; set; }
echo     
echo     // Calculated properties
echo     public bool IsExpired =^> DateTime.UtcNow ^> ExpiresAt;
echo     public TimeSpan TimeUntilExpiry =^> ExpiresAt - DateTime.UtcNow;
echo     public int MinutesUntilExpiry =^> Math.Max^(0, ^(int^)TimeUntilExpiry.TotalMinutes^);
echo     
echo     // Display properties
echo     public string AmountDisplay =^> $"{Amount:C} {Currency}";
echo     public string ExpiryDisplay =^> IsExpired ? "Expired" : $"Expires in {MinutesUntilExpiry} minutes";
echo }
) > "%BASE_PATH%\Features\Payments\DTOs\PaymobOrderDto.cs"

echo ✅ Features/Payments/DTOs (2/6 files created)

REM ============================================
REM 5. Features/Subscriptions/DTOs/
REM ============================================
echo [5/6] Creating Features/Subscriptions/DTOs...

mkdir "%BASE_PATH%\Features\Subscriptions\DTOs" 2>nul

REM SubscriptionDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Subscriptions/DTOs/SubscriptionDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Subscriptions.DTOs;
echo.
echo public class SubscriptionDto : BaseDto
echo {
echo     public int UserId { get; set; }
echo     public int PlanId { get; set; }
echo     public string PlanName { get; set; } = string.Empty;
echo     public decimal PlanPrice { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string BillingCycle { get; set; } = string.Empty;
echo     public string Status { get; set; } = string.Empty;
echo     public DateTime StartDate { get; set; }
echo     public DateTime EndDate { get; set; }
echo     public DateTime? TrialEndDate { get; set; }
echo     public DateTime? NextBillingDate { get; set; }
echo     public bool AutoRenewal { get; set; }
echo     public int? PaymentId { get; set; }
echo     
echo     // Calculated properties
echo     public int DaysRemaining { get; set; }
echo     public bool IsExpiringSoon { get; set; }
echo     public bool IsActive =^> Status == "Active" ^|^| Status == "Trial";
echo     public bool IsTrial =^> Status == "Trial";
echo     public bool IsExpired =^> Status == "Expired";
echo     public bool IsCancelled =^> Status == "Cancelled";
echo }
) > "%BASE_PATH%\Features\Subscriptions\DTOs\SubscriptionDto.cs"

REM SubscriptionPlanDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Subscriptions/DTOs/SubscriptionPlanDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Subscriptions.DTOs;
echo.
echo public class SubscriptionPlanDto : BaseDto
echo {
echo     public string Name { get; set; } = string.Empty;
echo     public string? Description { get; set; }
echo     public decimal Price { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public string BillingCycle { get; set; } = string.Empty;
echo     public int TrialDurationDays { get; set; }
echo     public string Features { get; set; } = string.Empty;
echo     public int MaxUsers { get; set; }
echo     public int MaxStorageGb { get; set; }
echo     public int PointsAwarded { get; set; }
echo     public bool IsActive { get; set; }
echo     public int DisplayOrder { get; set; }
echo     public decimal DiscountPercentage { get; set; }
echo     
echo     // Calculated properties
echo     public decimal DiscountedPrice =^> Price * ^(1 - DiscountPercentage / 100^);
echo     public bool HasDiscount =^> DiscountPercentage ^> 0;
echo     public bool HasTrial =^> TrialDurationDays ^> 0;
echo     public bool IsUnlimited =^> MaxUsers == -1 ^|^| MaxStorageGb == -1;
echo }
) > "%BASE_PATH%\Features\Subscriptions\DTOs\SubscriptionPlanDto.cs"

echo ✅ Features/Subscriptions/DTOs (2/4 files created)

REM ============================================
REM 6. Features/Wallet/DTOs/
REM ============================================
echo [6/6] Creating Features/Wallet/DTOs...

mkdir "%BASE_PATH%\Features\Wallet\DTOs" 2>nul

REM WalletDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/DTOs/WalletDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Wallet.DTOs;
echo.
echo public class WalletDto : BaseDto
echo {
echo     public int UserId { get; set; }
echo     public decimal Balance { get; set; }
echo     public int Points { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     
echo     // Calculated properties
echo     public string BalanceDisplay =^> $"{Balance:C} {Currency}";
echo     public string PointsDisplay =^> $"{Points:N0} Points";
echo     
echo     // Points conversion ^(assuming 1 EGP = 10 points^)
echo     public decimal PointsValue =^> Points / 10m;
echo     public string PointsValueDisplay =^> $"{PointsValue:C} {Currency}";
echo     
echo     // Total wallet value
echo     public decimal TotalValue =^> Balance + PointsValue;
echo     public string TotalValueDisplay =^> $"{TotalValue:C} {Currency}";
echo     
echo     // Status indicators
echo     public bool HasBalance =^> Balance ^> 0;
echo     public bool HasPoints =^> Points ^> 0;
echo     public bool IsEmpty =^> Balance == 0 ^&^& Points == 0;
echo }
) > "%BASE_PATH%\Features\Wallet\DTOs\WalletDto.cs"

REM TransactionDto.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Features/Wallet/DTOs/TransactionDto.cs
echo // ============================================
echo using DevPioneers.Application.Common.Models;
echo.
echo namespace DevPioneers.Application.Features.Wallet.DTOs;
echo.
echo public class TransactionDto : BaseDto
echo {
echo     public int WalletId { get; set; }
echo     public int UserId { get; set; }
echo     public string Type { get; set; } = string.Empty;
echo     public decimal Amount { get; set; }
echo     public string Currency { get; set; } = string.Empty;
echo     public decimal BalanceBefore { get; set; }
echo     public decimal BalanceAfter { get; set; }
echo     public string Description { get; set; } = string.Empty;
echo     public string? RelatedEntityType { get; set; }
echo     public int? RelatedEntityId { get; set; }
echo     public int? TransferToUserId { get; set; }
echo     
echo     // Calculated properties
echo     public bool IsCredit =^> Type.Contains^("Credit"^);
echo     public bool IsDebit =^> Type.Contains^("Debit"^);
echo     public bool IsPointsTransaction =^> Currency == "PTS";
echo     public bool IsMoneyTransaction =^> Currency != "PTS";
echo     public bool IsTransfer =^> TransferToUserId.HasValue;
echo     
echo     // Display properties
echo     public string AmountDisplay =^> IsPointsTransaction ? 
echo         $"{Amount:N0} Points" : 
echo         $"{Amount:C} {Currency}";
echo     
echo     public string BalanceDisplay =^> IsPointsTransaction ? 
echo         $"{BalanceAfter:N0} Points" : 
echo         $"{BalanceAfter:C} {Currency}";
echo     
echo     public string ChangeIndicator =^> IsCredit ? "+" : "-";
echo     public string ChangeColor =^> IsCredit ? "text-success" : "text-danger";
echo }
) > "%BASE_PATH%\Features\Wallet\DTOs\TransactionDto.cs"

echo ✅ Features/Wallet/DTOs (2/5 files created)

echo.
echo ============================================
echo ✅ Part 1 Complete!
echo ============================================
echo.
echo 📊 Summary - Part 1:
echo    - Common/Behaviors: 3 files
echo    - Common/Models: 2 files  
echo    - Features/Auth/DTOs: 3 files
echo    - Features/Payments/DTOs: 2 files
echo    - Features/Subscriptions/DTOs: 2 files
echo    - Features/Wallet/DTOs: 2 files
echo.
echo    Total files created: 14
echo.
echo 🎯 Next Steps:
echo    1. Run Part 2 script to complete remaining files
echo    2. Then test: dotnet build
echo.

pause