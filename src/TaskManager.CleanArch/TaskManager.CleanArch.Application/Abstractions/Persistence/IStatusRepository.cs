using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Abstractions.Persistence;

public interface IStatusRepository
{
    Task<StatusTarefa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StatusTarefa?> ObterPendenteInicialAsync(CancellationToken cancellationToken = default);
    Task<bool> CodigoEmUsoAsync(string codigo, Guid? ignorarStatusId = null, CancellationToken cancellationToken = default);
    void Adicionar(StatusTarefa status);
    void Remover(StatusTarefa status);
}
