using ProVantage.Domain.ValueObjects;

namespace ProVantage.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_rejects_end_date_before_start_date()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(-1);

        var exception = Assert.Throws<ArgumentException>(() => new DateRange(start, end));

        Assert.Contains("End date must be on or after start date", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Contains_is_inclusive_for_start_and_end_dates()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(5);
        var range = new DateRange(start, end);

        Assert.True(range.Contains(start));
        Assert.True(range.Contains(end));
        Assert.False(range.Contains(end.AddDays(1)));
    }

    [Fact]
    public void Expired_ranges_report_zero_days_remaining()
    {
        var range = new DateRange(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1));

        Assert.True(range.IsExpired);
        Assert.Equal(0, range.DaysRemaining);
    }
}
