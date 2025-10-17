// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/TransactionDto.cs
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Wallet.DTOs;

public class TransactionDto : BaseDto
{
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public int? TransferToUserId { get; set; }
 
    // Calculated properties
    public bool IsCredit => Type.Contains("Credit");
    public bool IsDebit => Type.Contains("Debit");
    public bool IsPointsTransaction => Currency == "PTS";
    public bool IsMoneyTransaction => Currency != "PTS";
    public bool IsTransfer => TransferToUserId.HasValue;
 
    // Display properties
    public string AmountDisplay => IsPointsTransaction ? 
        $"{Amount:N0} Points" : 
        $"{Amount:C} {Currency}";
 
    public string BalanceDisplay => IsPointsTransaction ? 
        $"{BalanceAfter:N0} Points" : 
        $"{BalanceAfter:C} {Currency}";
 
    public string ChangeIndicator => IsCredit ? "+" : "-";
    public string ChangeColor => IsCredit ? "text-success" : "text-danger";
}
