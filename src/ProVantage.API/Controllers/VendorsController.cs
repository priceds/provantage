using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using ProVantage.Application.Features.Vendors.Commands;
using ProVantage.Application.Features.Vendors.DTOs;
using ProVantage.Application.Features.Vendors.Queries;
using ProVantage.Domain.Enums;

namespace ProVantage.API.Controllers;

[Authorize]
[EnableRateLimiting("api")]
public class VendorsController : BaseApiController
{
    /// <summary>Get paginated list of vendors with optional filters.</summary>
    [HttpGet]
    [OutputCache(PolicyName = "VendorList")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetVendors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] VendorStatus? status = null,
        [FromQuery] string? category = null)
    {
        var result = await Mediator.Send(new GetVendorsQuery(page, pageSize, search, status, category));
        return Ok(result);
    }

    /// <summary>Get vendor detail by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VendorDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVendor(Guid id)
    {
        var result = await Mediator.Send(new GetVendorByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>Create a new vendor.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
    {
        var result = await Mediator.Send(new CreateVendorCommand(request));
        if (result.IsFailure) return HandleResult(result);
        return CreatedAtAction(nameof(GetVendor), new { id = result.Value }, result.Value);
    }

    /// <summary>Update vendor details.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateVendor(Guid id, [FromBody] UpdateVendorRequest request)
    {
        var result = await Mediator.Send(new UpdateVendorCommand(id, request));
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    /// <summary>Change vendor status (approve, suspend, blacklist).</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeVendorStatusRequest request)
    {
        var result = await Mediator.Send(new ChangeVendorStatusCommand(id, request.Status, request.Notes));
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    /// <summary>Get distinct vendor categories for filters.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> GetCategories()
    {
        // Inline query — not worth a MediatR command
        return Ok(new List<string>
        {
            "IT Services", "Office Supplies", "Manufacturing", "Logistics",
            "Consulting", "Marketing", "Facilities", "Raw Materials"
        });
    }
}
