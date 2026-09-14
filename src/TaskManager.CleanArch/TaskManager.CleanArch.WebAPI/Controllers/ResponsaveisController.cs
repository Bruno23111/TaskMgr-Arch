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
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ResponsavelContaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResponsavelContaDto>>> Listar(CancellationToken cancellationToken)
    {
        var responsaveis = await sender.Send(new ListarResponsaveisQuery(), cancellationToken);
        return Ok(responsaveis);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ResponsavelContaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsavelContaDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var responsavel = await sender.Send(new ObterResponsavelPorIdQuery(id), cancellationToken);
        return responsavel is null ? NotFound() : Ok(responsavel);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ResponsavelContaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsavelContaDto>> Atualizar(
        Guid id,
        AtualizarResponsavelRequest request,
        CancellationToken cancellationToken)
    {
        var responsavel = await sender.Send(
            new AtualizarResponsavelCommand(id, request.Nome, request.Email, request.Papel),
            cancellationToken);

        return responsavel is null ? NotFound() : Ok(responsavel);
    }

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
