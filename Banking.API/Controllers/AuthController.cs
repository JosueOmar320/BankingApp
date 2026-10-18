using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
        => Ok(await _authService.LoginAsync(request, cancellationToken));
}
