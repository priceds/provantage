using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_normalizes_currency_and_adds_amounts()
    {
        var baseAmount = new Money(125.50m, "usd");
        var extra = new Money(24.50m, "USD");

        var result = baseAmount.Add(extra);

        Assert.Equal(150.00m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Add_with_different_currency_throws()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        var exception = Assert.Throws<InvalidOperationException>(() => usd.Add(eur));

        Assert.Contains("different currencies", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Multiply_preserves_currency()
    {
        var amount = new Money(19.99m, "usd");

        var result = amount.Multiply(3);

        Assert.Equal(59.97m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }
}
