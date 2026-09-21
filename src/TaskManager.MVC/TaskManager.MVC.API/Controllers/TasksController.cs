using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Common;
using TaskManager.MVC.API.Contracts.Tasks;
using TaskManager.MVC.API.Services.Tasks;

namespace TaskManager.MVC.API.Controllers;

[ApiController]
[Authorize]
[Route("tarefas")]
public sealed class TasksController(TaskService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<TaskResponse>>> Listar(
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 100)] int tamanho = 20,
        [FromQuery] Guid? projetoId = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ListarAsync(pagina, tamanho, projetoId, cancellationToken));

    [HttpGet("{tarefaId:guid}")]
    public async Task<ActionResult<TaskResponse>> Obter(Guid tarefaId, CancellationToken cancellationToken)
    {
        var task = await service.ObterAsync(tarefaId, cancellationToken);
        if (task is null)
        {
            throw new ApiException(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "A tarefa informada não existe.");
        }
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Criar(
        TaskCreateRequest request,
        CancellationToken cancellationToken)
    {
        var task = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { tarefaId = task.Id }, task);
    }

    [HttpPut("{tarefaId:guid}")]
    public async Task<ActionResult<TaskResponse>> Atualizar(
        Guid tarefaId,
        TaskUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AtualizarAsync(tarefaId, request, cancellationToken));

    [HttpDelete("{tarefaId:guid}")]
    public async Task<IActionResult> Excluir(
        Guid tarefaId,
        [FromQuery] int versao,
        CancellationToken cancellationToken)
    {
        await service.ExcluirAsync(tarefaId, versao, cancellationToken);
        return NoContent();
    }

    [HttpPut("{tarefaId:guid}/responsavel")]
    public async Task<ActionResult<TaskResponse>> AtribuirResponsavel(
        Guid tarefaId,
        AssignResponsibleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AtribuirResponsavelAsync(tarefaId, request, cancellationToken));

    [HttpDelete("{tarefaId:guid}/responsavel")]
    public async Task<ActionResult<TaskResponse>> RemoverResponsavel(
        Guid tarefaId,
        [FromQuery] int versao,
        CancellationToken cancellationToken) =>
        Ok(await service.RemoverResponsavelAsync(tarefaId, versao, cancellationToken));

    [HttpPatch("{tarefaId:guid}/status")]
    public async Task<ActionResult<TaskResponse>> AlterarStatus(
        Guid tarefaId,
        ChangeTaskStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AlterarStatusAsync(tarefaId, request, cancellationToken));
}
