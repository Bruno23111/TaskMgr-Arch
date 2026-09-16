using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Status;

public sealed record ObterStatusQuery(Guid StatusId) : IRequest<StatusTarefaDto>;

public sealed class ObterStatusQueryHandler(IStatusRepository status, IUsuarioAtual usuarioAtual)
    : IRequestHandler<ObterStatusQuery, StatusTarefaDto>
{
    public async Task<StatusTarefaDto> Handle(ObterStatusQuery request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();
        var statusTarefa = await status.ObterPorIdAsync(request.StatusId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Status");
        return statusTarefa.ParaDto();
    }
}
