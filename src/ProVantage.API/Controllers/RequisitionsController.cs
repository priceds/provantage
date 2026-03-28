using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProVantage.Application.Features.Requisitions.Commands;
using ProVantage.Application.Features.Requisitions.DTOs;
using ProVantage.Application.Features.Requisitions.Queries;
using ProVantage.Domain.Enums;

namespace ProVantage.API.Controllers;

[Authorize]
[EnableRateLimiting("api")]
public class RequisitionsController : BaseApiController
{
    /// <summary>Get pending approval tasks for the current user.</summary>
    [HttpGet("pending-approvals")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetPendingApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPendingApprovalsQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>Get paginated list of purchase requisitions.</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetRequisitions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] RequisitionStatus? status = null,
        [FromQuery] string? department = null)
    {
        var result = await Mediator.Send(new GetRequisitionsQuery(page, pageSize, search, status, department));
        return Ok(result);
    }

    /// <summary>Get requisition detail by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RequisitionDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRequisition(Guid id)
    {
        var result = await Mediator.Send(new GetRequisitionByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>Create a new purchase requisition (draft).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateRequisition([FromBody] CreateRequisitionRequest request)
    {
        var result = await Mediator.Send(new CreateRequisitionCommand(request));
        if (result.IsFailure) return HandleResult(result);
        return CreatedAtAction(nameof(GetRequisition), new { id = result.Value }, result.Value);
    }

    /// <summary>Submit a draft requisition for approval.</summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitRequisition(Guid id)
    {
        var result = await Mediator.Send(new SubmitRequisitionCommand(id));
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    /// <summary>Approve a submitted requisition.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveRequisition(Guid id, [FromBody] ApproveRequest? request = null)
    {
        var result = await Mediator.Send(new ApproveRequisitionCommand(id, request?.Comments));
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    /// <summary>Reject a submitted requisition.</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RejectRequisition(Guid id, [FromBody] RejectRequest request)
    {
        var result = await Mediator.Send(new RejectRequisitionCommand(id, request.Reason));
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }
}

public record ApproveRequest(string? Comments);
public record RejectRequest(string Reason);
