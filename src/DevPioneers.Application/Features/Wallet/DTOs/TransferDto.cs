// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/TransferDto.cs
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

public class TransferDto
{
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
}
