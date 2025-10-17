// ============================================
// File: DevPioneers.Application/Common/Exceptions/ConflictException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception for resource conflicts (409 Conflict)
/// </summary>
public class ConflictException : Exception
{
    public ConflictException()
        : base("A conflict occurred with the current state of the resource.")
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConflictException(string resource, string reason)
        : base($"Conflict with {resource}: {reason}")
    {
        Resource = resource;
        Reason = reason;
    }

    public string? Resource { get; }
    public string? Reason { get; }
}