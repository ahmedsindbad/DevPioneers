// ============================================
// File: DevPioneers.Application/Common/Interfaces/IDateTime.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Service for date and time operations
/// Makes testing easier by allowing time mocking
/// </summary>
public interface IDateTime
{
    /// <summary>
    /// Current UTC date and time
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Current local date and time
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Current UTC date only
    /// </summary>
    DateOnly UtcToday { get; }

    /// <summary>
    /// Current local date only
    /// </summary>
    DateOnly Today { get; }

    /// <summary>
    /// Unix timestamp (seconds since 1970)
    /// </summary>
    long UnixTimestamp { get; }

    /// <summary>
    /// Add business days to a date (excluding weekends)
    /// </summary>
    DateTime AddBusinessDays(DateTime date, int businessDays);

    /// <summary>
    /// Check if date is weekend
    /// </summary>
    bool IsWeekend(DateTime date);

    /// <summary>
    /// Get start of day (00:00:00)
    /// </summary>
    DateTime StartOfDay(DateTime date);

    /// <summary>
    /// Get end of day (23:59:59.999)
    /// </summary>
    DateTime EndOfDay(DateTime date);
}