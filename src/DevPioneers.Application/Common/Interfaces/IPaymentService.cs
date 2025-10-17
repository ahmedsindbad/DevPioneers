// ============================================
// File: DevPioneers.Application/Common/Interfaces/IPaymentService.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Payment service interface for Paymob integration
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Create payment order with Paymob
    /// </summary>
    Task<PaymentOrderResult> CreateOrderAsync(
        CreatePaymentOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment callback from Paymob
    /// </summary>
    Task<PaymentVerificationResult> VerifyCallbackAsync(
        PaymentCallbackData callbackData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process refund
    /// </summary>
    Task<RefundResult> ProcessRefundAsync(
        ProcessRefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment status from Paymob
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(
        string paymobOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate payment URL for frontend
    /// </summary>
    Task<string> GeneratePaymentUrlAsync(
        string paymobOrderId,
        CancellationToken cancellationToken = default);
}
// ============================================
// Payment service DTOs
// ============================================

public record CreatePaymentOrderRequest(
    int UserId,
    decimal Amount,
    string Currency,
    string Description,
    int? SubscriptionPlanId = null,
    Dictionary<string, object>? Metadata = null);

public record PaymentOrderResult(
    bool Success,
    string? PaymobOrderId,
    string? PaymentUrl,
    string? ErrorMessage);

public record PaymentCallbackData(
    string PaymobOrderId,
    string PaymobTransactionId,
    string Status,
    decimal Amount,
    string Currency,
    Dictionary<string, object>? AdditionalData);

public record PaymentVerificationResult(
    bool IsValid,
    bool IsSuccess,
    string? TransactionId,
    decimal? Amount,
    string? ErrorMessage);

public record ProcessRefundRequest(
    string PaymobTransactionId,
    decimal Amount,
    string Reason);

public record RefundResult(
    bool Success,
    string? RefundId,
    string? ErrorMessage);

public record PaymentStatusResult(
    string Status,
    decimal Amount,
    string Currency,
    DateTime? CompletedAt,
    string? ErrorMessage);
