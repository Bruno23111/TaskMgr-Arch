using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.DTOs.Paginacao;
using TaskManager.CleanArch.Application.Features.Projetos;
using TaskManager.CleanArch.WebAPI.Contracts.Requests;

namespace TaskManager.CleanArch.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("projetos")]
public sealed class ProjetosController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResultadoPaginado<ProjetoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<ProjetoDto>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await sender.Send(new ListarProjetosQuery(pagina, tamanho), cancellationToken);
        return Ok(resultado);
    }

    [HttpPost]
    [ProducesResponseType<ProjetoDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjetoDto>> Criar(
        CriarProjetoRequest request,
        CancellationToken cancellationToken)
    {
        var projeto = await sender.Send(new CriarProjetoCommand(request.Nome, request.Descricao), cancellationToken);
        return Created($"/projetos/{projeto.Id}", projeto);
    }

    [HttpGet("{projetoId:guid}")]
    [ProducesResponseType<ProjetoDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjetoDto>> Obter(Guid projetoId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new ObterProjetoQuery(projetoId), cancellationToken));

    [HttpPut("{projetoId:guid}")]
    [ProducesResponseType<ProjetoDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjetoDto>> Atualizar(Guid projetoId, AtualizarProjetoRequest request, CancellationToken cancellationToken)
        => Ok(await sender.Send(new AtualizarProjetoCommand(projetoId, request.Nome, request.Descricao, request.Versao), cancellationToken));

    [HttpDelete("{projetoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Excluir(Guid projetoId, [FromQuery] int versao, CancellationToken cancellationToken)
    {
        await sender.Send(new ExcluirProjetoCommand(projetoId, versao), cancellationToken);
        return NoContent();
    }
}
