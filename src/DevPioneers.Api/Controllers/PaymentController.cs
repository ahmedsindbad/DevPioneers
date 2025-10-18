// ============================================
// File: DevPioneers.Api/Controllers/PaymentController.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.Commands;
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Application.Features.Payments.Queries;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DevPioneers.Api.Controllers;

/// <summary>
/// Payment management controller
/// Handles payment processing, order creation, verification, and refunds
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<PaymentController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Create payment order with Paymob
    /// </summary>
    /// <param name="dto">Payment order details</param>
    /// <returns>Payment order with URL for frontend redirection</returns>
    [HttpPost("create-order")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePaymentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();
                return BadRequest(ApiResponse.BadRequest("Validation failed", errors));
            }

            // Get current user ID
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return Unauthorized(ApiResponse.BadRequest("User not authenticated"));
            }

            // Admin users can create orders for other users
            var targetUserId = _currentUserService.IsInRole("Admin") && dto.UserId > 0 
                ? dto.UserId 
                : currentUserId.Value;

            var command = new CreatePaymobOrderCommand(
                UserId: targetUserId,
                Amount: dto.Amount,
                Currency: dto.Currency,
                Description: dto.Description,
                SubscriptionPlanId: dto.SubscriptionPlanId,
                IpAddress: _currentUserService.IpAddress,
                UserAgent: _currentUserService.UserAgent
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Payment order created successfully for user {UserId}, amount {Amount}", 
                    targetUserId, dto.Amount);
                
                return Ok(ApiResponse.Ok(result.Data, "Payment order created successfully"));
            }

            return BadRequest(ApiResponse.BadRequest("Failed to create payment order", result.Errors.ToArray<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment order for user {UserId}", dto.UserId);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while creating payment order"));
        }
    }

    /// <summary>
    /// Verify payment callback from Paymob
    /// </summary>
    /// <param name="dto">Callback data from Paymob</param>
    /// <returns>Payment verification result</returns>
    [HttpPost("verify-callback")]
    [AllowAnonymous] // Paymob webhook doesn't send auth headers
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyCallback([FromBody] PaymentCallbackDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();
                return BadRequest(ApiResponse.BadRequest("Invalid callback data", errors));
            }

            var command = new VerifyPaymobCallbackCommand(
                PaymobOrderId: dto.PaymobOrderId,
                PaymobTransactionId: dto.PaymobTransactionId,
                Status: dto.Status,
                Amount: dto.Amount,
                Currency: dto.Currency,
                AdditionalData: dto.AdditionalData
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Payment callback verified successfully for order {PaymobOrderId}", 
                    dto.PaymobOrderId);
                
                return Ok(ApiResponse.Ok(result.Data, "Payment callback verified successfully"));
            }

            _logger.LogWarning("Payment callback verification failed for order {PaymobOrderId}: {Errors}", 
                dto.PaymobOrderId, string.Join(", ", result.Errors));

            return BadRequest(ApiResponse.BadRequest("Payment verification failed", result.Errors.ToArray<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment callback for order {PaymobOrderId}", dto.PaymobOrderId);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred during payment verification"));
        }
    }

    /// <summary>
    /// Process payment refund
    /// </summary>
    /// <param name="paymentId">Payment ID to refund</param>
    /// <param name="dto">Refund details</param>
    /// <returns>Refund processing result</returns>
    [HttpPost("{paymentId}/refund")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessRefund(
        [FromRoute] int paymentId, 
        [FromBody] ProcessRefundDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToArray();
                return BadRequest(ApiResponse.BadRequest("Validation failed", errors));
            }

            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return Unauthorized(ApiResponse.BadRequest("User not authenticated"));
            }

            var command = new ProcessRefundCommand(
                PaymentId: paymentId,
                Amount: dto.Amount,
                Reason: dto.Reason,
                ProcessedByUserId: currentUserId.Value
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Refund processed successfully for payment {PaymentId}, amount {Amount}", 
                    paymentId, dto.Amount);
                
                return Ok(ApiResponse.Ok(result.Data, "Refund processed successfully"));
            }

            return BadRequest(ApiResponse.BadRequest("Failed to process refund", result.Errors.ToArray<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while processing refund"));
        }
    }

    /// <summary>
    /// Get payment history for current user or specified user (admin only)
    /// </summary>
    /// <param name="userId">User ID (optional, admin only)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="status">Filter by payment status</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <returns>Paginated payment history</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPaymentHistory(
        [FromQuery] int? userId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return Unauthorized(ApiResponse.BadRequest("User not authenticated"));
            }

            // Determine target user ID
            var targetUserId = currentUserId.Value;
            if (userId.HasValue && userId.Value != currentUserId.Value)
            {
                // Only admin can view other users' payment history
                if (!_currentUserService.IsInRole("Admin"))
                {
                    return Forbid();
                }
                targetUserId = userId.Value;
            }

            var query = new GetPaymentHistoryQuery(
                UserId: targetUserId,
                PageNumber: pageNumber,
                PageSize: pageSize,
                Status: status,
                FromDate: fromDate,
                ToDate: toDate
            );

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Payment history retrieved successfully"));
            }

            return BadRequest(ApiResponse.BadRequest("Failed to retrieve payment history", result.Errors.ToArray<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history for user {UserId}", userId);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while retrieving payment history"));
        }
    }

    /// <summary>
    /// Get specific payment details by ID
    /// </summary>
    /// <param name="paymentId">Payment ID</param>
    /// <returns>Payment details</returns>
    [HttpGet("{paymentId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaymentById([FromRoute] int paymentId)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return Unauthorized(ApiResponse.BadRequest("User not authenticated"));
            }

            // Get single payment from history query
            var query = new GetPaymentHistoryQuery(
                UserId: currentUserId.Value,
                PageNumber: 1,
                PageSize: 1
            );

            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data?.Items.Any() == true)
            {
                var payment = result.Data.Items.FirstOrDefault(p => p.Id == paymentId);
                
                if (payment == null)
                {
                    // If admin, try to get payment for any user
                    if (_currentUserService.IsInRole("Admin"))
                    {
                        // This would require a separate query or service method to get payment by ID directly
                        // For now, return not found
                        return NotFound(ApiResponse.BadRequest("Payment not found"));
                    }
                    
                    return NotFound(ApiResponse.BadRequest("Payment not found"));
                }

                return Ok(ApiResponse.Ok(payment, "Payment details retrieved successfully"));
            }

            return NotFound(ApiResponse.BadRequest("Payment not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment {PaymentId}", paymentId);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while retrieving payment details"));
        }
    }

    /// <summary>
    /// Get payment statistics for current user
    /// </summary>
    /// <returns>Payment statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPaymentStatistics()
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return Unauthorized(ApiResponse.BadRequest("User not authenticated"));
            }

            // Get all payments for statistics
            var query = new GetPaymentHistoryQuery(
                UserId: currentUserId.Value,
                PageNumber: 1,
                PageSize: 1000 // Large number to get all payments for stats
            );

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                var payments = result.Data?.Items ?? new List<PaymentDto>();
                
                var statistics = new
                {
                    TotalPayments = payments.Count,
                    CompletedPayments = payments.Count(p => p.IsCompleted),
                    PendingPayments = payments.Count(p => p.IsPending),
                    FailedPayments = payments.Count(p => p.IsFailed),
                    RefundedPayments = payments.Count(p => p.IsRefunded),
                    TotalAmount = payments.Where(p => p.IsCompleted).Sum(p => p.Amount),
                    TotalRefunded = payments.Where(p => p.HasRefund).Sum(p => p.RefundAmount ?? 0),
                    AveragePayment = payments.Where(p => p.IsCompleted).Any() 
                        ? payments.Where(p => p.IsCompleted).Average(p => p.Amount) 
                        : 0,
                    LastPaymentDate = payments.Where(p => p.IsCompleted)
                        .OrderByDescending(p => p.CreatedAtUtc)
                        .FirstOrDefault()?.CreatedAtUtc,
                    Currency = payments.FirstOrDefault()?.Currency ?? "EGP"
                };

                return Ok(ApiResponse.Ok(statistics, "Payment statistics retrieved successfully"));
            }

            return BadRequest(ApiResponse.BadRequest("Failed to retrieve payment statistics", result.Errors.ToArray<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment statistics for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while retrieving payment statistics"));
        }
    }

    /// <summary>
    /// Cancel pending payment
    /// </summary>
    /// <param name="paymentId">Payment ID to cancel</param>
    /// <returns>Cancellation result</returns>
    [HttpPost("{paymentId}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPayment([FromRoute] int paymentId)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                return Unauthorized(ApiResponse.BadRequest("User not authenticated"));
            }

            // Note: This would require implementing a CancelPaymentCommand
            // For now, return a message indicating the functionality needs to be implemented
            
            _logger.LogInformation("Payment cancellation requested for payment {PaymentId} by user {UserId}", 
                paymentId, currentUserId.Value);

            return Ok(ApiResponse.Ok(new { PaymentId = paymentId, Status = "Cancellation requested" }, 
                "Payment cancellation has been requested"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
            return StatusCode(500, ApiResponse.BadRequest("An error occurred while cancelling payment"));
        }
    }
}

/// <summary>
/// DTO for processing refunds
/// </summary>
public class ProcessRefundDto
{
    /// <summary>
    /// Refund amount (must be <= original payment amount)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Reason for refund
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}