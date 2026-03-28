using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProVantage.Application.Features.Invoices.Commands;
using ProVantage.Application.Features.Invoices.DTOs;
using ProVantage.Application.Features.Invoices.Queries;
using ProVantage.Domain.Enums;

namespace ProVantage.API.Controllers;

[Authorize]
[EnableRateLimiting("api")]
public class InvoicesController : BaseApiController
{
    /// <summary>Get paginated invoices with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] Guid? poId = null)
    {
        var result = await Mediator.Send(new GetInvoicesQuery(page, pageSize, search, status, poId));
        return Ok(result);
    }

    /// <summary>Get an invoice by ID with line items and match status.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetInvoice([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetInvoiceByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>Record a vendor invoice against a purchase order.</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var result = await Mediator.Send(new CreateInvoiceCommand(request));
        if (!result.IsSuccess) return HandleResult(result);
        return CreatedAtAction(nameof(GetInvoice), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Execute three-way match (Invoice vs PO vs Goods Receipt).
    /// Returns per-line match results and sets invoice status to Matched or Disputed.
    /// </summary>
    [HttpPost("{id:guid}/match")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PerformMatch([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new PerformThreeWayMatchCommand(id));
        return HandleResult(result);
    }
}
