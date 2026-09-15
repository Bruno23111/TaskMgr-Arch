using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Abstractions.Identity;

public interface IContaIdentityService
{
    Task<ContaAutenticada?> AutenticarAsync(string email, string senha, CancellationToken cancellationToken = default);
    Task<ContaAutenticada?> ObterAtivaAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResponsavelContaDto>> ListarResponsaveisComPerfisAsync(
        CancellationToken cancellationToken = default);
    Task<ResponsavelContaDto?> ObterResponsavelComPerfilAsync(
        Guid responsavelId,
        CancellationToken cancellationToken = default);
    Task<ResponsavelContaDto?> AtualizarResponsavelAsync(
        Guid responsavelId,
        AtualizarResponsavelConta dados,
        CancellationToken cancellationToken = default);
    Task<ResponsavelContaDto?> DesativarResponsavelAsync(
        Guid responsavelId,
        int versao,
        string usuarioExecutorId,
        CancellationToken cancellationToken = default);
    Task<bool?> ExcluirResponsavelAsync(
        Guid responsavelId,
        int versao,
        string usuarioExecutorId,
        CancellationToken cancellationToken = default);
    Task<ResponsavelContaDto> CriarResponsavelAsync(
        CriarResponsavelConta dados,
        CancellationToken cancellationToken = default);
}
