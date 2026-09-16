using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.Features.Status;
using TaskManager.CleanArch.WebAPI.Contracts.Requests;

namespace TaskManager.CleanArch.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("status")]
public sealed class StatusController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StatusTarefaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StatusTarefaDto>>> Listar(CancellationToken cancellationToken)
        => Ok(await sender.Send(new ListarStatusQuery(), cancellationToken));

    [HttpGet("{statusId:guid}")]
    [ProducesResponseType<StatusTarefaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StatusTarefaDto>> Obter(Guid statusId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ObterStatusQuery(statusId), cancellationToken));

    [HttpPost]
    [Authorize(Roles = PapeisSistema.Gestor)]
    [ProducesResponseType<StatusTarefaDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StatusTarefaDto>> Criar(CriarStatusRequest request, CancellationToken cancellationToken)
    {
        var status = await sender.Send(new CriarStatusCommand(request.Codigo, request.Nome, request.Categoria!.Value), cancellationToken);
        return Created($"/status/{status.Id}", status);
    }

    [HttpPut("{statusId:guid}")]
    [Authorize(Roles = PapeisSistema.Gestor)]
    [ProducesResponseType<StatusTarefaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StatusTarefaDto>> Atualizar(Guid statusId, AtualizarStatusRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new AtualizarStatusCommand(statusId, request.Nome, request.Versao), cancellationToken));

    [HttpDelete("{statusId:guid}")]
    [Authorize(Roles = PapeisSistema.Gestor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(
        Guid statusId,
        [FromQuery] int versao,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ExcluirStatusCommand(statusId, versao), cancellationToken);
        return NoContent();
    }
}
