using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProVantage.Application.Features.Budgets.Commands;
using ProVantage.Application.Features.Budgets.DTOs;
using ProVantage.Application.Features.Budgets.Queries;
using ProVantage.Domain.Enums;

namespace ProVantage.API.Controllers;

[Authorize]
[EnableRateLimiting("api")]
public class BudgetsController : BaseApiController
{
    /// <summary>Get budget utilization for a fiscal year, with optional period and department filters.</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetBudgets(
        [FromQuery] int fiscalYear,
        [FromQuery] BudgetPeriod? period = null,
        [FromQuery] string? department = null)
    {
        if (fiscalYear == 0) fiscalYear = DateTime.UtcNow.Year;
        var result = await Mediator.Send(new GetBudgetUtilizationQuery(fiscalYear, period, department));
        return Ok(result);
    }

    /// <summary>Create or update a budget allocation for a department/category/period combination.</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AllocateBudget([FromBody] AllocateBudgetRequest request)
    {
        var result = await Mediator.Send(new AllocateBudgetCommand(request));
        if (!result.IsSuccess) return HandleResult(result);
        return StatusCode(201, result.Value);
    }
}
