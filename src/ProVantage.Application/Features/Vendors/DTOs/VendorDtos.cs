using ProVantage.Domain.Enums;

namespace ProVantage.Application.Features.Vendors.DTOs;

public record VendorDto(
    Guid Id,
    string CompanyName,
    string Email,
    string Phone,
    string Category,
    VendorStatus Status,
    string PaymentTerms,
    decimal Rating,
    string City,
    string Country,
    DateTime CreatedAt);

public record VendorDetailDto(
    Guid Id,
    string CompanyName,
    string TaxId,
    string Email,
    string Phone,
    string Website,
    string Category,
    VendorStatus Status,
    string? StatusNotes,
    string PaymentTerms,
    decimal Rating,
    AddressDto Address,
    List<VendorContactDto> Contacts,
    DateTime CreatedAt,
    string CreatedBy);

public record AddressDto(string Street, string City, string State, string PostalCode, string Country);

public record VendorContactDto(
    Guid Id, string Name, string Email, string Phone, string JobTitle, bool IsPrimary);

public record CreateVendorRequest(
    string CompanyName,
    string TaxId,
    string Email,
    string Phone,
    string? Website,
    string Category,
    string PaymentTerms,
    AddressDto Address,
    List<CreateContactRequest>? Contacts);

public record CreateContactRequest(string Name, string Email, string Phone, string JobTitle, bool IsPrimary);

public record UpdateVendorRequest(
    string CompanyName,
    string Email,
    string Phone,
    string? Website,
    string Category,
    string PaymentTerms,
    AddressDto Address);

public record ChangeVendorStatusRequest(VendorStatus Status, string? Notes);
