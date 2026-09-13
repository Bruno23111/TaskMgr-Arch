using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Abstractions.Identity;

// Abstrai ASP.NET Core Identity para que a Application não dependa de UserManager ou EF Core.
public interface IContaIdentityService
{
    Task<ContaAutenticada?> AutenticarAsync(string email, string senha, CancellationToken cancellationToken = default);
    Task<ContaAutenticada?> ObterAtivaAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<ResponsavelContaDto> CriarResponsavelAsync(
        CriarResponsavelConta dados,
        CancellationToken cancellationToken = default);
}
