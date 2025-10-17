// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/TransferResultDto.cs
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

public class TransferResultDto
{
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
    public int FromTransactionId { get; set; }
    public int ToTransactionId { get; set; }
    public int FromBalanceAfter { get; set; }
    public int ToBalanceAfter { get; set; }
    public DateTime TransferredAt { get; set; }
 
    // Display properties
    public string PointsDisplay => $"{Points:N0} Points";
    public string FromBalanceDisplay => $"{FromBalanceAfter:N0} Points";
    public string ToBalanceDisplay => $"{ToBalanceAfter:N0} Points";
}
