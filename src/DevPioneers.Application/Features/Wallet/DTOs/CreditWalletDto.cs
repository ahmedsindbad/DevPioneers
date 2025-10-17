// ============================================
// File: DevPioneers.Application/Features/Wallet/DTOs/CreditWalletDto.cs
// ============================================
namespace DevPioneers.Application.Features.Wallet.DTOs;

public class CreditWalletDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Description { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}
