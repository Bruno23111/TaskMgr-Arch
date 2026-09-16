using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record ListarResponsaveisQuery : IRequest<IReadOnlyList<ResponsavelContaDto>>;

public sealed class ListarResponsaveisQueryHandler(
    IContaIdentityService contas,
    IResponsavelRepository responsaveis,
    IUsuarioAtual usuarioAtual) : IRequestHandler<ListarResponsaveisQuery, IReadOnlyList<ResponsavelContaDto>>
{
    public async Task<IReadOnlyList<ResponsavelContaDto>> Handle(
        ListarResponsaveisQuery request,
        CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();

        var perfis = await responsaveis.ListarAsync(cancellationToken);
        var contasPorUsuario = await contas.ObterContasAsync(perfis.Select(perfil => perfil.UsuarioId), cancellationToken);
        return perfis
            .Where(perfil => contasPorUsuario.TryGetValue(perfil.UsuarioId, out _))
            .Select(perfil => perfil.ParaDto(contasPorUsuario[perfil.UsuarioId]))
            .ToArray();
    }
}

public sealed record ObterResponsavelPorIdQuery(Guid Id) : IRequest<ResponsavelContaDto?>;

public sealed class ObterResponsavelPorIdQueryHandler(
    IContaIdentityService contas,
    IResponsavelRepository responsaveis,
    IUsuarioAtual usuarioAtual) : IRequestHandler<ObterResponsavelPorIdQuery, ResponsavelContaDto?>
{
    public async Task<ResponsavelContaDto?> Handle(
        ObterResponsavelPorIdQuery request,
        CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();

        var perfil = await responsaveis.ObterPorIdAsync(request.Id, cancellationToken);
        if (perfil is null)
        {
            return null;
        }

        var conta = await contas.ObterContaAsync(perfil.UsuarioId, cancellationToken);
        return conta is null ? null : perfil.ParaDto(conta);
    }
}
