using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ProVantage.API.Controllers;
using ProVantage.Application.Features.Dashboard.Queries;

namespace ProVantage.API.Controllers;

[Authorize]
[Route("api/dashboard")]
public class DashboardController : BaseApiController
{
    /// <summary>Returns aggregated KPI data for the dashboard.</summary>
    [HttpGet("kpis")]
    [OutputCache(PolicyName = "Dashboard")]
    public async Task<IActionResult> GetKpis(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDashboardKpisQuery(), ct);
        return HandleResult(result);
    }
}
