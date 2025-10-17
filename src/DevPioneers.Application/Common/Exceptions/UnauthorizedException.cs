// ============================================
// File: DevPioneers.Application/Common/Exceptions/UnauthorizedException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception for unauthorized access attempts
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException()
        : base("Unauthorized access.")
    {
    }

    public UnauthorizedException(string message)
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UnauthorizedException(string action, string? resource = null)
        : base($"Unauthorized access to {action}" + (resource != null ? $" on {resource}" : "."))
    {
        Action = action;
        Resource = resource;
    }

    public string? Action { get; }
    public string? Resource { get; }
}
