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
        catch (Exception exception) when (ObterPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ValorDuplicadoException("Já existe um registro com esse valor único.");
        }
        catch (Exception exception) when (ObterPostgresException(exception)?.SqlState is
            PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
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

    // Falhas transitórias chegam embrulhadas pelo provedor (InvalidOperationException → DbUpdateException → PostgresException).
    private static PostgresException? ObterPostgresException(Exception exception)
    {
        for (var atual = exception; atual is not null; atual = atual.InnerException)
        {
            if (atual is PostgresException postgresException)
                return postgresException;
        }

        return null;
    }
}
