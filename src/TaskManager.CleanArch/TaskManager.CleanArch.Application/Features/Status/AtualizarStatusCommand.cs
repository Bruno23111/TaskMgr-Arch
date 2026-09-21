using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Status;

public sealed record AtualizarStatusCommand(Guid StatusId, string Nome, int Versao) : IRequest<StatusTarefaDto>;

public sealed class AtualizarStatusCommandHandler(
    IStatusRepository status,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<AtualizarStatusCommand, StatusTarefaDto>
{
    public async Task<StatusTarefaDto> Handle(AtualizarStatusCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirVersaoInformada(request.Versao);
        var nome = RequestValidator.ExigirTexto(request.Nome, "nome", 60);
        var statusTarefa = await status.ObterPorIdAsync(request.StatusId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Status");
        RequestValidator.ExigirVersaoAtual(statusTarefa.Versao, request.Versao);
        statusTarefa.Renomear(nome);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return statusTarefa.ParaDto();
    }
}
