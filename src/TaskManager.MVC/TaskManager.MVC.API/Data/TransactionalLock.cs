using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Data;

public static class TransactionalLock
{
    public static Task<Projeto?> ProjetoAsync(
        this TaskManagerMvcDbContext dbContext,
        Guid projetoId,
        CancellationToken cancellationToken = default)
    {
        EnsureTransaction(dbContext);

        return dbContext.Projetos
            .FromSqlInterpolated($"SELECT * FROM \"Projetos\" WHERE \"Id\" = {projetoId} FOR UPDATE")
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static Task<Responsavel?> ResponsavelAsync(
        this TaskManagerMvcDbContext dbContext,
        Guid responsavelId,
        CancellationToken cancellationToken = default)
    {
        EnsureTransaction(dbContext);

        return dbContext.Responsaveis
            .FromSqlInterpolated($"SELECT * FROM \"Responsaveis\" WHERE \"Id\" = {responsavelId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static void EnsureTransaction(TaskManagerMvcDbContext dbContext)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("O bloqueio transacional exige uma transação ativa.");
        }
    }
}
