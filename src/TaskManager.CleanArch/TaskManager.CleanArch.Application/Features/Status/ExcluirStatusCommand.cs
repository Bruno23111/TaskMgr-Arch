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
        request.Versao.ExigirVersaoInformada();
        var statusTarefa = await status.ObterPorIdAsync(request.StatusId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Status");
        statusTarefa.Versao.ExigirVersao(request.Versao);
        statusTarefa.GarantirPodeSerExcluido(await tarefas.PossuiTarefasParaStatusAsync(statusTarefa.Id, cancellationToken));
        status.Remover(statusTarefa);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }
}
