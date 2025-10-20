// File: DevPioneers.Infrastructure/Services/Payment/PaymobService.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DevPioneers.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Infrastructure.Services.Payment;

public class PaymobService : IPaymentService
{
    private readonly HttpClient _http;
    private readonly PaymobSettings _settings;
    private readonly ILogger<PaymobService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PaymobService(
        IHttpClientFactory httpFactory,
        IConfiguration configuration,
        ILogger<PaymobService> logger)
    {
        _http = httpFactory.CreateClient("PaymobClient");
        _settings = configuration.GetSection("PaymobSettings").Get<PaymobSettings>() ?? throw new ArgumentNullException(nameof(PaymobSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    // Public API implementation

    public async Task<PaymentOrderResult> CreateOrderAsync(CreatePaymentOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1) Authenticate -> get token
            var token = await GetAuthTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
                return new PaymentOrderResult(false, null, null, "Failed to obtain auth token from Paymob.");

            // 2) Register order (creates Paymob order and returns order id)
            var orderResponse = await RegisterEcommerceOrderAsync(token, request, cancellationToken);
            if (!orderResponse.Success)
                return new PaymentOrderResult(false, null, null, orderResponse.ErrorMessage);

            // 3) Generate payment key (payment_token) for use in iframe or redirection
            var paymentKeyResp = await GeneratePaymentKeyAsync(
                token,
                new PaymentOrderForKeyRequest
                {
                    OrderId = orderResponse.PaymobOrderId!,
                    AmountCents = (int)(request.Amount * 100)
                },
                cancellationToken);
            if (!paymentKeyResp.Success)
                return new PaymentOrderResult(false, orderResponse.PaymobOrderId, null, paymentKeyResp.ErrorMessage);

            // 4) Build payment url (iframe)
            var paymentUrl = BuildPaymentUrl(paymentKeyResp.PaymentKey!);

            return new PaymentOrderResult(true, orderResponse.PaymobOrderId, paymentUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateOrderAsync failed for user {UserId}", request.UserId);
            return new PaymentOrderResult(false, null, null, ex.Message);
        }
    }

    public async Task<PaymentVerificationResult> VerifyCallbackAsync(PaymentCallbackData callbackData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Best practice: verify by querying Paymob transaction/transaction-id or order details to confirm status & amount.
            var token = await GetAuthTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
                return new PaymentVerificationResult(false, false, null, null, "Failed to obtain auth token.");

            // Try to fetch transaction by transaction id returned from callback
            var tx = await GetTransactionByIdAsync(token, callbackData.PaymobTransactionId, cancellationToken);

            if (tx == null)
                return new PaymentVerificationResult(false, false, null, null, "Transaction not found at Paymob.");

            // Compare statuses and amounts (amounts in cents on Paymob — convert if needed)
            var status = tx.Status?.ToLowerInvariant() ?? string.Empty;
            var isSuccess = status == "paid" || status == "captured" || status == "success";

            decimal? amount = tx.AmountCents.HasValue ? tx.AmountCents.Value / 100m : (decimal?)null;

            return new PaymentVerificationResult(true, isSuccess, tx.Id, amount, isSuccess ? null : $"Status: {tx.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyCallbackAsync failed for PaymobTransactionId {TxId}", callbackData.PaymobTransactionId);
            return new PaymentVerificationResult(false, false, null, null, ex.Message);
        }
    }

    public async Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
                return new RefundResult(false, null, "Failed to obtain auth token.");

            // Refund endpoint (Paymob supports refund through API) - body may vary by region/account.
            var payload = new
            {
                transaction_id = request.PaymobTransactionId,
                amount_cents = (int)(request.Amount * 100),
                reason = request.Reason
            };

            var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/api/acceptance/void_refund")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
            };

            httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _http.SendAsync(httpReq, cancellationToken);
            var content = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Refund failed: {Status} - {Content}", resp.StatusCode, content);
                return new RefundResult(false, null, $"Refund API call failed: {content}");
            }

            // Try to parse refund id from response (best-effort)
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string? refundId = null;
            if (root.TryGetProperty("id", out var idProp))
                refundId = idProp.GetRawText().Trim('"');

            return new RefundResult(true, refundId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessRefundAsync error for transaction {TxId}", request.PaymobTransactionId);
            return new RefundResult(false, null, ex.Message);
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string paymobOrderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
                return new PaymentStatusResult("unknown", 0m, _settings.Currency ?? "EGP", null, "Failed to obtain auth token.");

            // Endpoint: retrieve transaction by order id — Paymob docs provide "Retrieve Transaction With Order ID"
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/acceptance/transactions?order={Uri.EscapeDataString(paymobOrderId)}";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _http.SendAsync(req, cancellationToken);
            var content = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("GetPaymentStatusAsync failed: {Status} - {Content}", resp.StatusCode, content);
                return new PaymentStatusResult("error", 0m, _settings.Currency ?? "EGP", null, content);
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Attempt to map common fields
            var status = root.GetProperty("success").GetBoolean() ? "paid" : (root.GetProperty("message").GetString() ?? "unknown");
            decimal amount = 0m;
            if (root.TryGetProperty("amount_cents", out var amountProp) && amountProp.ValueKind == JsonValueKind.Number)
                amount = amountProp.GetInt32() / 100m;

            DateTime? completedAt = null;
            if (root.TryGetProperty("created_at", out var createdAtProp) && createdAtProp.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(createdAtProp.GetString(), out var dt))
                    completedAt = dt;
            }

            return new PaymentStatusResult(status, amount, _settings.Currency ?? "EGP", completedAt, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPaymentStatusAsync error for order {OrderId}", paymobOrderId);
            return new PaymentStatusResult("error", 0m, _settings.Currency ?? "EGP", null, ex.Message);
        }
    }

    public async Task<string> GeneratePaymentUrlAsync(string paymobOrderId, CancellationToken cancellationToken = default)
    {
        // This method attempts to produce the payment url by generating a payment key using the order id.
        try
        {
            var token = await GetAuthTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Failed to obtain Paymob auth token.");

            // In many Paymob flows you call the payment_keys endpoint to create a payment_key that you embed in iframe url.
            // However GeneratePaymentUrlAsync is often a wrapper around GeneratePaymentKeyAsync + BuildPaymentUrl.
            // We'll attempt to generate a payment key with minimal payload.
            var pkResp = await GeneratePaymentKeyAsync(token, new PaymentOrderForKeyRequest { OrderId = paymobOrderId, AmountCents = null }, cancellationToken);
            if (!pkResp.Success || string.IsNullOrEmpty(pkResp.PaymentKey))
                throw new InvalidOperationException(pkResp.ErrorMessage ?? "Failed to generate payment key.");

            return BuildPaymentUrl(pkResp.PaymentKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeneratePaymentUrlAsync failed for order {OrderId}", paymobOrderId);
            throw;
        }
    }

    // ------------------------------
    // Helper internal methods
    // ------------------------------

    private async Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken)
    {
        // Paymob auth endpoint (see Paymob docs)
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/auth/tokens";

        var payload = new { api_key = _settings.ApiKey };

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
        };

        var resp = await _http.SendAsync(req, cancellationToken);
        var content = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("GetAuthTokenAsync failed: {Status} - {Content}", resp.StatusCode, content);
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                return tokenProp.GetString();

            if (doc.RootElement.TryGetProperty("token_type", out var tokenTypeProp))
                return tokenTypeProp.GetString();

            // fallback: try "auth_token" or nested structure (some regions)
            if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("token", out var nestedToken))
                return nestedToken.GetString();

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse auth token response: {Content}", content);
            return null;
        }
    }

    private async Task<(bool Success, string? PaymobOrderId, string? ErrorMessage)> RegisterEcommerceOrderAsync(string token, CreatePaymentOrderRequest request, CancellationToken cancellationToken)
    {
        // Order registration endpoint per Paymob ecommerce flow
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/ecommerce/orders";

        var payload = new
        {
            delivery_needed = "false",
            amount_cents = (int)(request.Amount * 100),
            currency = request.Currency ?? _settings.Currency,
            merchant_order_id = Guid.NewGuid().ToString("N"),
            items = new object[] { }, // optional: list of items
            // you can pass metadata or billing data as required
        };

        var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
        };
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.SendAsync(httpReq, cancellationToken);
        var content = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("RegisterEcommerceOrderAsync failed: {Status} - {Content}", resp.StatusCode, content);
            return (false, null, $"Order registration failed: {content}");
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // common response shape: { id: 12345, ... } or { token: ..., order: { id: ... } }
            if (root.TryGetProperty("id", out var idProp))
            {
                return (true, idProp.GetRawText().Trim('"'), null);
            }
            // nested: data.id or order.id
            if (root.TryGetProperty("order", out var orderEl) && orderEl.TryGetProperty("id", out var nestedId))
            {
                return (true, nestedId.GetRawText().Trim('"'), null);
            }
            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var dataId))
            {
                return (true, dataId.GetRawText().Trim('"'), null);
            }

            return (false, null, "Unable to parse order id from Paymob response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse register order response: {Content}", content);
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool Success, string? PaymentKey, string? ErrorMessage)> GeneratePaymentKeyAsync(string token, object orderForKey, CancellationToken cancellationToken)
    {
        // orderForKey could be CreatePaymentOrderRequest or custom request type
        // Endpoint: /api/acceptance/payment_keys
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/acceptance/payment_keys";

        // Build minimal required payload for payment key generation
        object payload;
        if (orderForKey is CreatePaymentOrderRequest createReq)
        {
            payload = new
            {
                amount_cents = (int)(createReq.Amount * 100),
                expiration = 3600,
                order_id = createReq switch
                {
                    _ => orderForKey is CreatePaymentOrderRequest r ? r.UserId.ToString() : null
                },
                billing_data = new
                {
                    first_name = _settings.DefaultCustomerName ?? "Customer",
                    email = _settings.DefaultCustomerEmail ?? "no-reply@devpioneers.com"
                },
                integration_id = _settings.IntegrationId
            };
        }
        else if (orderForKey is PaymentOrderForKeyRequest pkReq)
        {
            payload = new
            {
                amount_cents = pkReq.AmountCents ?? 0,
                expiration = 3600,
                order_id = pkReq.OrderId,
                billing_data = new
                {
                    first_name = _settings.DefaultCustomerName ?? "Customer",
                    email = _settings.DefaultCustomerEmail ?? "no-reply@devpioneers.com"
                },
                integration_id = _settings.IntegrationId
            };
        }
        else
        {
            payload = orderForKey;
        }

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.SendAsync(req, cancellationToken);
        var content = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("GeneratePaymentKeyAsync failed: {Status} - {Content}", resp.StatusCode, content);
            return (false, null, $"Payment key generation failed: {content}");
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("token", out var tokenProp))
            {
                return (true, tokenProp.GetString(), null);
            }
            if (root.TryGetProperty("payment_key", out var pkProp))
            {
                return (true, pkProp.GetString(), null);
            }
            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("token", out var dt))
            {
                return (true, dt.GetString(), null);
            }

            return (false, null, "Unable to parse payment key from Paymob response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse payment key response: {Content}", content);
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool Success, string? PaymentKey, string? ErrorMessage)> GeneratePaymentKeyAsync(string token, PaymentOrderForKeyRequest reqObj, CancellationToken cancellationToken)
        => await GeneratePaymentKeyAsync(token, (object)reqObj, cancellationToken);

    private string BuildPaymentUrl(string paymentKey)
    {
        // Iframe-based checkout url (common pattern):
        // https://accept.paymob.com/api/acceptance/iframes/{iframeId}?payment_token={paymentKey}
        var iframeId = _settings.IframeId;
        var baseIframe = $"{_settings.BaseUrl.TrimEnd('/')}/api/acceptance/iframes/{iframeId}";
        return $"{baseIframe}?payment_token={Uri.EscapeDataString(paymentKey)}";
    }

    private async Task<PaymobTransaction?> GetTransactionByIdAsync(string token, string txId, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/acceptance/transactions/{Uri.EscapeDataString(txId)}";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _http.SendAsync(req, cancellationToken);
            var content = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("GetTransactionByIdAsync failed: {Status} - {Content}", resp.StatusCode, content);
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // map a minimal transaction object
            var tx = new PaymobTransaction
            {
                Id = root.TryGetProperty("id", out var idP) ? idP.GetString() ?? txId : txId,
                Status = root.TryGetProperty("success", out var succ) && succ.GetBoolean() ? "paid" :
                         (root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null),
                AmountCents = root.TryGetProperty("amount_cents", out var amountProp) ? amountProp.GetInt32() : (int?)null
            };

            return tx;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTransactionByIdAsync parsing error for {TxId}", txId);
            return null;
        }
    }

    // small DTOs used internally
    private record PaymobTransaction
    {
        public string? Id { get; init; }
        public string? Status { get; init; }
        public int? AmountCents { get; init; }
    }

    private class PaymentOrderForKeyRequest
    {
        public string? OrderId { get; set; }
        public int? AmountCents { get; set; }
    }

    // Settings mapping class (local to file for convenience)
    private class PaymobSettings
    {
        public string BaseUrl { get; set; } = "https://accept.paymob.com";
        public string ApiKey { get; set; } = "";
        public string IntegrationId { get; set; } = "";
        public string IframeId { get; set; } = "";
        public string Currency { get; set; } = "EGP";
        public string? DefaultCustomerName { get; set; }
        public string? DefaultCustomerEmail { get; set; }
    }
}
