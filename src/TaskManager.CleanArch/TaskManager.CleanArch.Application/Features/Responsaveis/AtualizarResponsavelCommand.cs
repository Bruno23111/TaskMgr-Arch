using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record AtualizarResponsavelCommand(
    Guid Id,
    string Nome,
    string Email) : IRequest<ResponsavelContaDto?>;

public sealed class AtualizarResponsavelCommandHandler(
    IContaIdentityService contas,
    IUsuarioAtual usuarioAtual) : IRequestHandler<AtualizarResponsavelCommand, ResponsavelContaDto?>
{
    public Task<ResponsavelContaDto?> Handle(
        AtualizarResponsavelCommand request,
        CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();

        return contas.AtualizarResponsavelAsync(
            request.Id,
            new AtualizarResponsavelConta(request.Nome, request.Email),
            cancellationToken);
    }
}
