// ============================================
// File: DevPioneers.Application/Common/Exceptions/BusinessException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception for business rule violations
/// </summary>
public class BusinessException : Exception
{
    public BusinessException()
        : base("A business rule violation occurred.")
    {
    }

    public BusinessException(string message)
        : base(message)
    {
    }

    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BusinessException(string message, string code)
        : base(message)
    {
        Code = code;
    }

    public BusinessException(string message, string code, Dictionary<string, object>? details)
        : base(message)
    {
        Code = code;
        Details = details ?? new Dictionary<string, object>();
    }

    public string? Code { get; }
    public Dictionary<string, object> Details { get; } = new();
}