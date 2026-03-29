using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Features.Invoices.Commands;
using ProVantage.Application.Tests.Common.Testing;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Tests.Features.Invoices;

public class PerformThreeWayMatchHandlerTests
{
    [Fact]
    public async Task Handle_marks_invoice_as_matched_and_updates_budget_when_lines_are_within_tolerance()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new TestCurrentTenantService(tenantId);
        await using var db = TestApplicationDbContextFactory.CreateContext(tenant);

        var invoiceId = await SeedMatchingScenarioAsync(db, tenantId, invoiceQuantity: 8, receivedQuantity: 8);
        var handler = new PerformThreeWayMatchHandler(db, tenant);

        var result = await handler.Handle(new PerformThreeWayMatchCommand(invoiceId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsFullyMatched);
        Assert.Equal(InvoiceStatus.Matched, result.Value.ResultStatus);

        var invoice = await db.Invoices.SingleAsync(i => i.Id == invoiceId);
        var budget = await db.BudgetAllocations.SingleAsync();

        Assert.Equal(InvoiceStatus.Matched, invoice.Status);
        Assert.Equal(900m, budget.CommittedAmount.Amount);
        Assert.Equal(900m, budget.SpentAmount.Amount);
    }

    [Fact]
    public async Task Handle_marks_invoice_as_disputed_when_quantity_exceeds_receipt_tolerance()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new TestCurrentTenantService(tenantId);
        await using var db = TestApplicationDbContextFactory.CreateContext(tenant);

        var invoiceId = await SeedMatchingScenarioAsync(db, tenantId, invoiceQuantity: 10, receivedQuantity: 8);
        var handler = new PerformThreeWayMatchHandler(db, tenant);

        var result = await handler.Handle(new PerformThreeWayMatchCommand(invoiceId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsFullyMatched);
        Assert.Equal(InvoiceStatus.Disputed, result.Value.ResultStatus);
        Assert.Contains("exceeds received qty", result.Value.LineResults.Single().DiscrepancyNote, StringComparison.OrdinalIgnoreCase);

        var invoice = await db.Invoices.SingleAsync(i => i.Id == invoiceId);
        var budget = await db.BudgetAllocations.SingleAsync();

        Assert.Equal(InvoiceStatus.Disputed, invoice.Status);
        Assert.Equal(100m, budget.SpentAmount.Amount);
        Assert.Equal(1700m, budget.CommittedAmount.Amount);
    }

    private static async Task<Guid> SeedMatchingScenarioAsync(
        ProVantage.Infrastructure.Persistence.ApplicationDbContext db,
        Guid tenantId,
        decimal invoiceQuantity,
        decimal receivedQuantity)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Acme Test",
            Subdomain = "acme-test",
            PriceVarianceTolerancePercent = 5m,
            QuantityVarianceTolerancePercent = 2m
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "buyer@test.com",
            PasswordHash = "hashed",
            FirstName = "Buyer",
            LastName = "User",
            Role = "Buyer",
            Department = "Procurement"
        };
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyName = "Northwind Systems",
            Email = "ops@northwind.example",
            Phone = "555-0110",
            Category = "IT",
            Status = VendorStatus.Approved,
            PaymentTerms = "Net 30",
            Address = new Address
            {
                Street = "10 Tech Park",
                City = "Seattle",
                State = "WA",
                PostalCode = "98101",
                Country = "USA"
            }
        };
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendor.Id,
            OrderNumber = "PO-2026-0001",
            Status = OrderStatus.Sent,
            OrderDate = DateTime.UtcNow.Date.AddDays(-5),
            ExpectedDeliveryDate = DateTime.UtcNow.Date.AddDays(5),
            PaymentTerms = "Net 30",
            ShippingAddress = "100 Market Street"
        };
        purchaseOrder.LineItems.Add(new OrderLineItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ItemDescription = "Laptop",
            ItemCode = "LTP-01",
            QuantityOrdered = 10,
            QuantityReceived = receivedQuantity,
            UnitPrice = new Money(100m, "USD")
        });

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendor.Id,
            PurchaseOrderId = purchaseOrder.Id,
            InvoiceNumber = "INV-2026-0001",
            InternalReference = "INV-2026-00001",
            Status = InvoiceStatus.Pending,
            InvoiceDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30)
        };
        invoice.LineItems.Add(new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ItemDescription = "Laptop",
            ItemCode = "LTP-01",
            Quantity = invoiceQuantity,
            UnitPrice = new Money(100m, "USD")
        });

        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.Vendors.Add(vendor);
        db.PurchaseOrders.Add(purchaseOrder);
        db.Invoices.Add(invoice);
        db.GoodsReceipts.Add(new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PurchaseOrderId = purchaseOrder.Id,
            ReceivedById = user.Id,
            ReceiptNumber = "GR-2026-0001",
            ItemCode = "LTP-01",
            QuantityReceived = receivedQuantity,
            QuantityRejected = 0,
            ReceivedDate = DateTime.UtcNow.Date.AddDays(-1)
        });
        db.BudgetAllocations.Add(new BudgetAllocation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Department = "IT",
            Category = "Hardware",
            Period = BudgetPeriod.Annual,
            FiscalYear = DateTime.UtcNow.Year,
            AllocatedAmount = new Money(5000m, "USD"),
            CommittedAmount = new Money(1700m, "USD"),
            SpentAmount = new Money(100m, "USD")
        });

        await db.SaveChangesAsync();
        return invoice.Id;
    }
}
