using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Abstractions.Persistence;

public interface IProjetoRepository
{
    Task<Projeto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PossuiTarefaEmAndamentoAsync(Guid projetoId, CancellationToken cancellationToken = default);
    void Adicionar(Projeto projeto);
    void Remover(Projeto projeto);
}
