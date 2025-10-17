// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/WalletStatisticsDto.cs
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

/// <summary>
/// DTO for wallet statistics
/// </summary>
public class WalletStatisticsDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Wallet ID
    /// </summary>
    public int WalletId { get; set; }

    /// <summary>
    /// Current wallet balance
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Current points
    /// </summary>
    public int CurrentPoints { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Statistics period start date
    /// </summary>
    public DateTime PeriodFromDate { get; set; }

    /// <summary>
    /// Statistics period end date
    /// </summary>
    public DateTime PeriodToDate { get; set; }

    /// <summary>
    /// Total credits in period
    /// </summary>
    public decimal TotalCredits { get; set; }

    /// <summary>
    /// Total debits in period
    /// </summary>
    public decimal TotalDebits { get; set; }

    /// <summary>
    /// Net amount (credits - debits)
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Number of transactions in period
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Points earned in period
    /// </summary>
    public int PointsEarned { get; set; }

    /// <summary>
    /// Points spent in period
    /// </summary>
    public int PointsSpent { get; set; }

    /// <summary>
    /// Net points (earned - spent)
    /// </summary>
    public int NetPoints { get; set; }

    /// <summary>
    /// Lifetime total earned
    /// </summary>
    public decimal LifetimeEarned { get; set; }

    /// <summary>
    /// Lifetime total spent
    /// </summary>
    public decimal LifetimeSpent { get; set; }

    /// <summary>
    /// Transactions breakdown by type
    /// </summary>
    public Dictionary<string, object> TransactionsByType { get; set; } = new();

    // Display properties
    public string CurrentBalanceDisplay => $"{CurrentBalance:N2} {Currency}";
    public string CurrentPointsDisplay => $"{CurrentPoints:N0} Points";
    public string TotalCreditsDisplay => $"{TotalCredits:N2} {Currency}";
    public string TotalDebitsDisplay => $"{TotalDebits:N2} {Currency}";
    public string NetAmountDisplay => $"{NetAmount:N2} {Currency}";
    public string PointsEarnedDisplay => $"{PointsEarned:N0} Points";
    public string PointsSpentDisplay => $"{PointsSpent:N0} Points";
    public string NetPointsDisplay => $"{NetPoints:N0} Points";
    public string LifetimeEarnedDisplay => $"{LifetimeEarned:N2} {Currency}";
    public string LifetimeSpentDisplay => $"{LifetimeSpent:N2} {Currency}";
    public string PeriodDisplay => $"{PeriodFromDate:yyyy-MM-dd} to {PeriodToDate:yyyy-MM-dd}";
    public bool IsPositiveBalance => CurrentBalance > 0;
    public bool IsPositiveNet => NetAmount > 0;
    public bool HasTransactions => TransactionCount > 0;
}
