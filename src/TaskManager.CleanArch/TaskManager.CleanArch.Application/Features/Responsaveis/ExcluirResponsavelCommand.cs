using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Common;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record ExcluirResponsavelCommand(Guid Id, int Versao) : IRequest<bool?>;

public sealed class ExcluirResponsavelCommandHandler(
    IContaIdentityService contas,
    IUsuarioAtual usuarioAtual) : IRequestHandler<ExcluirResponsavelCommand, bool?>
{
    public Task<bool?> Handle(ExcluirResponsavelCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        request.Versao.ExigirVersaoInformada();
        return contas.ExcluirResponsavelAsync(
            request.Id,
            request.Versao,
            usuarioAtual.UsuarioId,
            cancellationToken);
    }
}
