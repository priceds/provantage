using ProVantage.Application.Features.Contracts.Commands;

namespace ProVantage.Application.Tests.Features.Contracts;

public class CreateContractValidatorTests
{
    private readonly CreateContractValidator _validator = new();

    [Fact]
    public void Validate_returns_errors_for_invalid_dates_and_value()
    {
        var command = new CreateContractCommand(
            Guid.NewGuid(),
            "Support Agreement",
            DateTime.UtcNow.Date.AddDays(10),
            DateTime.UtcNow.Date.AddDays(5),
            0m,
            "US");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateContractCommand.EndDate));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateContractCommand.Value));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateContractCommand.Currency));
    }

    [Fact]
    public void Validate_passes_for_well_formed_contract_request()
    {
        var command = new CreateContractCommand(
            Guid.NewGuid(),
            "Support Agreement",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(60),
            15000m,
            "USD");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
