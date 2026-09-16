using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record AlterarStatusTarefaCommand(Guid TarefaId, Guid StatusId, int Versao) : IRequest<TarefaDto>;

public sealed class AlterarStatusTarefaCommandHandler(
    ITarefaRepository tarefas,
    IProjetoRepository projetos,
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
        request.Versao.ExigirVersaoInformada();
        if (request.StatusId == Guid.Empty)
            throw new ArgumentException("O status é obrigatório.", nameof(request.StatusId));

        IReadOnlyCollection<IDomainEvent> eventos = [];
        var resultado = await unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            var tarefa = await tarefas.ObterPorIdAsync(request.TarefaId, token)
                ?? throw new RecursoNaoEncontradoException("Tarefa");
            if (!await projetos.BloquearParaAtualizacaoAsync(tarefa.ProjetoId, token))
                throw new RecursoNaoEncontradoException("Projeto");

            var statusAtual = await status.ObterPorIdAsync(tarefa.StatusId, token)
                ?? throw new RecursoNaoEncontradoException("Status atual da tarefa");
            var novoStatus = await status.ObterPorIdAsync(request.StatusId, token)
                ?? throw new ArgumentException("O status informado não existe.", nameof(request.StatusId));

            if (!usuarioAtual.Gestor)
            {
                var responsavelAtual = await responsaveis.ObterPorUsuarioIdAsync(usuarioAtual.UsuarioId, token);
                if (responsavelAtual?.Id != tarefa.ResponsavelId)
                    throw new AcessoNegadoException();
            }

            tarefa.Versao.ExigirVersao(request.Versao);
            var responsavelDaTarefa = tarefa.ResponsavelId is { } responsavelId
                ? await responsaveis.ObterPorIdAsync(responsavelId, token)
                : null;

            tarefa.AlterarStatus(
                novoStatus.Id,
                statusAtual.Categoria,
                novoStatus.Categoria,
                responsavelDaTarefa?.Ativo == true,
                usuarioAtual.Gestor,
                relogio.DataHoraAgoraUTC);

            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            eventos = tarefa.LimparDomainEvents();
            return tarefa.ParaDto();
        }, cancellationToken);

        await dispatcher.DespacharAsync(eventos, cancellationToken);
        return resultado;
    }
}
