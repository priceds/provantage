using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ProVantage.API.Controllers;
using ProVantage.Application.Features.Analytics.Queries;

namespace ProVantage.API.Controllers;

[Authorize]
[Route("api/analytics")]
public class AnalyticsController : BaseApiController
{
    /// <summary>Returns spend grouped by item category for a given fiscal year.</summary>
    [HttpGet("spend-by-category")]
    [OutputCache(PolicyName = "Analytics")]
    public async Task<IActionResult> GetSpendByCategory([FromQuery] int year, CancellationToken ct)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        var result = await Mediator.Send(new GetSpendByCategoryQuery(year), ct);
        return HandleResult(result);
    }

    /// <summary>Returns per-vendor performance metrics for a given fiscal year.</summary>
    [HttpGet("vendor-performance")]
    [OutputCache(PolicyName = "Analytics")]
    public async Task<IActionResult> GetVendorPerformance([FromQuery] int year, CancellationToken ct)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        var result = await Mediator.Send(new GetVendorPerformanceQuery(year), ct);
        return HandleResult(result);
    }
}
