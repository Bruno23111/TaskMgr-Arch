using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Abstractions.Persistence;

public interface IResponsavelRepository
{
    Task<Responsavel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Responsavel?> ObterPorUsuarioIdAsync(string usuarioId, CancellationToken cancellationToken = default);
    void Adicionar(Responsavel responsavel);
    void Remover(Responsavel responsavel);
}
