// ============================================
// File: DevPioneers.Application/Common/Interfaces/IDateTime.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// DateTime service interface for testability
/// </summary>
public interface IDateTime
{
    /// <summary>
    /// Get current UTC date and time
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Get current local date and time
    /// </summary>
    DateTime Now { get; }
}