// ============================================
// File: DevPioneers.Application/Common/Exceptions/PaymentException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception specific to payment operations
/// </summary>
public class PaymentException : Exception
{
    public PaymentException()
        : base("A payment error occurred.")
    {
    }

    public PaymentException(string message)
        : base(message)
    {
    }

    public PaymentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PaymentException(string operation, string paymobOrderId, string? errorCode = null)
        : base($"Payment {operation} failed for order {paymobOrderId}" +
               (errorCode != null ? $" (Code: {errorCode})" : ""))
    {
        Operation = operation;
        PaymobOrderId = paymobOrderId;
        ErrorCode = errorCode;
    }

    public string? Operation { get; }
    public string? PaymobOrderId { get; }
    public string? PaymobTransactionId { get; set; }
    public string? ErrorCode { get; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}
