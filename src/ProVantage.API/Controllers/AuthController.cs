using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProVantage.Application.Features.Auth.Commands;
using ProVantage.Application.Features.Auth.DTOs;

namespace ProVantage.API.Controllers;

[EnableRateLimiting("auth")]
public class AuthController : BaseApiController
{
    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await Mediator.Send(new LoginCommand(request.Email, request.Password));
        return HandleResult(result);
    }

    /// <summary>Register a new tenant with admin user.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await Mediator.Send(new RegisterCommand(
            request.Email, request.Password, request.FirstName, request.LastName,
            request.TenantName, request.TenantSubdomain));
        return HandleResult(result);
    }

    /// <summary>Refresh an expired access token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await Mediator.Send(new RefreshTokenCommand(request.AccessToken, request.RefreshToken));
        return HandleResult(result);
    }

    /// <summary>Revoke a refresh token (server-side logout).</summary>
    [HttpPost("revoke")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
    {
        var result = await Mediator.Send(new RevokeTokenCommand(request.RefreshToken));
        if (result.IsSuccess) return NoContent();
        return HandleResult(result);
    }

    /// <summary>Get current user info from token.</summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(200)]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }
}
