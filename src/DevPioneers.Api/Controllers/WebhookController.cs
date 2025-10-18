// ============================================
// File: DevPioneers.Api/Controllers/WebhookController.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.Commands;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DevPioneers.Api.Controllers;

/// <summary>
/// Webhook controller for handling external service callbacks
/// Primarily handles Paymob payment webhooks
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Webhooks need to be accessible without authentication
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public WebhookController(
        IMediator mediator,
        ILogger<WebhookController> logger,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <summary>
    /// Handle Paymob payment webhook callbacks
    /// This endpoint receives notifications about payment status changes
    /// </summary>
    /// <param name="callbackData">Payment callback data from Paymob</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Webhook processing result</returns>
    [HttpPost("paymob/payment")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandlePaymobPaymentWebhook(
        [FromBody] PaymentCallbackDto callbackData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Log incoming webhook
            _logger.LogInformation("Received Paymob payment webhook for order {PaymobOrderId} with status {Status}",
                callbackData.PaymobOrderId, callbackData.Status);

            // Validate webhook signature if configured
            var isSignatureValid = await ValidatePaymobSignatureAsync(callbackData);
            if (!isSignatureValid)
            {
                _logger.LogWarning("Invalid webhook signature for Paymob order {PaymobOrderId}", callbackData.PaymobOrderId);
                return Unauthorized(ApiResponse.Unauthorized("Invalid webhook signature"));
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(callbackData.PaymobOrderId) ||
                string.IsNullOrWhiteSpace(callbackData.PaymobTransactionId) ||
                string.IsNullOrWhiteSpace(callbackData.Status))
            {
                _logger.LogWarning("Missing required fields in Paymob webhook callback: {CallbackData}",
                    JsonSerializer.Serialize(callbackData));
                return BadRequest(ApiResponse.BadRequest("Missing required callback fields"));
            }

            // Extract client IP and User-Agent for audit purposes
            var clientIp = GetClientIpAddress();
            var userAgent = Request.Headers.UserAgent.ToString();

            // Create verification command
            var command = new VerifyPaymobCallbackCommand(
                PaymobOrderId: callbackData.PaymobOrderId,
                PaymobTransactionId: callbackData.PaymobTransactionId,
                Status: callbackData.Status,
                Amount: callbackData.Amount,
                Currency: callbackData.Currency,
                AdditionalData: callbackData.AdditionalData ?? new Dictionary<string, object>
                {
                    ["client_ip"] = clientIp,
                    ["user_agent"] = userAgent,
                    ["webhook_received_at"] = DateTime.UtcNow,
                    ["signature"] = callbackData.Signature ?? string.Empty
                }
            );

            // Process webhook through MediatR
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully processed Paymob webhook for order {PaymobOrderId}. Payment success: {IsSuccess}",
                    callbackData.PaymobOrderId, result.Data?.IsSuccess);

                return Ok(ApiResponse.FromResult(result, "Webhook processed successfully"));
            }

            _logger.LogError("Failed to process Paymob webhook for order {PaymobOrderId}: {Errors}",
                callbackData.PaymobOrderId, string.Join(", ", result.ErrorMessage));

            return BadRequest(ApiResponse.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Paymob webhook for order {PaymobOrderId}",
                callbackData?.PaymobOrderId ?? "unknown");

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ServerError("An error occurred while processing the webhook"));
        }
    }

    /// <summary>
    /// Handle general payment status webhook (for future payment providers)
    /// </summary>
    /// <param name="provider">Payment provider name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Webhook processing result</returns>
    [HttpPost("payment/{provider}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> HandlePaymentWebhook(
        [FromRoute] string provider,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Received payment webhook from provider: {Provider}", provider);

            // Read raw request body for signature validation
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);

            // Route to appropriate handler based on provider
            return provider.ToLowerInvariant() switch
            {
                "paymob" => await ProcessPaymobWebhookFromRawBody(rawBody, cancellationToken),
                "stripe" => await ProcessStripeWebhook(rawBody, cancellationToken),
                "paypal" => await ProcessPayPalWebhook(rawBody, cancellationToken),
                _ => StatusCode(StatusCodes.Status501NotImplemented,
                    ApiResponse.BadRequest($"Payment provider '{provider}' is not supported"))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook from provider {Provider}", provider);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ServerError("An error occurred while processing the webhook"));
        }
    }

    /// <summary>
    /// Health check endpoint for webhook availability
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        var healthData = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            SupportedProviders = new[] { "paymob", "stripe", "paypal" },
            ServerTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
        };

        _logger.LogDebug("Webhook health check requested from {ClientIp}", GetClientIpAddress());

        return Ok(ApiResponse.Ok(healthData, "Webhook service is healthy"));
    }

    /// <summary>
    /// Test endpoint for webhook validation (Development only)
    /// </summary>
    /// <param name="testData">Test webhook data</param>
    /// <returns>Test result</returns>
    [HttpPost("test")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public IActionResult TestWebhook([FromBody] object testData)
    {
        // Only allow in development environment
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return Forbid("Test endpoint is only available in development environment");
        }

        var testResult = new
        {
            Message = "Webhook test endpoint reached successfully",
            ReceivedData = testData,
            Timestamp = DateTime.UtcNow,
            ClientIp = GetClientIpAddress(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _logger.LogInformation("Webhook test endpoint called with data: {TestData}",
            JsonSerializer.Serialize(testData));

        return Ok(ApiResponse.Ok(testResult, "Test webhook processed successfully"));
    }

    #region Private Helper Methods

    /// <summary>
    /// Validate Paymob webhook signature
    /// </summary>
    /// <param name="callbackData">Callback data to validate</param>
    /// <returns>True if signature is valid</returns>
    private async Task<bool> ValidatePaymobSignatureAsync(PaymentCallbackDto callbackData)
    {
        try
        {
            // Get webhook secret from configuration
            var webhookSecret = _configuration["PaymobSettings:WebhookSecret"];
            
            // If no secret is configured, skip validation (not recommended for production)
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogWarning("Paymob webhook secret not configured. Skipping signature validation.");
                return true;
            }

            // If no signature provided, validation fails
            if (string.IsNullOrWhiteSpace(callbackData.Signature))
            {
                _logger.LogWarning("No signature provided in Paymob webhook callback");
                return false;
            }

            // Construct the data string for signature validation
            // This should match Paymob's signature calculation method
            var dataToSign = $"{callbackData.PaymobOrderId}:{callbackData.PaymobTransactionId}:{callbackData.Status}:{callbackData.Amount}:{callbackData.Currency}";
            
            // Calculate expected signature using HMAC-SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
            var expectedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            // Compare signatures
            var isValid = string.Equals(callbackData.Signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
            
            if (!isValid)
            {
                _logger.LogWarning("Paymob webhook signature mismatch. Expected: {Expected}, Received: {Received}",
                    expectedSignature, callbackData.Signature);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Paymob webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Process Paymob webhook from raw body (for signature validation)
    /// </summary>
    /// <param name="rawBody">Raw request body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    private async Task<IActionResult> ProcessPaymobWebhookFromRawBody(string rawBody, CancellationToken cancellationToken)
    {
        try
        {
            // Parse the JSON body
            var callbackData = JsonSerializer.Deserialize<PaymentCallbackDto>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (callbackData == null)
            {
                return BadRequest(ApiResponse.BadRequest("Invalid JSON data in webhook"));
            }

            // Reuse the main Paymob webhook handler
            return await HandlePaymobPaymentWebhook(callbackData, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Paymob webhook JSON: {RawBody}", rawBody);
            return BadRequest(ApiResponse.BadRequest("Invalid JSON format"));
        }
    }

    /// <summary>
    /// Process Stripe webhook (placeholder for future implementation)
    /// </summary>
    /// <param name="rawBody">Raw request body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    private async Task<IActionResult> ProcessStripeWebhook(string rawBody, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stripe webhook received but not yet implemented");
        
        // TODO: Implement Stripe webhook processing
        return StatusCode(StatusCodes.Status501NotImplemented,
            ApiResponse.BadRequest("Stripe webhook processing not yet implemented"));
    }

    /// <summary>
    /// Process PayPal webhook (placeholder for future implementation)
    /// </summary>
    /// <param name="rawBody">Raw request body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    private async Task<IActionResult> ProcessPayPalWebhook(string rawBody, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal webhook received but not yet implemented");
        
        // TODO: Implement PayPal webhook processing
        return StatusCode(StatusCodes.Status501NotImplemented,
            ApiResponse.BadRequest("PayPal webhook processing not yet implemented"));
    }

    /// <summary>
    /// Get client IP address from request
    /// </summary>
    /// <returns>Client IP address</returns>
    private string GetClientIpAddress()
    {
        // Check for forwarded IP first (when behind load balancer/proxy)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // Take the first IP if multiple are present
            return forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? "unknown";
        }

        // Check real IP header
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    #endregion
}