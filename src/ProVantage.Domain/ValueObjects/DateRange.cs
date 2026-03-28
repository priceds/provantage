namespace ProVantage.Domain.ValueObjects;

public sealed record DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public DateRange() { }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be on or after start date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;
    public bool IsExpired => DateTime.UtcNow > EndDate;
    public int DaysRemaining => Math.Max(0, (EndDate - DateTime.UtcNow).Days);
    public int TotalDays => (EndDate - StartDate).Days;
}
