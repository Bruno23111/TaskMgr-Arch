using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
        })
        {
            throw new ConflitoDeConcorrenciaException();
        }
    }

    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken = default)
    {
        return await ExecutarEmTransacaoAsync(operacao, IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task<T> ExecutarEmTransacaoSerializavelAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken = default)
    {
        return await ExecutarEmTransacaoAsync(operacao, IsolationLevel.Serializable, cancellationToken);
    }

    private async Task<T> ExecutarEmTransacaoAsync<T>(
        Func<CancellationToken, Task<T>> operacao,
        IsolationLevel isolamento,
        CancellationToken cancellationToken)
    {
        await using var transacao = await context.Database.BeginTransactionAsync(isolamento, cancellationToken);
        try
        {
            var resultado = await operacao(cancellationToken);
            await transacao.CommitAsync(cancellationToken);
            return resultado;
        }
        catch (Exception exception)
        {
            await transacao.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();

            var postgresException = ObterPostgresException(exception);
            if (postgresException?.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
            {
                throw new ConflitoDeConcorrenciaException();
            }

            if (postgresException?.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new ValorDuplicadoException("Já existe um registro com esse valor único.");
            }

            throw;
        }
    }

    private static PostgresException? ObterPostgresException(Exception exception) =>
        exception as PostgresException ?? exception.InnerException as PostgresException;
}
