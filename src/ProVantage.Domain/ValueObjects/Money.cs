namespace ProVantage.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a monetary amount with currency.
/// Ensures consistent money handling across the domain.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";

    public Money() { }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot operate on different currencies: {Currency} vs {other.Currency}");
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
