using Banking.API.RateLimiting;
using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Banking.API.Controllers;

/// <summary>Login: exchanges a customer's credentials for a bearer token.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>Creates the controller.</summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Authenticates a customer and returns a JWT.</summary>
    /// <remarks>
    /// Send the token on every other request as <c>Authorization: Bearer &lt;accessToken&gt;</c>.
    /// In Swagger UI, paste it in the <b>Authorize</b> dialog.
    /// </remarks>
    /// <param name="request">Document number and password.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The access token and its expiry.</response>
    /// <response code="400">The payload is invalid.</response>
    /// <response code="401">The document number or the password is wrong.</response>
    /// <response code="429">Too many login attempts from this address; wait for the <c>Retry-After</c> seconds.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
        => Ok(await _authService.LoginAsync(request, cancellationToken));
}
