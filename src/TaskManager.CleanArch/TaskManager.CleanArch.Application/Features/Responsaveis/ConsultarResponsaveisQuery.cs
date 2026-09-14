using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record ListarResponsaveisQuery : IRequest<IReadOnlyList<ResponsavelContaDto>>;

public sealed class ListarResponsaveisQueryHandler(
    IContaIdentityService contas,
    IUsuarioAtual usuarioAtual) : IRequestHandler<ListarResponsaveisQuery, IReadOnlyList<ResponsavelContaDto>>
{
    public Task<IReadOnlyList<ResponsavelContaDto>> Handle(
        ListarResponsaveisQuery request,
        CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        return contas.ListarResponsaveisComPerfisAsync(cancellationToken);
    }
}

public sealed record ObterResponsavelPorIdQuery(Guid Id) : IRequest<ResponsavelContaDto?>;

public sealed class ObterResponsavelPorIdQueryHandler(
    IContaIdentityService contas,
    IUsuarioAtual usuarioAtual) : IRequestHandler<ObterResponsavelPorIdQuery, ResponsavelContaDto?>
{
    public Task<ResponsavelContaDto?> Handle(
        ObterResponsavelPorIdQuery request,
        CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        return contas.ObterResponsavelComPerfilAsync(request.Id, cancellationToken);
    }
}
