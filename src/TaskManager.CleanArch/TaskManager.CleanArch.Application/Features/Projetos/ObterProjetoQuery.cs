using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Projetos;

public sealed record ObterProjetoQuery(Guid ProjetoId) : IRequest<ProjetoDto>;

public sealed class ObterProjetoQueryHandler(IProjetoRepository projetos, IUsuarioAtual usuarioAtual)
    : IRequestHandler<ObterProjetoQuery, ProjetoDto>
{
    public async Task<ProjetoDto> Handle(ObterProjetoQuery request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();
        var projeto = await projetos.ObterPorIdAsync(request.ProjetoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Projeto");
        return projeto.ParaDto();
    }
}
