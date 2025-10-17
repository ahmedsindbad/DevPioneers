// ============================================
// File: DevPioneers.Application/Common/Exceptions/ExternalServiceException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception for external service failures (Paymob, Email, etc.)
/// </summary>
public class ExternalServiceException : Exception
{
    public ExternalServiceException()
        : base("An external service error occurred.")
    {
    }

    public ExternalServiceException(string message)
        : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ExternalServiceException(string serviceName, string operation, string? errorCode = null)
        : base($"External service '{serviceName}' failed during '{operation}'" + 
               (errorCode != null ? $" (Code: {errorCode})" : ""))
    {
        ServiceName = serviceName;
        Operation = operation;
        ErrorCode = errorCode;
    }

    public string? ServiceName { get; }
    public string? Operation { get; }
    public string? ErrorCode { get; }
    public Dictionary<string, object> ServiceData { get; } = new();
}