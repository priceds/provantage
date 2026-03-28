namespace ProVantage.Domain.Enums;

public enum InvoiceStatus
{
    Pending = 0,
    Matched = 1,
    PartiallyMatched = 2,
    Disputed = 3,
    Approved = 4,
    Paid = 5,
    Cancelled = 6
}
