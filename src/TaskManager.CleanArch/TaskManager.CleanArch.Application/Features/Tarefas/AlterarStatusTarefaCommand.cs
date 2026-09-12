using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record AlterarStatusTarefaCommand(Guid TarefaId, Guid StatusId, int Versao) : IRequest<TarefaDto>;

public sealed class AlterarStatusTarefaCommandHandler(
    ITarefaRepository tarefas,
    IStatusRepository status,
    IResponsavelRepository responsaveis,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual,
    IRelogio relogio,
    IDomainEventDispatcher dispatcher) : IRequestHandler<AlterarStatusTarefaCommand, TarefaDto>
{
    public async Task<TarefaDto> Handle(AlterarStatusTarefaCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();

        var tarefa = await tarefas.ObterPorIdAsync(request.TarefaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Tarefa");

        var statusAtual = await status.ObterPorIdAsync(tarefa.StatusId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Status atual da tarefa");

        var novoStatus = await status.ObterPorIdAsync(request.StatusId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Novo status");

        if (!usuarioAtual.Gestor)
        {
            var responsavelAtual = await responsaveis.ObterPorUsuarioIdAsync(usuarioAtual.UsuarioId, cancellationToken);

            if (responsavelAtual?.Id != tarefa.ResponsavelId)
                throw new AcessoNegadoException();
        }

        tarefa.Versao.ExigirVersao(request.Versao);

        var responsavelDaTarefa = tarefa.ResponsavelId is { } responsavelId
            ? await responsaveis.ObterPorIdAsync(responsavelId, cancellationToken)
            : null;

        tarefa.AlterarStatus(
            novoStatus.Id,
            statusAtual.Categoria,
            novoStatus.Categoria,
            responsavelDaTarefa?.Ativo == true,
            usuarioAtual.Gestor,
            relogio.DataHoraAgoraUTC);

        await tarefa.SalvarDespacharAsync(unidadeDeTrabalho, dispatcher, cancellationToken);

        return tarefa.ParaDto();
    }
}
