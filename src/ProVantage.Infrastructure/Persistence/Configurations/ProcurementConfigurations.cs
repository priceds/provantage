using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProVantage.Domain.Entities;

namespace ProVantage.Infrastructure.Persistence.Configurations;

public class PurchaseRequisitionConfiguration : IEntityTypeConfiguration<PurchaseRequisition>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisition> builder)
    {
        builder.ToTable("PurchaseRequisitions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequisitionNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.RequisitionNumber }).IsUnique();
        builder.Property(r => r.Title).HasMaxLength(300).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.Department).HasMaxLength(100);
        builder.Property(r => r.RejectionReason).HasMaxLength(1000);
        builder.Ignore(r => r.TotalAmount);

        builder.HasOne(r => r.RequestedBy).WithMany()
            .HasForeignKey(r => r.RequestedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.ApprovedBy).WithMany()
            .HasForeignKey(r => r.ApprovedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.PreferredVendor).WithMany()
            .HasForeignKey(r => r.PreferredVendorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RequisitionLineItemConfiguration : IEntityTypeConfiguration<RequisitionLineItem>
{
    public void Configure(EntityTypeBuilder<RequisitionLineItem> builder)
    {
        builder.ToTable("RequisitionLineItems");
        builder.HasKey(li => li.Id);
        builder.Property(li => li.ItemDescription).HasMaxLength(500).IsRequired();
        builder.Property(li => li.ItemCode).HasMaxLength(50);
        builder.Property(li => li.Category).HasMaxLength(100);
        builder.Property(li => li.UnitOfMeasure).HasMaxLength(10);
        builder.Property(li => li.Quantity).HasPrecision(18, 4);
        builder.Ignore(li => li.TotalPrice);

        builder.OwnsOne(li => li.UnitPrice, m =>
        {
            m.Property(p => p.Amount).HasPrecision(18, 2).HasColumnName("UnitPrice");
            m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("UnitPriceCurrency");
        });

        builder.HasOne(li => li.Requisition)
            .WithMany(r => r.LineItems)
            .HasForeignKey(li => li.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(po => po.Id);
        builder.Property(po => po.OrderNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(po => new { po.TenantId, po.OrderNumber }).IsUnique();
        builder.Property(po => po.PaymentTerms).HasMaxLength(50);
        builder.Property(po => po.ShippingAddress).HasMaxLength(500);
        builder.Property(po => po.Notes).HasMaxLength(2000);
        builder.Ignore(po => po.TotalAmount);

        builder.HasOne(po => po.Vendor).WithMany(v => v.PurchaseOrders)
            .HasForeignKey(po => po.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(po => po.Requisition).WithMany()
            .HasForeignKey(po => po.RequisitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderLineItemConfiguration : IEntityTypeConfiguration<OrderLineItem>
{
    public void Configure(EntityTypeBuilder<OrderLineItem> builder)
    {
        builder.ToTable("OrderLineItems");
        builder.HasKey(li => li.Id);
        builder.Property(li => li.ItemDescription).HasMaxLength(500).IsRequired();
        builder.Property(li => li.ItemCode).HasMaxLength(50);
        builder.Property(li => li.UnitOfMeasure).HasMaxLength(10);
        builder.Property(li => li.QuantityOrdered).HasPrecision(18, 4);
        builder.Property(li => li.QuantityReceived).HasPrecision(18, 4);
        builder.Ignore(li => li.TotalPrice);
        builder.Ignore(li => li.IsFullyReceived);

        builder.OwnsOne(li => li.UnitPrice, m =>
        {
            m.Property(p => p.Amount).HasPrecision(18, 2).HasColumnName("UnitPrice");
            m.Property(p => p.Currency).HasMaxLength(3).HasColumnName("UnitPriceCurrency");
        });

        builder.HasOne(li => li.PurchaseOrder)
            .WithMany(po => po.LineItems)
            .HasForeignKey(li => li.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
