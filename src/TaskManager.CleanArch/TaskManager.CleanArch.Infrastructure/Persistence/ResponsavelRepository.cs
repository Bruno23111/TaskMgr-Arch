using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class ResponsavelRepository(TaskManagerCleanArchDbContext context) : IResponsavelRepository
{
    public Task<Responsavel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Responsaveis.SingleOrDefaultAsync(responsavel => responsavel.Id == id, cancellationToken);

    public Task<Responsavel?> ObterPorUsuarioIdAsync(string usuarioId, CancellationToken cancellationToken = default) =>
        context.Responsaveis.SingleOrDefaultAsync(responsavel => responsavel.UsuarioId == usuarioId, cancellationToken);

    public async Task<bool> BloquearParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var responsavelBloqueado = await context.Database
            .SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"Responsaveis\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        return responsavelBloqueado != Guid.Empty;
    }

    public async Task<IReadOnlyList<Responsavel>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.Responsaveis.AsNoTracking().OrderBy(responsavel => responsavel.Nome).ToArrayAsync(cancellationToken);

    public void Adicionar(Responsavel responsavel) => context.Responsaveis.Add(responsavel);
    public void Remover(Responsavel responsavel) => context.Responsaveis.Remove(responsavel);
}
