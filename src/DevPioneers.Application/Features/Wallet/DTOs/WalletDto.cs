// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/WalletDto.cs (Update)
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Wallet.DTOs;

public class WalletDto : BaseDto
{
    public int UserId { get; set; }
    public decimal Balance { get; set; }
    public int Points { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal TotalEarned { get; set; }
    public decimal TotalSpent { get; set; }
    public bool IsActive { get; set; }
    
    // Calculated properties
    public string BalanceDisplay => $"{Balance:C} {Currency}";
    public string PointsDisplay => $"{Points:N0} Points";
    
    // Points conversion (assuming 1 EGP = 10 points)
    public decimal PointsValue => Points / 10m;
    public string PointsValueDisplay => $"{PointsValue:C} {Currency}";
    
    // Total wallet value
    public decimal TotalValue => Balance + PointsValue;
    public string TotalValueDisplay => $"{TotalValue:C} {Currency}";
    
    // Status indicators
    public bool HasBalance => Balance > 0;
    public bool HasPoints => Points > 0;
    public bool IsEmpty => Balance == 0 && Points == 0;
}