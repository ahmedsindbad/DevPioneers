// ============================================
// File: DevPioneers.Domain/Entities/Wallet.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.ValueObjects;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// User wallet entity
/// </summary>
public class Wallet : AuditableEntity
{
    /// <summary>
    /// User ID (one-to-one)
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Navigation: User
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Current balance
    /// </summary>
    public decimal Balance { get; set; } = 0;

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Current points
    /// </summary>
    public int Points { get; set; } = 0;

    /// <summary>
    /// Total earned (lifetime)
    /// </summary>
    public decimal TotalEarned { get; set; } = 0;

    /// <summary>
    /// Total spent (lifetime)
    /// </summary>
    public decimal TotalSpent { get; set; } = 0;

    /// <summary>
    /// Is wallet active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation: Wallet transactions
    /// </summary>
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();

    /// <summary>
    /// Credit wallet
    /// </summary>
    public void Credit(decimal amount, string description, int? relatedEntityId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be positive", nameof(amount));

        Balance += amount;
        TotalEarned += amount;
    }

    /// <summary>
    /// Debit wallet
    /// </summary>
    public void Debit(decimal amount, string description, int? relatedEntityId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Debit amount must be positive", nameof(amount));

        if (Balance < amount)
            throw new InvalidOperationException($"Insufficient balance. Available: {Balance}, Required: {amount}");

        Balance -= amount;
        TotalSpent += amount;
    }

    /// <summary>
    /// Add points
    /// </summary>
    public void AddPoints(int points)
    {
        if (points <= 0)
            throw new ArgumentException("Points must be positive", nameof(points));

        Points += points;
    }

    /// <summary>
    /// Deduct points
    /// </summary>
    public void DeductPoints(int points)
    {
        if (points <= 0)
            throw new ArgumentException("Points must be positive", nameof(points));

        if (Points < points)
            throw new InvalidOperationException($"Insufficient points. Available: {Points}, Required: {points}");

        Points -= points;
    }

    /// <summary>
    /// Check if wallet has sufficient balance
    /// </summary>
    public bool HasSufficientBalance(decimal amount) => Balance >= amount;

    /// <summary>
    /// Check if wallet has sufficient points
    /// </summary>
    public bool HasSufficientPoints(int points) => Points >= points;

    /// <summary>
    /// Get balance as Money value object
    /// </summary>
    public Money GetBalance() => new Money(Balance, Currency);

    /// <summary>
    /// Get points as Points value object
    /// </summary>
    public Points GetPoints() => new Points(Points);
}

