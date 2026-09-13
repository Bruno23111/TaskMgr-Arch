using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record CriarResponsavelCommand(
    string Nome,
    string Email,
    string Senha,
    string Papel) : IRequest<ResponsavelContaDto>;

public sealed class CriarResponsavelCommandHandler(
    IContaIdentityService contas,
    IUsuarioAtual usuarioAtual) : IRequestHandler<CriarResponsavelCommand, ResponsavelContaDto>
{
    public Task<ResponsavelContaDto> Handle(CriarResponsavelCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();

        if (!PapeisSistema.Valido(request.Papel))
            throw new ArgumentException("O papel deve ser Usuario ou Gestor.", nameof(request.Papel));

        return contas.CriarResponsavelAsync(
            new CriarResponsavelConta(request.Nome, request.Email, request.Senha, request.Papel),
            cancellationToken);
    }
}
