using Microsoft.EntityFrameworkCore;
using ProVantage.Domain.Entities;

namespace ProVantage.Domain.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so the Domain/Application layers
/// never depend on Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<VendorContact> VendorContacts { get; }
    DbSet<PurchaseRequisition> PurchaseRequisitions { get; }
    DbSet<RequisitionLineItem> RequisitionLineItems { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<OrderLineItem> OrderLineItems { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ApprovalWorkflow> ApprovalWorkflows { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }
    DbSet<BudgetAllocation> BudgetAllocations { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
