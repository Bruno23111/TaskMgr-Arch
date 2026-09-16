using MediatR;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Application.Features.Responsaveis;
using TaskManager.CleanArch.WebAPI.Contracts.Requests;

namespace TaskManager.CleanArch.WebAPI.Controllers;

[ApiController]
[Authorize]
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

    [HttpGet("{responsavelId:guid}")]
    [ProducesResponseType<ResponsavelContaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsavelContaDto>> ObterPorId(Guid responsavelId, CancellationToken cancellationToken)
    {
        var responsavel = await sender.Send(new ObterResponsavelPorIdQuery(responsavelId), cancellationToken);
        return responsavel is null ? NotFound() : Ok(responsavel);
    }

    [HttpPut("{responsavelId:guid}")]
    [Authorize(Roles = PapeisSistema.Gestor)]
    [ProducesResponseType<ResponsavelContaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponsavelContaDto>> Atualizar(
        Guid responsavelId,
        AtualizarResponsavelRequest request,
        CancellationToken cancellationToken)
    {
        var responsavel = await sender.Send(
            new AtualizarResponsavelCommand(responsavelId, request.Nome, request.Email, request.Papel, request.Versao),
            cancellationToken);

        return responsavel is null ? NotFound() : Ok(responsavel);
    }

    [HttpPatch("{responsavelId:guid}/desativacao")]
    [Authorize(Roles = PapeisSistema.Gestor)]
    [ProducesResponseType<ResponsavelContaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponsavelContaDto>> Desativar(
        Guid responsavelId,
        DesativarResponsavelRequest request,
        CancellationToken cancellationToken)
    {
        var responsavel = await sender.Send(new DesativarResponsavelCommand(responsavelId, request.Versao), cancellationToken);
        return responsavel is null ? NotFound() : Ok(responsavel);
    }

    [HttpDelete("{responsavelId:guid}")]
    [Authorize(Roles = PapeisSistema.Gestor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(
        Guid responsavelId,
        [FromQuery, Range(1, int.MaxValue)] int versao,
        CancellationToken cancellationToken)
    {
        var excluido = await sender.Send(new ExcluirResponsavelCommand(responsavelId, versao), cancellationToken);
        return excluido is null ? NotFound() : NoContent();
    }

    [HttpPost]
    [Authorize(Roles = PapeisSistema.Gestor)]
    [ProducesResponseType<ResponsavelContaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
