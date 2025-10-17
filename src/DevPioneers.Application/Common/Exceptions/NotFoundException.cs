// ============================================
// File: DevPioneers.Application/Common/Exceptions/NotFoundException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception for when a requested resource is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException()
        : base()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
        EntityName = name;
        Key = key;
    }

    public string? EntityName { get; }
    public object? Key { get; }
}
