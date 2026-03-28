namespace ProVantage.Domain.Enums;

public enum OrderStatus
{
    Created = 0,
    Sent = 1,
    Acknowledged = 2,
    PartiallyReceived = 3,
    Received = 4,
    Closed = 5,
    Cancelled = 6
}
