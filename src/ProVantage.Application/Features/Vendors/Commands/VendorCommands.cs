using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProVantage.Application.Common.Interfaces;
using ProVantage.Application.Common.Models;
using ProVantage.Application.Features.Vendors.DTOs;
using ProVantage.Domain.Entities;
using ProVantage.Domain.Enums;
using ProVantage.Domain.Interfaces;
using ProVantage.Domain.ValueObjects;

namespace ProVantage.Application.Features.Vendors.Commands;

// ═══════════════════════════════════════════
// CREATE VENDOR
// ═══════════════════════════════════════════
public record CreateVendorCommand(CreateVendorRequest Data) : ICommand<Result<Guid>>;

public class CreateVendorValidator : AbstractValidator<CreateVendorCommand>
{
    public CreateVendorValidator()
    {
        RuleFor(x => x.Data.CompanyName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Data.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Data.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Data.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Data.Address).NotNull();
        RuleFor(x => x.Data.Address.City).NotEmpty().When(x => x.Data.Address != null);
        RuleFor(x => x.Data.Address.Country).NotEmpty().When(x => x.Data.Address != null);
    }
}

public class CreateVendorHandler : IRequestHandler<CreateVendorCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public CreateVendorHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<Guid>> Handle(CreateVendorCommand request, CancellationToken ct)
    {
        var data = request.Data;
        var vendor = new Vendor
        {
            CompanyName = data.CompanyName,
            TaxId = data.TaxId,
            Email = data.Email,
            Phone = data.Phone,
            Website = data.Website ?? string.Empty,
            Category = data.Category,
            PaymentTerms = data.PaymentTerms,
            Status = VendorStatus.PendingApproval,
            TenantId = _tenant.TenantId,
            Address = new Address
            {
                Street = data.Address.Street,
                City = data.Address.City,
                State = data.Address.State,
                PostalCode = data.Address.PostalCode,
                Country = data.Address.Country
            }
        };

        if (data.Contacts?.Any() == true)
        {
            foreach (var c in data.Contacts)
            {
                vendor.Contacts.Add(new VendorContact
                {
                    Name = c.Name, Email = c.Email, Phone = c.Phone,
                    JobTitle = c.JobTitle, IsPrimary = c.IsPrimary,
                    TenantId = _tenant.TenantId
                });
            }
        }

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);

        return Result<Guid>.Success(vendor.Id);
    }
}

// ═══════════════════════════════════════════
// UPDATE VENDOR
// ═══════════════════════════════════════════
public record UpdateVendorCommand(Guid Id, UpdateVendorRequest Data) : ICommand<Result>;

public class UpdateVendorHandler : IRequestHandler<UpdateVendorCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public UpdateVendorHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result> Handle(UpdateVendorCommand request, CancellationToken ct)
    {
        var vendor = await _db.Vendors
            .FirstOrDefaultAsync(v => v.Id == request.Id && v.TenantId == _tenant.TenantId, ct);

        if (vendor is null) return Result.NotFound("Vendor not found.");

        var d = request.Data;
        vendor.CompanyName = d.CompanyName;
        vendor.Email = d.Email;
        vendor.Phone = d.Phone;
        vendor.Website = d.Website ?? string.Empty;
        vendor.Category = d.Category;
        vendor.PaymentTerms = d.PaymentTerms;
        vendor.Address = new Address
        {
            Street = d.Address.Street, City = d.Address.City,
            State = d.Address.State, PostalCode = d.Address.PostalCode,
            Country = d.Address.Country
        };

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════════════════════
// CHANGE VENDOR STATUS
// ═══════════════════════════════════════════
public record ChangeVendorStatusCommand(Guid Id, VendorStatus Status, string? Notes)
    : ICommand<Result>;

public class ChangeVendorStatusHandler : IRequestHandler<ChangeVendorStatusCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public ChangeVendorStatusHandler(IApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result> Handle(ChangeVendorStatusCommand request, CancellationToken ct)
    {
        var vendor = await _db.Vendors
            .FirstOrDefaultAsync(v => v.Id == request.Id && v.TenantId == _tenant.TenantId, ct);

        if (vendor is null) return Result.NotFound("Vendor not found.");

        vendor.Status = request.Status;
        vendor.StatusNotes = request.Notes;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
