using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class StatusRepository(TaskManagerCleanArchDbContext context) : IStatusRepository
{
    public Task<StatusTarefa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Statuses.SingleOrDefaultAsync(status => status.Id == id, cancellationToken);

    public Task<StatusTarefa?> ObterPendenteInicialAsync(CancellationToken cancellationToken = default) =>
        context.Statuses.OrderBy(status => status.Codigo)
            .FirstOrDefaultAsync(status => status.Categoria == CategoriaStatus.Pendente, cancellationToken);

    public Task<bool> CodigoEmUsoAsync(string codigo, Guid? ignorarStatusId = null, CancellationToken cancellationToken = default) =>
        context.Statuses.AnyAsync(status => status.Codigo == codigo.ToUpperInvariant() &&
            (!ignorarStatusId.HasValue || status.Id != ignorarStatusId.Value), cancellationToken);

    public void Adicionar(StatusTarefa status) => context.Statuses.Add(status);
    public void Remover(StatusTarefa status) => context.Statuses.Remove(status);
}
