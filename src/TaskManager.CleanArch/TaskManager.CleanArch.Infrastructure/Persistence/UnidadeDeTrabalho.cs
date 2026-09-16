using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ValorDuplicadoException("Já existe um registro com esse valor único.");
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
