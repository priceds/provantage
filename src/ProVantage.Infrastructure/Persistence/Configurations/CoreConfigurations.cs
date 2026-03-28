using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProVantage.Domain.Entities;

namespace ProVantage.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Subdomain).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.Subdomain).IsUnique();
        builder.Property(t => t.PrimaryCurrency).HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(t => t.AutoApproveThreshold).HasPrecision(18, 2);
        builder.Property(t => t.ManagerApprovalThreshold).HasPrecision(18, 2);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Department).HasMaxLength(100);
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Ignore(u => u.FullName);

        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.CompanyName).HasMaxLength(300).IsRequired();
        builder.Property(v => v.TaxId).HasMaxLength(50);
        builder.Property(v => v.Email).HasMaxLength(256);
        builder.Property(v => v.Phone).HasMaxLength(20);
        builder.Property(v => v.Category).HasMaxLength(100);
        builder.Property(v => v.PaymentTerms).HasMaxLength(50);
        builder.Property(v => v.Rating).HasPrecision(3, 2);

        // Owned type for Address value object
        builder.OwnsOne(v => v.Address, a =>
        {
            a.Property(p => p.Street).HasMaxLength(300).HasColumnName("Address_Street");
            a.Property(p => p.City).HasMaxLength(100).HasColumnName("Address_City");
            a.Property(p => p.State).HasMaxLength(100).HasColumnName("Address_State");
            a.Property(p => p.PostalCode).HasMaxLength(20).HasColumnName("Address_PostalCode");
            a.Property(p => p.Country).HasMaxLength(100).HasColumnName("Address_Country");
        });

        builder.HasOne(v => v.Tenant)
            .WithMany(t => t.Vendors)
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VendorContactConfiguration : IEntityTypeConfiguration<VendorContact>
{
    public void Configure(EntityTypeBuilder<VendorContact> builder)
    {
        builder.ToTable("VendorContacts");
        builder.HasKey(vc => vc.Id);
        builder.Property(vc => vc.Name).HasMaxLength(200).IsRequired();
        builder.Property(vc => vc.Email).HasMaxLength(256);
        builder.Property(vc => vc.Phone).HasMaxLength(20);
        builder.Property(vc => vc.JobTitle).HasMaxLength(100);

        builder.HasOne(vc => vc.Vendor)
            .WithMany(v => v.Contacts)
            .HasForeignKey(vc => vc.VendorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
