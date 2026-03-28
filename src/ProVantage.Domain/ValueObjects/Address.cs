namespace ProVantage.Domain.ValueObjects;

public sealed record Address
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;

    public override string ToString() => $"{Street}, {City}, {State} {PostalCode}, {Country}";
}
