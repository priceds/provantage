using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProVantage.Application.Common.Models;

namespace ProVantage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess) return Ok();
        return StatusCode(result.StatusCode ?? 400, new { error = result.Error });
    }

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        return StatusCode(result.StatusCode ?? 400, new { error = result.Error });
    }
}
