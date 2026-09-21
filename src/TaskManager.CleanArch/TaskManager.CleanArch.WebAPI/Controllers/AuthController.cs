using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.Features.Autenticacao;
using TaskManager.CleanArch.WebAPI.Contracts.Requests;
using TaskManager.CleanArch.WebAPI.Contracts.Responses;

namespace TaskManager.CleanArch.WebAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await sender.Send(new LoginCommand(request.Email, request.Senha), cancellationToken);
        return Ok(new LoginResponse(token.Valor, token.ExpiraEm));
    }
}
