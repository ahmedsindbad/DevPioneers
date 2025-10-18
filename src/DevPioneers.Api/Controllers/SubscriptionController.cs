// ============================================
// File: DevPioneers.Api/Controllers/SubscriptionController.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.Commands;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using DevPioneers.Application.Features.Subscriptions.Queries;
using DevPioneers.Application.Features.Payments.Commands;
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Application.Features.Payments.Queries;
using DevPioneers.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevPioneers.Api.Controllers;

/// <summary>
/// Subscription management controller
/// Handles subscription plans, user subscriptions, and payment integration with Paymob
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<SubscriptionController> logger)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    #region Subscription Plans

    /// <summary>
    /// Get all available subscription plans
    /// </summary>
    /// <returns>List of subscription plans</returns>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        try
        {
            var query = new GetSubscriptionPlansQuery();
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Subscription plans retrieved successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve subscription plans"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plans");
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Get subscription plan by ID
    /// </summary>
    /// <param name="id">Plan ID</param>
    /// <returns>Subscription plan details</returns>
    [HttpGet("plans/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionPlan(int id)
    {
        try
        {
            var query = new GetSubscriptionPlanByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                return Ok(ApiResponse.Ok(result.Data, "Subscription plan retrieved successfully"));
            }

            return NotFound(ApiResponse.NotFound("Subscription plan not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plan {PlanId}", id);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    #endregion

    #region User Subscriptions

    /// <summary>
    /// Get current user's active subscription
    /// </summary>
    /// <returns>Active subscription details or null if no active subscription</returns>
    [HttpGet("current")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.Unauthorized("User not authenticated"));
            }

            var query = new GetActiveSubscriptionQuery(_currentUserService.UserId.Value);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                if (result.Data != null)
                {
                    return Ok(ApiResponse.Ok(result.Data, "Active subscription retrieved successfully"));
                }
                else
                {
                    return Ok(ApiResponse.Ok(null, "No active subscription found"));
                }
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve subscription"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current subscription for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Get user's subscription history
    /// </summary>
    /// <returns>List of user subscriptions</returns>
    [HttpGet("history")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionHistory()
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.Unauthorized("User not authenticated"));
            }

            var query = new GetUserSubscriptionsQuery(_currentUserService.UserId.Value);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Subscription history retrieved successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve subscription history"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription history for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    #endregion

    #region Payment & Subscription Creation

    /// <summary>
    /// Create Paymob payment order for subscription
    /// </summary>
    /// <param name="request">Payment order request</param>
    /// <returns>Paymob order details with payment URL</returns>
    [HttpPost("create-payment-order")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePaymentOrder([FromBody] CreatePaymentOrderRequestDto request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.Unauthorized("User not authenticated"));
            }

            // ✅ إصلاح Constructor - استخدام record syntax صحيح
            var command = new CreatePaymobOrderCommand(
                UserId: _currentUserService.UserId.Value,
                Amount: request.Amount,
                Currency: request.Currency ?? "EGP",
                Description: request.Description ?? "Subscription payment",
                SubscriptionPlanId: request.SubscriptionPlanId,
                IpAddress: _currentUserService.IpAddress,
                UserAgent: _currentUserService.UserAgent
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetPaymentOrderStatus),
                    new { orderId = result.Data?.PaymobOrderId },
                    ApiResponse.Created(result.Data, "Payment order created successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to create payment order"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment order for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Get payment order status
    /// </summary>
    /// <param name="orderId">Paymob order ID</param>
    /// <returns>Payment order status</returns>
    [HttpGet("payment-order/{orderId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentOrderStatus(string orderId)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.Unauthorized("User not authenticated"));
            }

            var query = new GetPaymentOrderStatusQuery(orderId, _currentUserService.UserId.Value);
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                return Ok(ApiResponse.Ok(result.Data, "Payment order status retrieved successfully"));
            }

            return NotFound(ApiResponse.NotFound("Payment order not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment order status {OrderId} for user {UserId}", 
                orderId, _currentUserService.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Verify Paymob payment callback and activate subscription
    /// </summary>
    /// <param name="request">Payment callback data from Paymob</param>
    /// <returns>Payment verification result</returns>
    [HttpPost("verify-payment")]
    [AllowAnonymous] // Paymob callback doesn't include authentication
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyPaymentCallback([FromBody] PaymentCallbackRequestDto request)
    {
        try
        {
            // ✅ إصلاح Constructor - استخدام record syntax صحيح
            var command = new VerifyPaymobCallbackCommand(
                PaymobOrderId: request.PaymobOrderId,
                PaymobTransactionId: request.PaymobTransactionId,
                Status: request.Status,
                Amount: request.Amount,
                Currency: request.Currency,
                AdditionalData: request.AdditionalData
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Payment verified successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Payment verification failed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment callback for order {OrderId}", request.PaymobOrderId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Create subscription manually (for admin or free trials)
    /// </summary>
    /// <param name="request">Subscription creation request</param>
    /// <returns>Created subscription details</returns>
    [HttpPost("create")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequestDto request)
    {
        try
        {
            // ✅ إصلاح Constructor - استخدام record syntax صحيح
            var command = new CreateSubscriptionCommand(
                UserId: request.UserId,
                SubscriptionPlanId: request.SubscriptionPlanId,
                PaymentId: request.PaymentId
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetCurrentSubscription),
                    ApiResponse.Created(result.Data, "Subscription created successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to create subscription"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}", request.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    #endregion

    #region Subscription Management

    /// <summary>
    /// Cancel current subscription
    /// </summary>
    /// <param name="request">Cancellation request</param>
    /// <returns>Cancellation result</returns>
    [HttpPost("cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequestDto request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.Unauthorized("User not authenticated"));
            }

            // ✅ تم إصلاح المشكلة - نحتاج للحصول على SubscriptionId أولاً
            // نجد الاشتراك النشط للمستخدم الحالي
            var activeSubscriptionQuery = new GetActiveSubscriptionQuery(_currentUserService.UserId.Value);
            var activeSubscriptionResult = await _mediator.Send(activeSubscriptionQuery);

            if (!activeSubscriptionResult.IsSuccess || activeSubscriptionResult.Data == null)
            {
                return BadRequest(ApiResponse.BadRequest("No active subscription found to cancel"));
            }

            // ✅ استخدام الـ Constructor الصحيح مع SubscriptionId
            var command = new CancelSubscriptionCommand(
                UserId: _currentUserService.UserId.Value,
                SubscriptionId: activeSubscriptionResult.Data.Id,
                Reason: request.Reason
            );

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Subscription cancelled successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to cancel subscription"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Reactivate cancelled subscription
    /// </summary>
    /// <returns>Reactivation result</returns>
    [HttpPost("reactivate")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReactivateSubscription()
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.Unauthorized("User not authenticated"));
            }

            var command = new ReactivateSubscriptionCommand(_currentUserService.UserId.Value);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Subscription reactivated successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to reactivate subscription"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Update subscription auto-renewal setting
    /// </summary>
    /// <param name="request">Auto-renewal update request</param>
    /// <returns>Update result</returns>
    [HttpPut("auto-renewal")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAutoRenewal([FromBody] UpdateAutoRenewalRequestDto request)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.Unauthorized("User not authenticated"));
            }

            var command = new UpdateAutoRenewalCommand(_currentUserService.UserId.Value, request.AutoRenewal);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Auto-renewal setting updated successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to update auto-renewal setting"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auto-renewal for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    #endregion

    #region Analytics & Admin

    /// <summary>
    /// Get subscription analytics (Admin only)
    /// </summary>
    /// <param name="fromDate">Start date for analytics</param>
    /// <param name="toDate">End date for analytics</param>
    /// <returns>Subscription analytics</returns>
    [HttpGet("analytics")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionAnalytics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetSubscriptionAnalyticsQuery(
                FromDate: fromDate ?? DateTime.UtcNow.AddDays(-30),
                ToDate: toDate ?? DateTime.UtcNow
            );

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Subscription analytics retrieved successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve analytics"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription analytics");
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    /// <summary>
    /// Get users with expiring subscriptions (Admin only)
    /// </summary>
    /// <param name="daysAhead">Number of days ahead to check for expiration</param>
    /// <returns>List of users with expiring subscriptions</returns>
    [HttpGet("expiring")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExpiringSubscriptions([FromQuery] int daysAhead = 7)
    {
        try
        {
            var query = new GetExpiringSubscriptionsQuery(daysAhead);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(ApiResponse.Ok(result.Data, "Expiring subscriptions retrieved successfully"));
            }

            return BadRequest(ApiResponse.BadRequest(result.ErrorMessage ?? "Failed to retrieve expiring subscriptions"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring subscriptions");
            return StatusCode(500, ApiResponse.ServerError());
        }
    }

    #endregion
}

#region DTOs for Request/Response

/// <summary>
/// Request DTO for creating payment order
/// </summary>
public class CreatePaymentOrderRequestDto
{
    public int SubscriptionPlanId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for payment callback from Paymob
/// </summary>
public class PaymentCallbackRequestDto
{
    public string PaymobOrderId { get; set; } = string.Empty;
    public string PaymobTransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Request DTO for creating subscription manually
/// </summary>
public class CreateSubscriptionRequestDto
{
    public int UserId { get; set; }
    public int SubscriptionPlanId { get; set; }
    public int? PaymentId { get; set; }
}

/// <summary>
/// Request DTO for cancelling subscription
/// </summary>
public class CancelSubscriptionRequestDto
{
    public string? Reason { get; set; }
    // ✅ إزالة CancelImmediately لأنه غير موجود في Command الأصلي
}

/// <summary>
/// Request DTO for updating auto-renewal setting
/// </summary>
public class UpdateAutoRenewalRequestDto
{
    public bool AutoRenewal { get; set; }
}

#endregion