namespace TaskManager.CleanArch.Application.Abstractions.Persistence;

public interface IUnidadeDeTrabalho
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
