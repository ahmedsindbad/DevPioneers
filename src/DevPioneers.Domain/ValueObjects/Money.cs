// ============================================
// File: DevPioneers.Domain/ValueObjects/Money.cs
// ============================================
namespace DevPioneers.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary amount with currency
/// </summary>
public sealed class Money : IEquatable<Money>
{
    /// <summary>
    /// Amount value
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private Money()
    {
        Currency = "EGP"; // Default currency
    }

    /// <summary>
    /// Create new Money instance
    /// </summary>
    public Money(decimal amount, string currency = "EGP")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = Math.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Create zero money
    /// </summary>
    public static Money Zero(string currency = "EGP") => new Money(0, currency);

    /// <summary>
    /// Add two money amounts (same currency only)
    /// </summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtract two money amounts (same currency only)
    /// </summary>
    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Multiply money by a factor
    /// </summary>
    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }

    /// <summary>
    /// Check if amount is zero
    /// </summary>
    public bool IsZero() => Amount == 0;

    /// <summary>
    /// Check if amount is positive
    /// </summary>
    public bool IsPositive() => Amount > 0;

    /// <summary>
    /// Check if amount is negative
    /// </summary>
    public bool IsNegative() => Amount < 0;

    // Operator overloading
    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);
    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;
    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    // Equality
    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money? left, Money? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    public override string ToString() => $"{Amount:N2} {Currency}";
}