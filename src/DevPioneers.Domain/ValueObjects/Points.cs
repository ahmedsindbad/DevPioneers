// ============================================
// File: DevPioneers.Domain/ValueObjects/Points.cs
// ============================================
namespace DevPioneers.Domain.ValueObjects;

/// <summary>
/// Value object representing loyalty/reward points
/// </summary>
public sealed class Points : IEquatable<Points>
{
    /// <summary>
    /// Number of points
    /// </summary>
    public int Value { get; private set; }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private Points()
    {
    }

    /// <summary>
    /// Create new Points instance
    /// </summary>
    public Points(int value)
    {
        if (value < 0)
            throw new ArgumentException("Points cannot be negative", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Create zero points
    /// </summary>
    public static Points Zero() => new Points(0);

    /// <summary>
    /// Add points
    /// </summary>
    public Points Add(Points other)
    {
        return new Points(Value + other.Value);
    }

    /// <summary>
    /// Subtract points (if sufficient)
    /// </summary>
    public Points Subtract(Points other)
    {
        if (Value < other.Value)
            throw new InvalidOperationException($"Insufficient points. Available: {Value}, Required: {other.Value}");

        return new Points(Value - other.Value);
    }

    /// <summary>
    /// Check if points are sufficient for deduction
    /// </summary>
    public bool IsSufficient(Points required) => Value >= required.Value;

    /// <summary>
    /// Check if points are zero
    /// </summary>
    public bool IsZero() => Value == 0;

    /// <summary>
    /// Convert money to points (based on conversion rate)
    /// </summary>
    public static Points FromMoney(Money money, decimal pointsPerUnit = 10)
    {
        if (money.Currency != "EGP")
            throw new InvalidOperationException($"Cannot convert {money.Currency} to points");

        int points = (int)Math.Floor(money.Amount * pointsPerUnit);
        return new Points(points);
    }

    /// <summary>
    /// Convert points to money (based on conversion rate)
    /// </summary>
    public Money ToMoney(decimal unitsPerPoint = 0.1m, string currency = "EGP")
    {
        decimal amount = Value * unitsPerPoint;
        return new Money(amount, currency);
    }

    // Operator overloading
    public static Points operator +(Points left, Points right) => left.Add(right);
    public static Points operator -(Points left, Points right) => left.Subtract(right);
    public static bool operator >(Points left, Points right) => left.Value > right.Value;
    public static bool operator <(Points left, Points right) => left.Value < right.Value;
    public static bool operator >=(Points left, Points right) => left.Value >= right.Value;
    public static bool operator <=(Points left, Points right) => left.Value <= right.Value;

    // Equality
    public bool Equals(Points? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Points);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Points? left, Points? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Points? left, Points? right) => !(left == right);

    public override string ToString() => $"{Value:N0} Points";
}
