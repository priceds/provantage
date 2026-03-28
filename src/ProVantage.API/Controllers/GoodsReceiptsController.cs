using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProVantage.Application.Features.GoodsReceipts.Commands;
using ProVantage.Application.Features.GoodsReceipts.DTOs;
using ProVantage.Application.Features.GoodsReceipts.Queries;

namespace ProVantage.API.Controllers;

[Authorize]
[EnableRateLimiting("api")]
public class GoodsReceiptsController : BaseApiController
{
    /// <summary>Get goods receipts, optionally filtered by purchase order.</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetGoodsReceipts(
        [FromQuery] Guid? poId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetGoodsReceiptsQuery(poId, page, pageSize));
        return Ok(result);
    }

    /// <summary>Record a goods receipt against a purchase order line item.</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateGoodsReceipt([FromBody] CreateGoodsReceiptRequest request)
    {
        var result = await Mediator.Send(new CreateGoodsReceiptCommand(request));
        if (!result.IsSuccess) return HandleResult(result);
        return StatusCode(201, result.Value);
    }
}
