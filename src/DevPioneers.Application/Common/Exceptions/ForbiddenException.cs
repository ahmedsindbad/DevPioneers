// ============================================
// File: DevPioneers.Application/Common/Exceptions/ForbiddenException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception for forbidden access (403 Forbidden)
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException()
        : base("Access to the requested resource is forbidden.")
    {
    }

    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ForbiddenException(string action, string resource)
        : base($"Access forbidden: Cannot {action} {resource}")
    {
        Action = action;
        Resource = resource;
    }

    public string? Action { get; }
    public string? Resource { get; }
}