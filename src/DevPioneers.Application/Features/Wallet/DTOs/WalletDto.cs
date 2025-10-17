// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/WalletDto.cs (Updated Version)
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

/// <summary>
/// DTO for wallet information
/// </summary>
public class WalletDto
{
    /// <summary>
    /// Wallet ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Current balance
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Current points
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Total earned (lifetime)
    /// </summary>
    public decimal TotalEarned { get; set; }

    /// <summary>
    /// Total spent (lifetime)
    /// </summary>
    public decimal TotalSpent { get; set; }

    /// <summary>
    /// Is wallet active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }

    // User Information (for admin queries)
    /// <summary>
    /// User email (populated in admin queries)
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// User full name (populated in admin queries)
    /// </summary>
    public string? UserFullName { get; set; }

    /// <summary>
    /// User mobile (populated in admin queries)
    /// </summary>
    public string? UserMobile { get; set; }

    // Display properties
    public string BalanceDisplay => $"{Balance:N2} {Currency}";
    public string PointsDisplay => $"{Points:N0} Points";
    public string TotalEarnedDisplay => $"{TotalEarned:N2} {Currency}";
    public string TotalSpentDisplay => $"{TotalSpent:N2} {Currency}";
    public string NetLifetimeDisplay => $"{(TotalEarned - TotalSpent):N2} {Currency}";
    public string StatusDisplay => IsActive ? "Active" : "Inactive";
    public bool HasSufficientBalance => Balance > 0;
    public bool HasPoints => Points > 0;
}