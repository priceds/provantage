using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProVantage.Domain.Entities;

namespace ProVantage.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).HasMaxLength(100).IsRequired();
        builder.Property(i => i.InternalReference).HasMaxLength(50).IsRequired();
        builder.HasIndex(i => new { i.TenantId, i.InternalReference }).IsUnique();
        builder.Property(i => i.DisputeNotes).HasMaxLength(2000);
        builder.Ignore(i => i.TotalAmount);

        builder.HasOne(i => i.PurchaseOrder).WithMany(po => po.Invoices)
            .HasForeignKey(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Vendor).WithMany()
            .HasForeignKey(i => i.VendorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");
        builder.HasKey(li => li.Id);
        builder.Property(li => li.ItemDescription).HasMaxLength(500).IsRequired();
        builder.Property(li => li.ItemCode).HasMaxLength(50);
        builder.Property(li => li.Quantity).HasPrecision(18, 4);
        builder.Ignore(li => li.TotalPrice);

        builder.OwnsOne(li => li.UnitPrice, m =>
        {
            m.Property(p => p.Amount).HasPrecision(18, 2).HasColumnName("UnitPrice");
            m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("UnitPriceCurrency");
        });

        builder.HasOne(li => li.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipts");
        builder.HasKey(gr => gr.Id);
        builder.Property(gr => gr.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(gr => new { gr.TenantId, gr.ReceiptNumber }).IsUnique();
        builder.Property(gr => gr.Notes).HasMaxLength(2000);
        builder.Property(gr => gr.DeliveryNote).HasMaxLength(500);
        builder.Property(gr => gr.ItemCode).HasMaxLength(50);
        builder.Property(gr => gr.QuantityReceived).HasPrecision(18, 4);
        builder.Property(gr => gr.QuantityRejected).HasPrecision(18, 4);
        builder.Property(gr => gr.RejectionReason).HasMaxLength(500);

        builder.HasOne(gr => gr.PurchaseOrder).WithMany(po => po.GoodsReceipts)
            .HasForeignKey(gr => gr.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(gr => gr.ReceivedBy).WithMany()
            .HasForeignKey(gr => gr.ReceivedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contracts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ContractNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.ContractNumber }).IsUnique();
        builder.Property(c => c.Title).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Terms).HasMaxLength(5000);

        builder.OwnsOne(c => c.Duration, d =>
        {
            d.Property(p => p.StartDate).HasColumnName("StartDate");
            d.Property(p => p.EndDate).HasColumnName("EndDate");
        });

        builder.OwnsOne(c => c.TotalValue, m =>
        {
            m.Property(p => p.Amount).HasPrecision(18, 2).HasColumnName("TotalValue");
            m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("TotalValueCurrency");
        });

        builder.HasOne(c => c.Vendor).WithMany(v => v.Contracts)
            .HasForeignKey(c => c.VendorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.ToTable("ApprovalWorkflows");
        builder.HasKey(w => w.Id);
        builder.HasOne(w => w.Requisition).WithMany(r => r.ApprovalWorkflows)
            .HasForeignKey(w => w.RequisitionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Comments).HasMaxLength(1000);
        builder.Ignore(s => s.IsPending);

        builder.HasOne(s => s.Workflow).WithMany(w => w.Steps)
            .HasForeignKey(s => s.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Approver).WithMany()
            .HasForeignKey(s => s.ApproverId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BudgetAllocationConfiguration : IEntityTypeConfiguration<BudgetAllocation>
{
    public void Configure(EntityTypeBuilder<BudgetAllocation> builder)
    {
        builder.ToTable("BudgetAllocations");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Department).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Category).HasMaxLength(100);
        builder.Ignore(b => b.AvailableAmount);
        builder.Ignore(b => b.UtilizationPercent);
        builder.Ignore(b => b.IsOverBudget);

        builder.OwnsOne(b => b.AllocatedAmount, m =>
        {
            m.Property(p => p.Amount).HasPrecision(18, 2).HasColumnName("AllocatedAmount");
            m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("AllocatedCurrency");
        });
        builder.OwnsOne(b => b.CommittedAmount, m =>
        {
            m.Property(p => p.Amount).HasPrecision(18, 2).HasColumnName("CommittedAmount");
            m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("CommittedCurrency");
        });
        builder.OwnsOne(b => b.SpentAmount, m =>
        {
            m.Property(p => p.Amount).HasPrecision(18, 2).HasColumnName("SpentAmount");
            m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("SpentCurrency");
        });
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.ActionUrl).HasMaxLength(500);
        builder.Property(n => n.EntityType).HasMaxLength(100);

        builder.HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserName).HasMaxLength(256);
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
