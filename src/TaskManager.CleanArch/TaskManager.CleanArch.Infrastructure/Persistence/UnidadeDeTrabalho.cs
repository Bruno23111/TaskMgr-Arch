using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class UnidadeDeTrabalho(TaskManagerCleanArchDbContext context) : IUnidadeDeTrabalho
{
    public async Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoDeConcorrenciaException();
        }
    }

    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken = default)
    {
        await using var transacao = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var resultado = await operacao(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
            return resultado;
        }
        catch
        {
            await transacao.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
