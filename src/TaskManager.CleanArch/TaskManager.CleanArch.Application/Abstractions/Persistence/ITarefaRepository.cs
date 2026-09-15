using TaskManager.CleanArch.Application.DTOs.Paginacao;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Abstractions.Persistence;

public interface ITarefaRepository
{
    Task<Tarefa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tarefa>> ListarVisiveisPorProjetoAsync(Guid projetoId, CancellationToken cancellationToken = default);
    Task<ResultadoPaginado<Tarefa>> ListarAsync(Guid? projetoId, RequisicaoPaginada paginacao, CancellationToken cancellationToken = default);
    Task<bool> PossuiTarefasParaResponsavelAsync(Guid responsavelId, CancellationToken cancellationToken = default);
    Task<bool> PossuiTarefasParaStatusAsync(Guid statusId, CancellationToken cancellationToken = default);
    void Adicionar(Tarefa tarefa);
    void Remover(Tarefa tarefa);
}
