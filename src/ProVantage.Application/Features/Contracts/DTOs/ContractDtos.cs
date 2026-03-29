using ProVantage.Domain.Enums;

namespace ProVantage.Application.Features.Contracts.DTOs;

public record ContractDto(
    Guid Id,
    string ContractNumber,
    Guid VendorId,
    string VendorName,
    string Title,
    ContractStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    decimal Value,
    string Currency,
    int DaysRemaining,
    DateTime CreatedAt);
