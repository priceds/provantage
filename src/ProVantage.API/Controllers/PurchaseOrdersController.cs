using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProVantage.Application.Features.PurchaseOrders.Commands;
using ProVantage.Application.Features.PurchaseOrders.DTOs;
using ProVantage.Application.Features.PurchaseOrders.Queries;
using ProVantage.Domain.Enums;

namespace ProVantage.API.Controllers;

[Authorize]
[EnableRateLimiting("api")]
public class PurchaseOrdersController : BaseApiController
{
    /// <summary>Get paginated purchase orders with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? vendorId = null)
    {
        var result = await Mediator.Send(new GetPurchaseOrdersQuery(page, pageSize, search, status, vendorId));
        return Ok(result);
    }

    /// <summary>Get a purchase order by ID with full line item detail.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPurchaseOrder([FromRoute] Guid id)
    {
        var result = await Mediator.Send(new GetPurchaseOrderByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>Generate a new purchase order (optionally from an approved requisition).</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        var result = await Mediator.Send(new CreatePurchaseOrderCommand(request));
        if (!result.IsSuccess) return HandleResult(result);
        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = result.Value }, result.Value);
    }

    /// <summary>Update the status of a purchase order (e.g., Sent, Acknowledged, Received).</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await Mediator.Send(new UpdateOrderStatusCommand(id, request.Status));
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }
}
