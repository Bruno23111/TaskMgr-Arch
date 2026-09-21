using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Common;
using TaskManager.MVC.API.Contracts.Projects;
using TaskManager.MVC.API.Services.Projects;

namespace TaskManager.MVC.API.Controllers;

[ApiController]
[Authorize]
[Route("projetos")]
public sealed class ProjectsController(ProjectService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<ProjectResponse>>> Listar(
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 100)] int tamanho = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.ListarAsync(pagina, tamanho, cancellationToken));

    [HttpGet("{projetoId:guid}")]
    public async Task<ActionResult<ProjectResponse>> Obter(Guid projetoId, CancellationToken cancellationToken)
    {
        var project = await service.ObterAsync(projetoId, cancellationToken);
        if (project is null)
        {
            throw new ApiException(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "O projeto informado não existe.");
        }

        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Criar(
        ProjectCreateRequest request,
        CancellationToken cancellationToken)
    {
        var project = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { projetoId = project.Id }, project);
    }

    [HttpPut("{projetoId:guid}")]
    public async Task<ActionResult<ProjectResponse>> Atualizar(
        Guid projetoId,
        ProjectUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AtualizarAsync(projetoId, request, cancellationToken));

    [HttpDelete("{projetoId:guid}")]
    public async Task<IActionResult> Excluir(
        Guid projetoId,
        [FromQuery] int versao,
        CancellationToken cancellationToken)
    {
        await service.ExcluirAsync(projetoId, versao, cancellationToken);
        return NoContent();
    }
}
