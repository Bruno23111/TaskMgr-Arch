using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Abstractions.Persistence;

public interface IStatusRepository
{
    Task<StatusTarefa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Status que toda tarefa nova recebe: o status protegido da categoria Pendente criado pelo seed.
    /// Status adicionais da mesma categoria nunca são usados como status inicial.
    /// </summary>
    Task<StatusTarefa?> ObterPendenteInicialAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatusTarefa>> ListarAsync(CancellationToken cancellationToken = default);
    Task<bool> CodigoEmUsoAsync(string codigo, Guid? ignorarStatusId = null, CancellationToken cancellationToken = default);
    void Adicionar(StatusTarefa status);
    void Remover(StatusTarefa status);
}
