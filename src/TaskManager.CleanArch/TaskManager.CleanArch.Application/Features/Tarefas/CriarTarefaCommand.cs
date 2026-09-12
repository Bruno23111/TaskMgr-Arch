using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record CriarTarefaCommand(Guid ProjetoId, string Titulo, string? Descricao) : IRequest<TarefaDto>;

public sealed class CriarTarefaCommandHandler(
    IProjetoRepository projetos,
    IStatusRepository status,
    ITarefaRepository tarefas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<CriarTarefaCommand, TarefaDto>
{
    public async Task<TarefaDto> Handle(CriarTarefaCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        if (await projetos.ObterPorIdAsync(request.ProjetoId, cancellationToken) is null)
            throw new RecursoNaoEncontradoException("Projeto");

        var statusInicial = await status.ObterPendenteInicialAsync(cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Status inicial pendente");
            
        var tarefa = Tarefa.Criar(request.ProjetoId, request.Titulo, request.Descricao, statusInicial.Id);
        tarefas.Adicionar(tarefa);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return tarefa.ParaDto();
    }
}
