using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Abstractions.Identity;

public interface IContaIdentityService
{
    Task<ContaAutenticada?> AutenticarAsync(string email, string senha, CancellationToken cancellationToken = default);
    Task<ContaAutenticada?> ObterAtivaAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<ContaIdentityDto?> ObterContaAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, ContaIdentityDto>> ObterContasAsync(IEnumerable<string> usuarioIds, CancellationToken cancellationToken = default);
    Task<ContaIdentityDto> CriarContaAsync(CriarContaIdentity dados, CancellationToken cancellationToken = default);
    Task AtualizarContaAsync(string usuarioId, string email, string papel, CancellationToken cancellationToken = default);
    Task DesativarContaAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task ExcluirContaAsync(string usuarioId, CancellationToken cancellationToken = default);
    Task<int> ContarGestoresAtivosAsync(CancellationToken cancellationToken = default);
}
