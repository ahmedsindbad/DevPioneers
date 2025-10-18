// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/WalletStatisticsDto.cs
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

/// <summary>
/// DTO for individual user wallet statistics
/// </summary>
public class WalletStatisticsDto
{
    public int UserId { get; set; }
    public int WalletId { get; set; }
    public decimal CurrentBalance { get; set; }
    public int CurrentPoints { get; set; }
    public string Currency { get; set; } = "EGP";
    
    // Period information
    public DateTime PeriodFromDate { get; set; }
    public DateTime PeriodToDate { get; set; }
    
    // Period statistics
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
    
    // Points statistics
    public int PointsEarned { get; set; }
    public int PointsSpent { get; set; }
    public int NetPoints { get; set; }
    
    // Lifetime statistics
    public decimal LifetimeEarned { get; set; }
    public decimal LifetimeSpent { get; set; }
    
    // Transaction breakdown
    public Dictionary<string, object> TransactionsByType { get; set; } = new();
    
    // Display properties
    public string CurrentBalanceDisplay => $"{CurrentBalance:C} {Currency}";
    public string CurrentPointsDisplay => $"{CurrentPoints:N0} Points";
    public string NetAmountDisplay => $"{NetAmount:C} {Currency}";
    public string NetPointsDisplay => $"{NetPoints:N0} Points";
}

