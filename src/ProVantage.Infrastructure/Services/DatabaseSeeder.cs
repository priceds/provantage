using Microsoft.EntityFrameworkCore;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;
using ProVantage.Infrastructure.Persistence;

namespace ProVantage.Infrastructure.Services;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(ApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        await _db.Database.MigrateAsync().ConfigureAwait(false);

        if (await _db.Tenants.AnyAsync()) return; // Already seeded

        // ── Tenant ──
        var tenant = new Tenant
        {
            Name = "Acme Corporation",
            Subdomain = "acme",
            PrimaryCurrency = "USD",
            AutoApproveThreshold = 1000m,
            ManagerApprovalThreshold = 10000m,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);

        // ── Users ──
        var admin = CreateUser(tenant, "admin@acme.com", "Admin", "John", "Mitchell", "Administration");
        var manager = CreateUser(tenant, "jane.smith@acme.com", "Manager", "Jane", "Smith", "Engineering");
        var buyer = CreateUser(tenant, "mike.johnson@acme.com", "Buyer", "Mike", "Johnson", "Procurement");
        var viewer = CreateUser(tenant, "sarah.davis@acme.com", "Viewer", "Sarah", "Davis", "Finance");

        _db.Users.AddRange(admin, manager, buyer, viewer);

        // ── Vendors ──
        var vendors = new[]
        {
            CreateVendor(tenant, "TechServe Solutions", "TS-2026-001", "contact@techserve.io",
                "+1-555-0101", "IT Services", "Net 30", 4.5m,
                new Address { Street = "100 Tech Park Dr", City = "San Francisco", State = "CA", PostalCode = "94105", Country = "USA" }),
            CreateVendor(tenant, "GlobalSupply Corp", "GS-2026-002", "sales@globalsupply.com",
                "+1-555-0102", "Office Supplies", "Net 45", 3.8m,
                new Address { Street = "250 Commerce Blvd", City = "Chicago", State = "IL", PostalCode = "60601", Country = "USA" }),
            CreateVendor(tenant, "BuildRight Materials", "BR-2026-003", "orders@buildright.co",
                "+1-555-0103", "Manufacturing", "Net 30", 4.2m,
                new Address { Street = "800 Industrial Ave", City = "Detroit", State = "MI", PostalCode = "48201", Country = "USA" }),
            CreateVendor(tenant, "SwiftLogistics Inc", "SL-2026-004", "dispatch@swiftlog.com",
                "+1-555-0104", "Logistics", "Net 15", 4.0m,
                new Address { Street = "45 Port Authority", City = "Newark", State = "NJ", PostalCode = "07102", Country = "USA" }),
            CreateVendor(tenant, "Pinnacle Consulting", "PC-2026-005", "engage@pinnacle.com",
                "+1-555-0105", "Consulting", "Net 60", 4.7m,
                new Address { Street = "1 Financial Center", City = "Boston", State = "MA", PostalCode = "02110", Country = "USA" }),
            CreateVendor(tenant, "MediaMax Agency", "MM-2026-006", "hello@mediamax.io",
                "+1-555-0106", "Marketing", "Net 30", 3.5m,
                new Address { Street = "500 Creative Dr", City = "Austin", State = "TX", PostalCode = "73301", Country = "USA" }),
        };
        vendors[0].Status = VendorStatus.Approved;
        vendors[1].Status = VendorStatus.Approved;
        vendors[2].Status = VendorStatus.Approved;
        vendors[3].Status = VendorStatus.Approved;
        vendors[4].Status = VendorStatus.PendingApproval;
        vendors[5].Status = VendorStatus.Suspended;
        vendors[5].StatusNotes = "Late deliveries in Q1 2026";

        _db.Vendors.AddRange(vendors);

        // ── Vendor Contacts ──
        _db.VendorContacts.Add(new VendorContact
        {
            VendorId = vendors[0].Id, Name = "Alex Turner", Email = "alex@techserve.io",
            Phone = "+1-555-1001", JobTitle = "Account Manager", IsPrimary = true, TenantId = tenant.Id
        });
        _db.VendorContacts.Add(new VendorContact
        {
            VendorId = vendors[1].Id, Name = "Lisa Chen", Email = "lisa@globalsupply.com",
            Phone = "+1-555-1002", JobTitle = "Sales Director", IsPrimary = true, TenantId = tenant.Id
        });

        // ── Purchase Requisitions ──
        var reqApproved = new PurchaseRequisition
        {
            RequisitionNumber = "PR-2026-00001", Title = "Cloud Infrastructure — AWS Q2",
            Description = "AWS reserved instances and infrastructure for Q2 scaling",
            Department = "Engineering", Status = RequisitionStatus.Approved,
            RequestedById = buyer.Id, ApprovedById = admin.Id,
            PreferredVendorId = vendors[0].Id, TenantId = tenant.Id,
            SubmittedAt = DateTime.UtcNow.AddDays(-5), ApprovedAt = DateTime.UtcNow.AddDays(-3)
        };
        reqApproved.LineItems.Add(new RequisitionLineItem
        {
            ItemDescription = "EC2 Reserved Instances (1yr)", ItemCode = "AWS-EC2-RI",
            Category = "Cloud", Quantity = 10, UnitOfMeasure = "ea",
            UnitPrice = new Money(3200m, "USD"), TenantId = tenant.Id
        });
        reqApproved.LineItems.Add(new RequisitionLineItem
        {
            ItemDescription = "S3 Storage (10TB)", ItemCode = "AWS-S3-10T",
            Category = "Cloud", Quantity = 1, UnitOfMeasure = "ea",
            UnitPrice = new Money(2400m, "USD"), TenantId = tenant.Id
        });

        var reqPending = new PurchaseRequisition
        {
            RequisitionNumber = "PR-2026-00002", Title = "Office Supplies — Q2 Restock",
            Description = "Standard office supplies restock for Q2",
            Department = "Administration", Status = RequisitionStatus.Submitted,
            RequestedById = buyer.Id, TenantId = tenant.Id,
            SubmittedAt = DateTime.UtcNow.AddDays(-1)
        };
        reqPending.LineItems.Add(new RequisitionLineItem
        {
            ItemDescription = "Copy Paper (A4, 5000 sheets)", ItemCode = "OFF-PAP-A4",
            Category = "Office Supplies", Quantity = 50, UnitOfMeasure = "box",
            UnitPrice = new Money(42m, "USD"), TenantId = tenant.Id
        });

        var reqDraft = new PurchaseRequisition
        {
            RequisitionNumber = "PR-2026-00003", Title = "New Laptops — Engineering Team",
            Description = "MacBook Pro 16\" M3 Pro for 5 new hires",
            Department = "Engineering", Status = RequisitionStatus.Draft,
            RequestedById = manager.Id, TenantId = tenant.Id
        };
        reqDraft.LineItems.Add(new RequisitionLineItem
        {
            ItemDescription = "MacBook Pro 16\" M3 Pro 36GB", ItemCode = "HW-MBP-16",
            Category = "IT Equipment", Quantity = 5, UnitOfMeasure = "ea",
            UnitPrice = new Money(3499m, "USD"), TenantId = tenant.Id
        });

        _db.PurchaseRequisitions.AddRange(reqApproved, reqPending, reqDraft);

        // ── Budgets ──
        _db.BudgetAllocations.AddRange(
            new BudgetAllocation
            {
                Department = "Engineering", Category = "Cloud Infrastructure",
                Period = BudgetPeriod.Quarterly, FiscalYear = 2026, FiscalQuarter = 2,
                AllocatedAmount = new Money(150000m, "USD"),
                CommittedAmount = new Money(34400m, "USD"),
                SpentAmount = new Money(12000m, "USD"), TenantId = tenant.Id
            },
            new BudgetAllocation
            {
                Department = "Administration", Category = "Office Supplies",
                Period = BudgetPeriod.Quarterly, FiscalYear = 2026, FiscalQuarter = 2,
                AllocatedAmount = new Money(25000m, "USD"),
                CommittedAmount = new Money(2100m, "USD"),
                SpentAmount = new Money(8500m, "USD"), TenantId = tenant.Id
            }
        );

        await _db.SaveChangesAsync();
    }

    private User CreateUser(Tenant tenant, string email, string role, string first, string last, string dept)
    {
        return new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash("Password1!"),
            FirstName = first, LastName = last,
            Role = role, Department = dept,
            TenantId = tenant.Id, IsActive = true,
            CreatedBy = "seed"
        };
    }

    private static Vendor CreateVendor(Tenant tenant, string name, string taxId, string email,
        string phone, string category, string terms, decimal rating, Address address)
    {
        return new Vendor
        {
            CompanyName = name, TaxId = taxId, Email = email, Phone = phone,
            Category = category, PaymentTerms = terms, Rating = rating,
            Address = address, TenantId = tenant.Id
        };
    }
}
