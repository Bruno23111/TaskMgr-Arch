using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Status;
using TaskManager.MVC.API.Services.Status;

namespace TaskManager.MVC.API.Controllers;

[ApiController]
[Authorize]
[Route("status")]
public sealed class StatusController(StatusService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StatusResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StatusResponse>>> Listar(CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(cancellationToken));

    [HttpGet("{statusId:guid}")]
    [ProducesResponseType<StatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StatusResponse>> Obter(Guid statusId, CancellationToken cancellationToken)
    {
        var status = await service.ObterAsync(statusId, cancellationToken);
        if (status is null)
        {
            throw new ApiException(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "O status informado não existe.");
        }

        return Ok(status);
    }

    [HttpPost]
    [Authorize(Roles = "Gestor")]
    [ProducesResponseType<StatusResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StatusResponse>> Criar(
        StatusCreateRequest request,
        CancellationToken cancellationToken)
    {
        var status = await service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { statusId = status.Id }, status);
    }

    [HttpPut("{statusId:guid}")]
    [Authorize(Roles = "Gestor")]
    [ProducesResponseType<StatusResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StatusResponse>> Atualizar(
        Guid statusId,
        StatusUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AtualizarAsync(statusId, request, cancellationToken));

    [HttpDelete("{statusId:guid}")]
    [Authorize(Roles = "Gestor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(
        Guid statusId,
        [FromQuery] int versao,
        CancellationToken cancellationToken)
    {
        await service.ExcluirAsync(statusId, versao, cancellationToken);
        return NoContent();
    }
}
