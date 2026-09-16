using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record DesativarResponsavelCommand(Guid Id, int Versao) : IRequest<ResponsavelContaDto?>;

public sealed class DesativarResponsavelCommandHandler(
    IContaIdentityService contas,
    IUsuarioAtual usuarioAtual) : IRequestHandler<DesativarResponsavelCommand, ResponsavelContaDto?>
{
    public Task<ResponsavelContaDto?> Handle(DesativarResponsavelCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        request.Versao.ExigirVersaoInformada();
        return contas.DesativarResponsavelAsync(
            request.Id,
            request.Versao,
            usuarioAtual.UsuarioId,
            cancellationToken);
    }
}
