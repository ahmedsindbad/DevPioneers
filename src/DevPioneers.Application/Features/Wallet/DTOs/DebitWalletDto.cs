// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/DebitWalletDto.cs
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

public class DebitWalletDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Description { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}