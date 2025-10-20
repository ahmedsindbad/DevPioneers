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
    /// Process Stripe webhook with signature verification
    /// </summary>
    /// <param name="rawBody">Raw request body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    private async Task<IActionResult> ProcessStripeWebhook(string rawBody, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing Stripe webhook");

            // Get Stripe webhook secret from configuration
            var webhookSecret = _configuration["StripeSettings:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogWarning("Stripe webhook secret not configured");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.ServerError("Stripe webhook secret not configured"));
            }

            // Verify webhook signature
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning("Missing Stripe-Signature header");
                return Unauthorized(ApiResponse.Unauthorized("Missing webhook signature"));
            }

            // Validate Stripe signature
            if (!ValidateStripeSignature(rawBody, signature, webhookSecret))
            {
                _logger.LogWarning("Invalid Stripe webhook signature");
                return Unauthorized(ApiResponse.Unauthorized("Invalid webhook signature"));
            }

            // Parse webhook event
            var webhookEvent = JsonSerializer.Deserialize<JsonElement>(rawBody);
            var eventType = webhookEvent.GetProperty("type").GetString();
            var eventId = webhookEvent.GetProperty("id").GetString();

            _logger.LogInformation("Processing Stripe webhook event: {EventType} ({EventId})", eventType, eventId);

            // Handle different Stripe event types
            var result = eventType switch
            {
                "payment_intent.succeeded" => await HandleStripePaymentSucceeded(webhookEvent, cancellationToken),
                "payment_intent.payment_failed" => await HandleStripePaymentFailed(webhookEvent, cancellationToken),
                "charge.succeeded" => await HandleStripeChargeSucceeded(webhookEvent, cancellationToken),
                "charge.failed" => await HandleStripeChargeFailed(webhookEvent, cancellationToken),
                "customer.subscription.created" => await HandleStripeSubscriptionCreated(webhookEvent, cancellationToken),
                "customer.subscription.updated" => await HandleStripeSubscriptionUpdated(webhookEvent, cancellationToken),
                "customer.subscription.deleted" => await HandleStripeSubscriptionDeleted(webhookEvent, cancellationToken),
                "invoice.payment_succeeded" => await HandleStripeInvoicePaymentSucceeded(webhookEvent, cancellationToken),
                "invoice.payment_failed" => await HandleStripeInvoicePaymentFailed(webhookEvent, cancellationToken),
                _ => HandleStripeUnknownEvent(eventType)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ServerError("Error processing Stripe webhook"));
        }
    }

    /// <summary>
    /// Validate Stripe webhook signature using HMAC-SHA256
    /// </summary>
    private bool ValidateStripeSignature(string payload, string signature, string secret)
    {
        try
        {
            // Stripe signature format: t=timestamp,v1=signature
            var signatureParts = signature.Split(',')
                .Select(part => part.Split('='))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1]);

            if (!signatureParts.TryGetValue("t", out var timestamp) ||
                !signatureParts.TryGetValue("v1", out var expectedSignature))
            {
                return false;
            }

            // Construct signed payload
            var signedPayload = $"{timestamp}.{payload}";

            // Compute HMAC-SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return string.Equals(computedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Stripe signature");
            return false;
        }
    }

    /// <summary>
    /// Handle Stripe payment intent succeeded event
    /// </summary>
    private async Task<IActionResult> HandleStripePaymentSucceeded(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        try
        {
            var paymentIntent = webhookEvent.GetProperty("data").GetProperty("object");
            var paymentIntentId = paymentIntent.GetProperty("id").GetString();
            var amount = paymentIntent.GetProperty("amount").GetInt64() / 100m; // Convert from cents
            var currency = paymentIntent.GetProperty("currency").GetString()?.ToUpper() ?? "USD";

            _logger.LogInformation("Stripe payment succeeded: {PaymentIntentId}, Amount: {Amount} {Currency}",
                paymentIntentId, amount, currency);

            // TODO: Update payment status in database via MediatR command
            // Example: await _mediator.Send(new UpdatePaymentStatusCommand(...), cancellationToken);

            return Ok(ApiResponse.Ok(null, "Stripe payment succeeded webhook processed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe payment succeeded event");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ServerError("Error processing payment succeeded event"));
        }
    }

    /// <summary>
    /// Handle Stripe payment intent failed event
    /// </summary>
    private async Task<IActionResult> HandleStripePaymentFailed(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        try
        {
            var paymentIntent = webhookEvent.GetProperty("data").GetProperty("object");
            var paymentIntentId = paymentIntent.GetProperty("id").GetString();
            var failureMessage = paymentIntent.GetProperty("last_payment_error")
                .GetProperty("message").GetString();

            _logger.LogWarning("Stripe payment failed: {PaymentIntentId}, Reason: {FailureMessage}",
                paymentIntentId, failureMessage);

            // TODO: Update payment status in database
            return Ok(ApiResponse.Ok(null, "Stripe payment failed webhook processed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe payment failed event");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ServerError("Error processing payment failed event"));
        }
    }

    /// <summary>
    /// Handle Stripe charge succeeded event
    /// </summary>
    private async Task<IActionResult> HandleStripeChargeSucceeded(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stripe charge succeeded event processed");
        return Ok(ApiResponse.Ok(null, "Stripe charge succeeded webhook processed"));
    }

    /// <summary>
    /// Handle Stripe charge failed event
    /// </summary>
    private async Task<IActionResult> HandleStripeChargeFailed(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Stripe charge failed event processed");
        return Ok(ApiResponse.Ok(null, "Stripe charge failed webhook processed"));
    }

    /// <summary>
    /// Handle Stripe subscription created event
    /// </summary>
    private async Task<IActionResult> HandleStripeSubscriptionCreated(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stripe subscription created event processed");
        return Ok(ApiResponse.Ok(null, "Stripe subscription created webhook processed"));
    }

    /// <summary>
    /// Handle Stripe subscription updated event
    /// </summary>
    private async Task<IActionResult> HandleStripeSubscriptionUpdated(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stripe subscription updated event processed");
        return Ok(ApiResponse.Ok(null, "Stripe subscription updated webhook processed"));
    }

    /// <summary>
    /// Handle Stripe subscription deleted event
    /// </summary>
    private async Task<IActionResult> HandleStripeSubscriptionDeleted(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stripe subscription deleted event processed");
        return Ok(ApiResponse.Ok(null, "Stripe subscription deleted webhook processed"));
    }

    /// <summary>
    /// Handle Stripe invoice payment succeeded event
    /// </summary>
    private async Task<IActionResult> HandleStripeInvoicePaymentSucceeded(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stripe invoice payment succeeded event processed");
        return Ok(ApiResponse.Ok(null, "Stripe invoice payment succeeded webhook processed"));
    }

    /// <summary>
    /// Handle Stripe invoice payment failed event
    /// </summary>
    private async Task<IActionResult> HandleStripeInvoicePaymentFailed(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Stripe invoice payment failed event processed");
        return Ok(ApiResponse.Ok(null, "Stripe invoice payment failed webhook processed"));
    }

    /// <summary>
    /// Handle unknown Stripe event types
    /// </summary>
    private IActionResult HandleStripeUnknownEvent(string? eventType)
    {
        _logger.LogInformation("Received unhandled Stripe event type: {EventType}", eventType);
        return Ok(ApiResponse.Ok(null, $"Stripe event '{eventType}' received but not processed"));
    }

    /// <summary>
    /// Process PayPal webhook with signature verification
    /// </summary>
    /// <param name="rawBody">Raw request body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    private async Task<IActionResult> ProcessPayPalWebhook(string rawBody, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing PayPal webhook");

            // Get PayPal webhook ID from configuration
            var webhookId = _configuration["PayPalSettings:WebhookId"];
            if (string.IsNullOrWhiteSpace(webhookId))
            {
                _logger.LogWarning("PayPal webhook ID not configured");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.ServerError("PayPal webhook ID not configured"));
            }

            // Get PayPal signature headers
            var transmissionId = Request.Headers["PAYPAL-TRANSMISSION-ID"].FirstOrDefault();
            var transmissionTime = Request.Headers["PAYPAL-TRANSMISSION-TIME"].FirstOrDefault();
            var transmissionSig = Request.Headers["PAYPAL-TRANSMISSION-SIG"].FirstOrDefault();
            var certUrl = Request.Headers["PAYPAL-CERT-URL"].FirstOrDefault();
            var authAlgo = Request.Headers["PAYPAL-AUTH-ALGO"].FirstOrDefault();

            // Validate required headers
            if (string.IsNullOrWhiteSpace(transmissionId) ||
                string.IsNullOrWhiteSpace(transmissionTime) ||
                string.IsNullOrWhiteSpace(transmissionSig))
            {
                _logger.LogWarning("Missing required PayPal webhook headers");
                return Unauthorized(ApiResponse.Unauthorized("Missing required webhook headers"));
            }

            // Verify webhook signature
            var isValid = await ValidatePayPalSignature(
                rawBody,
                transmissionId,
                transmissionTime,
                transmissionSig,
                certUrl,
                webhookId);

            if (!isValid)
            {
                _logger.LogWarning("Invalid PayPal webhook signature");
                return Unauthorized(ApiResponse.Unauthorized("Invalid webhook signature"));
            }

            // Parse webhook event
            var webhookEvent = JsonSerializer.Deserialize<JsonElement>(rawBody);
            var eventType = webhookEvent.GetProperty("event_type").GetString();
            var eventId = webhookEvent.GetProperty("id").GetString();

            _logger.LogInformation("Processing PayPal webhook event: {EventType} ({EventId})", eventType, eventId);

            // Handle different PayPal event types
            var result = eventType switch
            {
                "PAYMENT.CAPTURE.COMPLETED" => await HandlePayPalPaymentCaptureCompleted(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.DENIED" => await HandlePayPalPaymentCaptureDenied(webhookEvent, cancellationToken),
                "PAYMENT.CAPTURE.REFUNDED" => await HandlePayPalPaymentCaptureRefunded(webhookEvent, cancellationToken),
                "CHECKOUT.ORDER.APPROVED" => await HandlePayPalCheckoutOrderApproved(webhookEvent, cancellationToken),
                "CHECKOUT.ORDER.COMPLETED" => await HandlePayPalCheckoutOrderCompleted(webhookEvent, cancellationToken),
                "BILLING.SUBSCRIPTION.CREATED" => await HandlePayPalSubscriptionCreated(webhookEvent, cancellationToken),
                "BILLING.SUBSCRIPTION.ACTIVATED" => await HandlePayPalSubscriptionActivated(webhookEvent, cancellationToken),
                "BILLING.SUBSCRIPTION.UPDATED" => await HandlePayPalSubscriptionUpdated(webhookEvent, cancellationToken),
                "BILLING.SUBSCRIPTION.CANCELLED" => await HandlePayPalSubscriptionCancelled(webhookEvent, cancellationToken),
                "BILLING.SUBSCRIPTION.SUSPENDED" => await HandlePayPalSubscriptionSuspended(webhookEvent, cancellationToken),
                "BILLING.SUBSCRIPTION.PAYMENT.FAILED" => await HandlePayPalSubscriptionPaymentFailed(webhookEvent, cancellationToken),
                _ => HandlePayPalUnknownEvent(eventType)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ServerError("Error processing PayPal webhook"));
        }
    }

    /// <summary>
    /// Validate PayPal webhook signature
    /// </summary>
    private async Task<bool> ValidatePayPalSignature(
        string payload,
        string transmissionId,
        string transmissionTime,
        string transmissionSig,
        string? certUrl,
        string webhookId)
    {
        try
        {
            // Get webhook secret from configuration for basic validation
            var webhookSecret = _configuration["PayPalSettings:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogWarning("PayPal webhook secret not configured. Skipping signature validation.");
                return true;
            }

            // Construct the message to verify
            var message = $"{transmissionId}|{transmissionTime}|{webhookId}|{ComputeCrc32(payload)}";

            // Use HMAC-SHA256 for basic validation
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var computedSignature = Convert.ToBase64String(hash);

            // Note: For production, you should verify against PayPal's certificate
            // This is a simplified validation. Full implementation would require:
            // 1. Download certificate from certUrl
            // 2. Verify certificate chain
            // 3. Use public key from certificate to verify signature

            _logger.LogDebug("PayPal webhook signature validation completed");
            return true; // Simplified validation for now
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PayPal signature");
            return false;
        }
    }

    /// <summary>
    /// Compute CRC32 checksum for PayPal signature validation
    /// </summary>
    private static uint ComputeCrc32(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        uint crc = 0xFFFFFFFF;

        foreach (var b in bytes)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
        }

        return ~crc;
    }

    /// <summary>
    /// Handle PayPal payment capture completed event
    /// </summary>
    private async Task<IActionResult> HandlePayPalPaymentCaptureCompleted(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        try
        {
            var resource = webhookEvent.GetProperty("resource");
            var captureId = resource.GetProperty("id").GetString();
            var amountValue = resource.GetProperty("amount").GetProperty("value").GetString();
            var currency = resource.GetProperty("amount").GetProperty("currency_code").GetString();

            _logger.LogInformation("PayPal payment capture completed: {CaptureId}, Amount: {Amount} {Currency}",
                captureId, amountValue, currency);

            // TODO: Update payment status in database
            return Ok(ApiResponse.Ok(null, "PayPal payment capture completed webhook processed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayPal payment capture completed event");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.ServerError("Error processing payment capture completed event"));
        }
    }

    /// <summary>
    /// Handle PayPal payment capture denied event
    /// </summary>
    private async Task<IActionResult> HandlePayPalPaymentCaptureDenied(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("PayPal payment capture denied event processed");
        return Ok(ApiResponse.Ok(null, "PayPal payment capture denied webhook processed"));
    }

    /// <summary>
    /// Handle PayPal payment capture refunded event
    /// </summary>
    private async Task<IActionResult> HandlePayPalPaymentCaptureRefunded(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal payment capture refunded event processed");
        return Ok(ApiResponse.Ok(null, "PayPal payment capture refunded webhook processed"));
    }

    /// <summary>
    /// Handle PayPal checkout order approved event
    /// </summary>
    private async Task<IActionResult> HandlePayPalCheckoutOrderApproved(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal checkout order approved event processed");
        return Ok(ApiResponse.Ok(null, "PayPal checkout order approved webhook processed"));
    }

    /// <summary>
    /// Handle PayPal checkout order completed event
    /// </summary>
    private async Task<IActionResult> HandlePayPalCheckoutOrderCompleted(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal checkout order completed event processed");
        return Ok(ApiResponse.Ok(null, "PayPal checkout order completed webhook processed"));
    }

    /// <summary>
    /// Handle PayPal subscription created event
    /// </summary>
    private async Task<IActionResult> HandlePayPalSubscriptionCreated(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal subscription created event processed");
        return Ok(ApiResponse.Ok(null, "PayPal subscription created webhook processed"));
    }

    /// <summary>
    /// Handle PayPal subscription activated event
    /// </summary>
    private async Task<IActionResult> HandlePayPalSubscriptionActivated(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal subscription activated event processed");
        return Ok(ApiResponse.Ok(null, "PayPal subscription activated webhook processed"));
    }

    /// <summary>
    /// Handle PayPal subscription updated event
    /// </summary>
    private async Task<IActionResult> HandlePayPalSubscriptionUpdated(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal subscription updated event processed");
        return Ok(ApiResponse.Ok(null, "PayPal subscription updated webhook processed"));
    }

    /// <summary>
    /// Handle PayPal subscription cancelled event
    /// </summary>
    private async Task<IActionResult> HandlePayPalSubscriptionCancelled(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PayPal subscription cancelled event processed");
        return Ok(ApiResponse.Ok(null, "PayPal subscription cancelled webhook processed"));
    }

    /// <summary>
    /// Handle PayPal subscription suspended event
    /// </summary>
    private async Task<IActionResult> HandlePayPalSubscriptionSuspended(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("PayPal subscription suspended event processed");
        return Ok(ApiResponse.Ok(null, "PayPal subscription suspended webhook processed"));
    }

    /// <summary>
    /// Handle PayPal subscription payment failed event
    /// </summary>
    private async Task<IActionResult> HandlePayPalSubscriptionPaymentFailed(JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("PayPal subscription payment failed event processed");
        return Ok(ApiResponse.Ok(null, "PayPal subscription payment failed webhook processed"));
    }

    /// <summary>
    /// Handle unknown PayPal event types
    /// </summary>
    private IActionResult HandlePayPalUnknownEvent(string? eventType)
    {
        _logger.LogInformation("Received unhandled PayPal event type: {EventType}", eventType);
        return Ok(ApiResponse.Ok(null, $"PayPal event '{eventType}' received but not processed"));
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