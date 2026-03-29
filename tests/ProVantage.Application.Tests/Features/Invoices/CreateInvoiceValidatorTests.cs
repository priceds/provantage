using ProVantage.Application.Features.Invoices.Commands;
using ProVantage.Application.Features.Invoices.DTOs;

namespace ProVantage.Application.Tests.Features.Invoices;

public class CreateInvoiceValidatorTests
{
    private readonly CreateInvoiceValidator _validator = new();

    [Fact]
    public void Validate_requires_due_date_after_invoice_date_and_at_least_one_line_item()
    {
        var command = new CreateInvoiceCommand(new CreateInvoiceRequest(
            "INV-2026-1001",
            Guid.NewGuid(),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            []));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Data.DueDate");
        Assert.Contains(result.Errors, e => e.PropertyName == "Data.LineItems");
    }

    [Fact]
    public void Validate_accepts_valid_invoice_payload()
    {
        var command = new CreateInvoiceCommand(new CreateInvoiceRequest(
            "INV-2026-1002",
            Guid.NewGuid(),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30),
            [
                new CreateInvoiceLineItemRequest("Laptop", "LTP-01", 2, 1500m, "USD")
            ]));

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
