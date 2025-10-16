// ============================================
// File: DevPioneers.Domain/Enums/PaymentMethod.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// Payment method type
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Credit/Debit card
    /// </summary>
    Card = 1,

    /// <summary>
    /// Wallet balance
    /// </summary>
    Wallet = 2,

    /// <summary>
    /// Bank transfer
    /// </summary>
    BankTransfer = 3,

    /// <summary>
    /// Mobile wallet (Vodafone Cash, etc.)
    /// </summary>
    MobileWallet = 4,

    /// <summary>
    /// Cash on delivery
    /// </summary>
    Cash = 5,

    /// <summary>
    /// Other payment methods
    /// </summary>
    Other = 99
}
