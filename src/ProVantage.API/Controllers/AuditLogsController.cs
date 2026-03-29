using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProVantage.Application.Features.AuditLogs.Queries;

namespace ProVantage.API.Controllers;

[Authorize(Roles = "Admin,TenantAdmin")]
[Route("api/audit-logs")]
public class AuditLogsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAuditLogsQuery query, CancellationToken ct)
        => HandleResult(await Mediator.Send(query, ct));
}
