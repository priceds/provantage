using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProVantage.Application.Features.Contracts.Commands;
using ProVantage.Application.Features.Contracts.Queries;

namespace ProVantage.API.Controllers;

[Authorize]
[Route("api/contracts")]
public class ContractsController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetContractsQuery query, CancellationToken ct)
        => HandleResult(await Mediator.Send(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await Mediator.Send(new GetContractByIdQuery(id), ct));

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int daysAhead = 30, CancellationToken ct = default)
        => HandleResult(await Mediator.Send(new GetExpiringContractsQuery(daysAhead), ct));
}
