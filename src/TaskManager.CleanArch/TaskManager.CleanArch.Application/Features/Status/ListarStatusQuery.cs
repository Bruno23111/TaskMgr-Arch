using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Status;

public sealed record ListarStatusQuery : IRequest<IReadOnlyList<StatusTarefaDto>>;

public sealed class ListarStatusQueryHandler(IStatusRepository status, IUsuarioAtual usuarioAtual)
    : IRequestHandler<ListarStatusQuery, IReadOnlyList<StatusTarefaDto>>
{
    public async Task<IReadOnlyList<StatusTarefaDto>> Handle(ListarStatusQuery request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();
        return (await status.ListarAsync(cancellationToken)).Select(item => item.ParaDto()).ToArray();
    }
}
