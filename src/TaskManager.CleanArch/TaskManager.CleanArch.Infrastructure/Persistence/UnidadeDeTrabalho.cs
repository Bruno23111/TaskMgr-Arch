using TaskManager.CleanArch.Application.Abstractions.Persistence;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class UnidadeDeTrabalho(TaskManagerCleanArchDbContext context) : IUnidadeDeTrabalho
{
    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
