using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Application.Features.Responsaveis;
using TaskManager.CleanArch.WebAPI.Contracts.Requests;

namespace TaskManager.CleanArch.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = PapeisSistema.Gestor)]
[Route("responsaveis")]
public sealed class ResponsaveisController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ResponsavelContaDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponsavelContaDto>> Criar(
        CriarResponsavelRequest request,
        CancellationToken cancellationToken)
    {
        var responsavel = await sender.Send(
            new CriarResponsavelCommand(request.Nome, request.Email, request.Senha, request.Papel),
            cancellationToken);

        return Created($"/responsaveis/{responsavel.Id}", responsavel);
    }
}
