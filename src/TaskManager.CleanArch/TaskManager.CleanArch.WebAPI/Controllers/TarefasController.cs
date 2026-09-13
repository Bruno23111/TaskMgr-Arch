using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.DTOs.Paginacao;
using TaskManager.CleanArch.Application.Features.Tarefas;
using TaskManager.CleanArch.WebAPI.Contracts.Requests;

namespace TaskManager.CleanArch.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("tarefas")]
public sealed class TarefasController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<TarefaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<TarefaDto>>> Listar(
        [FromQuery] Guid? projetoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await sender.Send(new ListarTarefasQuery(projetoId, pagina, tamanho), cancellationToken);
        return Ok(resultado);
    }

    [HttpPost]
    [ProducesResponseType<TarefaDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TarefaDto>> Criar(
        CriarTarefaRequest request,
        CancellationToken cancellationToken)
    {
        var tarefa = await sender.Send(new CriarTarefaCommand(request.ProjetoId, request.Titulo, request.Descricao), cancellationToken);
        return Created($"/tarefas/{tarefa.Id}", tarefa);
    }

    [HttpPut("{tarefaId:guid}")]
    [ProducesResponseType<TarefaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TarefaDto>> Atualizar(
        Guid tarefaId,
        AtualizarTarefaRequest request,
        CancellationToken cancellationToken)
    {
        var tarefa = await sender.Send(
            new AtualizarTarefaCommand(tarefaId, request.Titulo, request.Descricao, request.Versao),
            cancellationToken);
        return Ok(tarefa);
    }

    [HttpPatch("{tarefaId:guid}/status")]
    [ProducesResponseType<TarefaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TarefaDto>> AlterarStatus(
        Guid tarefaId,
        AlterarStatusRequest request,
        CancellationToken cancellationToken)
    {
        var tarefa = await sender.Send(
            new AlterarStatusTarefaCommand(tarefaId, request.StatusId, request.Versao),
            cancellationToken);
        return Ok(tarefa);
    }
}
