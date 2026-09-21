using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;

namespace TaskManager.CleanArch.Application.Features.Status;

public sealed record ExcluirStatusCommand(Guid StatusId, int Versao) : IRequest;

public sealed class ExcluirStatusCommandHandler(
    IStatusRepository status,
    ITarefaRepository tarefas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<ExcluirStatusCommand>
{
    public async Task Handle(ExcluirStatusCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirVersaoInformada(request.Versao);
        var statusTarefa = await status.ObterPorIdAsync(request.StatusId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Status");
        RequestValidator.ExigirVersaoAtual(statusTarefa.Versao, request.Versao);
        statusTarefa.GarantirPodeSerExcluido(await tarefas.PossuiTarefasParaStatusAsync(statusTarefa.Id, cancellationToken));
        status.Remover(statusTarefa);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }
}
