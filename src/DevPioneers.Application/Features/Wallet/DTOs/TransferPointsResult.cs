// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/TransferPointsResult.cs  
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

/// <summary>
/// Result model for points transfer operation
/// </summary>
public class TransferPointsResult
{
    public TransactionDto FromTransaction { get; set; } = null!;
    public TransactionDto ToTransaction { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess => !string.IsNullOrEmpty(Message) && !Message.Contains("error", StringComparison.OrdinalIgnoreCase);
}
