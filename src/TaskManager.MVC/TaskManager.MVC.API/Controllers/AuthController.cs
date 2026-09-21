using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.MVC.API.Contracts.Auth;
using TaskManager.MVC.API.Identity;

namespace TaskManager.MVC.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("auth")]
public sealed class AuthController(AuthenticationService authenticationService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        Ok(await authenticationService.LoginAsync(request, cancellationToken));
}
