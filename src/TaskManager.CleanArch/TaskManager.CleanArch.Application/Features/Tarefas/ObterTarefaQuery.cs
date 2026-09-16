using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record ObterTarefaQuery(Guid TarefaId) : IRequest<TarefaDto>;

public sealed class ObterTarefaQueryHandler(ITarefaRepository tarefas, IUsuarioAtual usuarioAtual)
    : IRequestHandler<ObterTarefaQuery, TarefaDto>
{
    public async Task<TarefaDto> Handle(ObterTarefaQuery request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();
        var tarefa = await tarefas.ObterPorIdAsync(request.TarefaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Tarefa");
        return tarefa.ParaDto();
    }
}
