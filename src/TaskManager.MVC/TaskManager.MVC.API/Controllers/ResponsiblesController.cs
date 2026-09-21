using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Responsibles;
using TaskManager.MVC.API.Identity;

namespace TaskManager.MVC.API.Controllers;

[ApiController]
[Authorize]
[Route("responsaveis")]
public sealed class ResponsiblesController(AccountService service, CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResponsibleResponse>>> Listar(CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(cancellationToken));

    [HttpGet("{responsavelId:guid}")]
    public async Task<ActionResult<ResponsibleResponse>> Obter(Guid responsavelId, CancellationToken cancellationToken)
    {
        var responsible = await service.ObterAsync(responsavelId, cancellationToken);
        if (responsible is null)
        {
            throw new ApiException(
                404,
                "RECURSO_NAO_ENCONTRADO",
                "Recurso não encontrado.",
                "O responsável informado não existe.");
        }

        return Ok(responsible);
    }

    [HttpPost]
    [Authorize(Roles = "Gestor")]
    public async Task<ActionResult<ResponsibleResponse>> Criar(
        ResponsibleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var responsible = await service.CriarAsync(
            request.Nome,
            request.Email,
            request.Senha,
            request.Papel,
            cancellationToken);
        var response = await service.ObterAsync(responsible.Id, cancellationToken)
            ?? throw new InvalidOperationException("A conta criada não pôde ser consultada.");
        return CreatedAtAction(nameof(Obter), new { responsavelId = responsible.Id }, response);
    }

    [HttpPut("{responsavelId:guid}")]
    [Authorize(Roles = "Gestor")]
    public async Task<ActionResult<ResponsibleResponse>> Atualizar(
        Guid responsavelId,
        ResponsibleUpdateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AtualizarAsync(responsavelId, request, currentUser.Id, cancellationToken));

    [HttpPatch("{responsavelId:guid}/desativacao")]
    [Authorize(Roles = "Gestor")]
    public async Task<ActionResult<ResponsibleResponse>> Desativar(
        Guid responsavelId,
        DeactivateResponsibleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.DesativarAsync(responsavelId, request.Versao, currentUser.Id, cancellationToken));

    [HttpDelete("{responsavelId:guid}")]
    [Authorize(Roles = "Gestor")]
    public async Task<IActionResult> Excluir(
        Guid responsavelId,
        [FromQuery] int versao,
        CancellationToken cancellationToken)
    {
        await service.ExcluirAsync(responsavelId, versao, currentUser.Id, cancellationToken);
        return NoContent();
    }
}
