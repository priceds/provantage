using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Infrastructure.Persistence;

public static class SeedData
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ManagerUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BuyerUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.EnsureCreatedAsync();

        if (await db.Tenants.AnyAsync(t => t.Name == "Acme Corp"))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var tenant = new Tenant
        {
            Id = TenantId,
            Name = "Acme Corp",
            Subdomain = "acme",
            PrimaryCurrency = "USD",
            AutoApproveThreshold = 5000m,
            ManagerApprovalThreshold = 25000m,
            PriceVarianceTolerancePercent = 5m,
            QuantityVarianceTolerancePercent = 2m,
            CreatedAt = now.AddDays(-180)
        };

        var admin = CreateUser(
            AdminUserId,
            tenant.Id,
            "admin@acme.com",
            "Admin",
            "Avery",
            "Stone",
            "Operations",
            passwordHasher.Hash("Admin123!"),
            now.AddDays(-180));
        var manager = CreateUser(
            ManagerUserId,
            tenant.Id,
            "manager@acme.com",
            "Manager",
            "Maya",
            "Brooks",
            "Finance",
            passwordHasher.Hash("Admin123!"),
            now.AddDays(-170));
        var buyer = CreateUser(
            BuyerUserId,
            tenant.Id,
            "buyer@acme.com",
            "Buyer",
            "Leo",
            "Martinez",
            "Procurement",
            passwordHasher.Hash("Admin123!"),
            now.AddDays(-160));

        var vendors = new[]
        {
            CreateVendor(Guid.Parse("20000000-0000-0000-0000-000000000001"), tenant.Id, "Northwind Tech", "VTX-001", "hello@northwindtech.com", "+1-415-555-0101", "IT Hardware", "Net 30", 4.7m, VendorStatus.Approved, "San Francisco", "USA", now.AddDays(-150)),
            CreateVendor(Guid.Parse("20000000-0000-0000-0000-000000000002"), tenant.Id, "BluePeak Logistics", "VTX-002", "ops@bluepeaklogistics.com", "+1-312-555-0102", "Logistics", "Net 15", 4.2m, VendorStatus.Approved, "Chicago", "USA", now.AddDays(-145)),
            CreateVendor(Guid.Parse("20000000-0000-0000-0000-000000000003"), tenant.Id, "Atlas Office Supply", "VTX-003", "sales@atlasoffice.com", "+1-646-555-0103", "Office Supplies", "Net 45", 4.0m, VendorStatus.Approved, "New York", "USA", now.AddDays(-140)),
            CreateVendor(Guid.Parse("20000000-0000-0000-0000-000000000004"), tenant.Id, "Vertex Consulting Group", "VTX-004", "engage@vertexconsulting.com", "+1-617-555-0104", "Consulting", "Net 30", 4.8m, VendorStatus.PendingApproval, "Boston", "USA", now.AddDays(-130)),
            CreateVendor(Guid.Parse("20000000-0000-0000-0000-000000000005"), tenant.Id, "Crescent Media Labs", "VTX-005", "team@crescentmedia.com", "+1-737-555-0105", "Marketing", "Net 30", 3.9m, VendorStatus.Suspended, "Austin", "USA", now.AddDays(-120))
        };
        vendors[4].StatusNotes = "Campaign performance review pending.";

        var vendorContacts = new[]
        {
            CreateVendorContact(Guid.Parse("21000000-0000-0000-0000-000000000001"), tenant.Id, vendors[0].Id, "Nina Patel", "nina@northwindtech.com", "+1-415-555-1101", "Account Director", true, now.AddDays(-150)),
            CreateVendorContact(Guid.Parse("21000000-0000-0000-0000-000000000002"), tenant.Id, vendors[1].Id, "Owen Price", "owen@bluepeaklogistics.com", "+1-312-555-1102", "Logistics Lead", true, now.AddDays(-145)),
            CreateVendorContact(Guid.Parse("21000000-0000-0000-0000-000000000003"), tenant.Id, vendors[2].Id, "Harper Jones", "harper@atlasoffice.com", "+1-646-555-1103", "Sales Manager", true, now.AddDays(-140))
        };

        var budgets = new[]
        {
            new BudgetAllocation
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                TenantId = tenant.Id,
                Department = "IT",
                Category = "Infrastructure",
                Period = BudgetPeriod.Annual,
                FiscalYear = now.Year,
                AllocatedAmount = new Money(350000m, "USD"),
                CommittedAmount = new Money(185000m, "USD"),
                SpentAmount = new Money(148000m, "USD"),
                CreatedAt = now.AddDays(-110),
                CreatedBy = admin.Email
            },
            new BudgetAllocation
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                TenantId = tenant.Id,
                Department = "Operations",
                Category = "Facilities",
                Period = BudgetPeriod.Annual,
                FiscalYear = now.Year,
                AllocatedAmount = new Money(220000m, "USD"),
                CommittedAmount = new Money(121000m, "USD"),
                SpentAmount = new Money(99000m, "USD"),
                CreatedAt = now.AddDays(-109),
                CreatedBy = admin.Email
            },
            new BudgetAllocation
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
                TenantId = tenant.Id,
                Department = "Marketing",
                Category = "Campaigns",
                Period = BudgetPeriod.Annual,
                FiscalYear = now.Year,
                AllocatedAmount = new Money(180000m, "USD"),
                CommittedAmount = new Money(84000m, "USD"),
                SpentAmount = new Money(52000m, "USD"),
                CreatedAt = now.AddDays(-108),
                CreatedBy = admin.Email
            }
        };

        var contracts = new[]
        {
            CreateContract(Guid.Parse("40000000-0000-0000-0000-000000000001"), tenant.Id, vendors[0].Id, "CTR-ACME-202601-0001", "Cloud Hardware Support", ContractStatus.Active, now.AddDays(-90), now.AddDays(120), 98000m, "USD", admin.Email),
            CreateContract(Guid.Parse("40000000-0000-0000-0000-000000000002"), tenant.Id, vendors[1].Id, "CTR-ACME-202602-0002", "Regional Freight Services", ContractStatus.Expiring, now.AddDays(-200), now.AddDays(18), 125000m, "USD", manager.Email),
            CreateContract(Guid.Parse("40000000-0000-0000-0000-000000000003"), tenant.Id, vendors[2].Id, "CTR-ACME-202603-0003", "Office Restock Master Agreement", ContractStatus.Active, now.AddDays(-45), now.AddDays(240), 67000m, "USD", buyer.Email),
            CreateContract(Guid.Parse("40000000-0000-0000-0000-000000000004"), tenant.Id, vendors[0].Id, "CTR-ACME-202510-0004", "Device Warranty Extension", ContractStatus.Expired, now.AddDays(-420), now.AddDays(-15), 42000m, "USD", admin.Email),
            CreateContract(Guid.Parse("40000000-0000-0000-0000-000000000005"), tenant.Id, vendors[1].Id, "CTR-ACME-202511-0005", "Priority Returns SLA", ContractStatus.Terminated, now.AddDays(-300), now.AddDays(60), 18000m, "USD", manager.Email)
        };

        var requisitions = BuildRequisitions(tenant.Id, buyer.Id, manager.Id, admin.Id, vendors, now);
        var purchaseOrders = BuildPurchaseOrders(tenant.Id, vendors, requisitions, now);
        var goodsReceipts = BuildGoodsReceipts(tenant.Id, purchaseOrders, admin.Id, buyer.Id, now);
        var invoices = BuildInvoices(tenant.Id, vendors, purchaseOrders, now);
        var notifications = BuildNotifications(tenant.Id, admin.Id, now);
        var auditLogs = BuildAuditLogs(tenant.Id, admin.Id, admin.Email, vendors, invoices, now);

        db.Tenants.Add(tenant);
        db.Users.AddRange(admin, manager, buyer);
        db.Vendors.AddRange(vendors);
        db.VendorContacts.AddRange(vendorContacts);
        db.BudgetAllocations.AddRange(budgets);
        db.Contracts.AddRange(contracts);
        db.PurchaseRequisitions.AddRange(requisitions);
        db.PurchaseOrders.AddRange(purchaseOrders);
        db.GoodsReceipts.AddRange(goodsReceipts);
        db.Invoices.AddRange(invoices);
        db.Notifications.AddRange(notifications);
        db.AuditLogs.AddRange(auditLogs);

        await db.SaveChangesAsync();
    }

    private static User CreateUser(
        Guid id,
        Guid tenantId,
        string email,
        string role,
        string firstName,
        string lastName,
        string department,
        string passwordHash,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            Department = department,
            IsActive = true,
            CreatedAt = createdAt,
            CreatedBy = "seed"
        };

    private static Vendor CreateVendor(
        Guid id,
        Guid tenantId,
        string companyName,
        string taxId,
        string email,
        string phone,
        string category,
        string paymentTerms,
        decimal rating,
        VendorStatus status,
        string city,
        string country,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            CompanyName = companyName,
            TaxId = taxId,
            Email = email,
            Phone = phone,
            Category = category,
            PaymentTerms = paymentTerms,
            Rating = rating,
            Status = status,
            Address = new Address
            {
                Street = "100 Market Street",
                City = city,
                State = "N/A",
                PostalCode = "00000",
                Country = country
            },
            CreatedAt = createdAt,
            CreatedBy = "seed"
        };

    private static VendorContact CreateVendorContact(
        Guid id,
        Guid tenantId,
        Guid vendorId,
        string name,
        string email,
        string phone,
        string jobTitle,
        bool isPrimary,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            VendorId = vendorId,
            Name = name,
            Email = email,
            Phone = phone,
            JobTitle = jobTitle,
            IsPrimary = isPrimary,
            CreatedAt = createdAt,
            CreatedBy = "seed"
        };

    private static Contract CreateContract(
        Guid id,
        Guid tenantId,
        Guid vendorId,
        string contractNumber,
        string title,
        ContractStatus status,
        DateTime startDate,
        DateTime endDate,
        decimal value,
        string currency,
        string createdBy) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            VendorId = vendorId,
            ContractNumber = contractNumber,
            Title = title,
            Status = status,
            Duration = new DateRange(startDate, endDate),
            TotalValue = new Money(value, currency),
            Terms = "Standard enterprise terms and renewal clause.",
            CreatedAt = startDate,
            CreatedBy = createdBy
        };

    private static List<PurchaseRequisition> BuildRequisitions(
        Guid tenantId,
        Guid buyerId,
        Guid managerId,
        Guid adminId,
        IReadOnlyList<Vendor> vendors,
        DateTime now)
    {
        var seed = new List<(string Number, string Title, string Description, string Department, RequisitionStatus Status, Guid RequesterId, Guid? ApproverId, Guid? VendorId, int CreatedDaysAgo, int? SubmittedDaysAgo, int? ApprovedDaysAgo, decimal Amount, string Category)>
        {
            ("PR-2026-0001", "Developer laptop refresh", "MacBook Pro devices for platform team.", "IT", RequisitionStatus.Approved, buyerId, managerId, vendors[0].Id, 70, 68, 66, 18500m, "Hardware"),
            ("PR-2026-0002", "Quarterly office restock", "Stationery and printer consumables.", "Operations", RequisitionStatus.Approved, buyerId, adminId, vendors[2].Id, 60, 59, 57, 6200m, "Office Supplies"),
            ("PR-2026-0003", "Warehouse network upgrade", "Switches and cabling for west warehouse.", "IT", RequisitionStatus.Approved, managerId, adminId, vendors[0].Id, 54, 53, 51, 24800m, "Networking"),
            ("PR-2026-0004", "Logistics overflow support", "Temporary carrier support during peak season.", "Operations", RequisitionStatus.Approved, buyerId, managerId, vendors[1].Id, 45, 44, 42, 14200m, "Logistics"),
            ("PR-2026-0005", "Marketing launch assets", "Creative production for spring launch.", "Marketing", RequisitionStatus.Rejected, managerId, adminId, null, 38, 36, null, 9800m, "Campaigns"),
            ("PR-2026-0006", "Facility access badges", "Replacement secure access cards.", "Operations", RequisitionStatus.Submitted, buyerId, null, vendors[2].Id, 28, 26, null, 3400m, "Security"),
            ("PR-2026-0007", "Incident response retainer", "Specialist advisory retainer.", "IT", RequisitionStatus.UnderReview, managerId, null, vendors[0].Id, 22, 21, null, 15500m, "Consulting"),
            ("PR-2026-0008", "Customer gifting kits", "Branded kits for enterprise outreach.", "Marketing", RequisitionStatus.Draft, buyerId, null, null, 15, null, null, 4300m, "Brand"),
            ("PR-2026-0009", "Regional distribution pilot", "Pilot routes for southeast shipping.", "Operations", RequisitionStatus.Approved, buyerId, adminId, vendors[1].Id, 12, 11, 10, 27200m, "Logistics"),
            ("PR-2026-0010", "Meeting room peripherals", "Displays and audio kits for conference rooms.", "IT", RequisitionStatus.Submitted, managerId, null, vendors[0].Id, 9, 8, null, 7100m, "AV")
        };

        var requisitions = new List<PurchaseRequisition>();

        foreach (var item in seed)
        {
            var requisition = new PurchaseRequisition
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RequisitionNumber = item.Number,
                Title = item.Title,
                Description = item.Description,
                Department = item.Department,
                Status = item.Status,
                RequestedById = item.RequesterId,
                ApprovedById = item.ApproverId,
                PreferredVendorId = item.VendorId,
                CreatedAt = now.AddDays(-item.CreatedDaysAgo),
                RequiredByDate = now.AddDays(Math.Max(7, 30 - item.CreatedDaysAgo)),
                SubmittedAt = item.SubmittedDaysAgo.HasValue ? now.AddDays(-item.SubmittedDaysAgo.Value) : null,
                ApprovedAt = item.ApprovedDaysAgo.HasValue ? now.AddDays(-item.ApprovedDaysAgo.Value) : null,
                RejectionReason = item.Status == RequisitionStatus.Rejected ? "Budget shifted to higher-priority campaign work." : null,
                CreatedBy = "seed"
            };

            requisition.LineItems.Add(new RequisitionLineItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ItemDescription = item.Title,
                ItemCode = item.Number.Replace("PR", "LI"),
                Category = item.Category,
                Quantity = 1,
                UnitOfMeasure = "lot",
                UnitPrice = new Money(item.Amount, "USD"),
                CreatedAt = requisition.CreatedAt,
                CreatedBy = "seed"
            });

            requisitions.Add(requisition);
        }

        return requisitions;
    }

    private static List<PurchaseOrder> BuildPurchaseOrders(
        Guid tenantId,
        IReadOnlyList<Vendor> vendors,
        IReadOnlyList<PurchaseRequisition> requisitions,
        DateTime now)
    {
        var approved = requisitions.Where(r => r.Status == RequisitionStatus.Approved).ToList();
        var configs = new[]
        {
            (approved[0], "PO-2026-0001", OrderStatus.Closed, -64, -45, vendors[0].Id, 18500m, "MacBook Pro 16\""),
            (approved[1], "PO-2026-0002", OrderStatus.Received, -56, -32, vendors[2].Id, 6200m, "Office supply bundle"),
            (approved[2], "PO-2026-0003", OrderStatus.Acknowledged, -50, 6, vendors[0].Id, 24800m, "Warehouse switch stack"),
            (approved[3], "PO-2026-0004", OrderStatus.Sent, -40, -2, vendors[1].Id, 14200m, "Peak season freight blocks"),
            (approved[4], "PO-2026-0005", OrderStatus.Created, -9, 21, vendors[1].Id, 27200m, "Distribution pilot routes"),
            (approved[0], "PO-2026-0006", OrderStatus.Received, -18, -5, vendors[0].Id, 9800m, "Docking stations"),
            (approved[1], "PO-2026-0007", OrderStatus.Closed, -15, -1, vendors[2].Id, 4100m, "Printer cartridges"),
            (approved[2], "PO-2026-0008", OrderStatus.Sent, -6, 14, vendors[0].Id, 11600m, "Structured cabling")
        };

        var orders = new List<PurchaseOrder>();

        foreach (var (requisition, orderNumber, status, orderDaysAgo, expectedOffset, vendorId, amount, description) in configs)
        {
            var order = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RequisitionId = requisition.Id,
                VendorId = vendorId,
                OrderNumber = orderNumber,
                Status = status,
                OrderDate = now.AddDays(orderDaysAgo),
                ExpectedDeliveryDate = now.AddDays(expectedOffset),
                PaymentTerms = "Net 30",
                ShippingAddress = "100 Acme Plaza, Seattle, WA 98101",
                Notes = "Seeded procurement order for demo walkthroughs.",
                CreatedAt = now.AddDays(orderDaysAgo),
                CreatedBy = "seed"
            };

            order.LineItems.Add(new OrderLineItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ItemDescription = description,
                ItemCode = orderNumber.Replace("PO", "SKU"),
                UnitOfMeasure = "lot",
                QuantityOrdered = 1,
                QuantityReceived = status is OrderStatus.Received or OrderStatus.Closed ? 1 : 0,
                UnitPrice = new Money(amount, "USD"),
                CreatedAt = order.CreatedAt,
                CreatedBy = "seed"
            });

            orders.Add(order);
        }

        return orders;
    }

    private static List<GoodsReceipt> BuildGoodsReceipts(
        Guid tenantId,
        IReadOnlyList<PurchaseOrder> purchaseOrders,
        Guid adminId,
        Guid buyerId,
        DateTime now)
    {
        var receivedOrders = purchaseOrders
            .Where(po => po.Status is OrderStatus.Received or OrderStatus.Closed or OrderStatus.Acknowledged)
            .Take(5)
            .ToList();

        return receivedOrders
            .Select((po, index) => new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PurchaseOrderId = po.Id,
                ReceivedById = index % 2 == 0 ? adminId : buyerId,
                ReceiptNumber = $"GR-2026-000{index + 1}",
                ReceivedDate = now.AddDays(-(12 - index * 2)),
                Notes = po.Status == OrderStatus.Acknowledged
                    ? "Partial receipt logged while order remains acknowledged."
                    : "Received in good condition.",
                DeliveryNote = $"DLV-{index + 101}",
                ItemCode = po.LineItems.First().ItemCode,
                QuantityReceived = 1,
                QuantityRejected = 0,
                CreatedAt = now.AddDays(-(12 - index * 2)),
                CreatedBy = "seed"
            })
            .ToList();
    }

    private static List<Invoice> BuildInvoices(
        Guid tenantId,
        IReadOnlyList<Vendor> vendors,
        IReadOnlyList<PurchaseOrder> purchaseOrders,
        DateTime now)
    {
        var selectedOrders = purchaseOrders.Take(5).ToList();
        var invoiceConfigs = new[]
        {
            ("INV-VT-1001", "INV-2026-0001", selectedOrders[0], vendors.First(v => v.Id == selectedOrders[0].VendorId).Id, InvoiceStatus.Matched, 32, 5),
            ("INV-VT-1002", "INV-2026-0002", selectedOrders[1], vendors.First(v => v.Id == selectedOrders[1].VendorId).Id, InvoiceStatus.Matched, 27, 3),
            ("INV-VT-1003", "INV-2026-0003", selectedOrders[2], vendors.First(v => v.Id == selectedOrders[2].VendorId).Id, InvoiceStatus.Pending, 18, 7),
            ("INV-VT-1004", "INV-2026-0004", selectedOrders[3], vendors.First(v => v.Id == selectedOrders[3].VendorId).Id, InvoiceStatus.Disputed, 10, 10),
            ("INV-VT-1005", "INV-2026-0005", selectedOrders[4], vendors.First(v => v.Id == selectedOrders[4].VendorId).Id, InvoiceStatus.Matched, 6, 12)
        };

        var invoices = new List<Invoice>();

        foreach (var (vendorInvoiceNo, internalRef, order, vendorId, status, invoiceDaysAgo, dueOffset) in invoiceConfigs)
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PurchaseOrderId = order.Id,
                VendorId = vendorId,
                InvoiceNumber = vendorInvoiceNo,
                InternalReference = internalRef,
                Status = status,
                InvoiceDate = now.AddDays(-invoiceDaysAgo),
                DueDate = now.AddDays(dueOffset),
                MatchedAt = status == InvoiceStatus.Matched ? now.AddDays(-(invoiceDaysAgo - 1)) : null,
                DisputeNotes = status == InvoiceStatus.Disputed ? "Quantity variance exceeded tolerance." : null,
                CreatedAt = now.AddDays(-invoiceDaysAgo),
                CreatedBy = "seed"
            };

            for (var i = 0; i < 4; i++)
            {
                invoice.LineItems.Add(new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    InvoiceId = invoice.Id,
                    ItemDescription = i switch
                    {
                        0 => "Hardware",
                        1 => "Freight",
                        2 => "Services",
                        _ => "Support"
                    },
                    ItemCode = $"{internalRef}-LI-{i + 1}",
                    Quantity = 1,
                    UnitPrice = new Money(Math.Round(order.LineItems.First().UnitPrice.Amount / 4m, 2), "USD"),
                    CreatedAt = invoice.CreatedAt,
                    CreatedBy = "seed"
                });
            }

            invoices.Add(invoice);
        }

        return invoices;
    }

    private static List<Notification> BuildNotifications(Guid tenantId, Guid adminId, DateTime now)
    {
        return Enumerable.Range(1, 15)
            .Select(i => new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = adminId,
                Title = i % 3 == 0 ? "Invoice attention required" : $"Workflow update #{i}",
                Message = i % 3 == 0
                    ? "An invoice variance needs manual review."
                    : "A procurement workflow step changed status.",
                Type = i % 3 == 0 ? NotificationType.Warning : NotificationType.Info,
                IsRead = i % 4 == 0,
                ActionUrl = i % 3 == 0 ? "/invoices" : "/dashboard",
                EntityType = i % 3 == 0 ? "Invoice" : "Requisition",
                EntityId = Guid.NewGuid(),
                CreatedAt = now.AddHours(-i * 6)
            })
            .ToList();
    }

    private static List<AuditLog> BuildAuditLogs(
        Guid tenantId,
        Guid adminId,
        string userName,
        IReadOnlyList<Vendor> vendors,
        IReadOnlyList<Invoice> invoices,
        DateTime now)
    {
        var logs = new List<AuditLog>();

        logs.AddRange(vendors.Take(5).Select((vendor, index) => new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = adminId,
            UserName = userName,
            Action = index % 2 == 0 ? AuditAction.Created : AuditAction.Updated,
            EntityType = nameof(Vendor),
            EntityId = vendor.Id,
            OldValues = index % 2 == 0 ? null : "{\"Status\":\"PendingApproval\"}",
            NewValues = index % 2 == 0
                ? $"{{\"CompanyName\":\"{vendor.CompanyName}\",\"Status\":\"{vendor.Status}\"}}"
                : $"{{\"Status\":\"{vendor.Status}\",\"PaymentTerms\":\"{vendor.PaymentTerms}\"}}",
            CreatedAt = now.AddDays(-(20 - index)),
            IpAddress = "127.0.0.1",
            UserAgent = "SeedData"
        }));

        logs.AddRange(invoices.Take(5).Select((invoice, index) => new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = adminId,
            UserName = userName,
            Action = index % 2 == 0 ? AuditAction.Matched : AuditAction.Updated,
            EntityType = nameof(Invoice),
            EntityId = invoice.Id,
            OldValues = index % 2 == 0 ? "{\"Status\":\"Pending\"}" : "{\"Status\":\"Pending\"}",
            NewValues = $"{{\"Status\":\"{invoice.Status}\",\"InternalReference\":\"{invoice.InternalReference}\"}}",
            CreatedAt = now.AddDays(-(10 - index)),
            IpAddress = "127.0.0.1",
            UserAgent = "SeedData"
        }));

        return logs;
    }
}
