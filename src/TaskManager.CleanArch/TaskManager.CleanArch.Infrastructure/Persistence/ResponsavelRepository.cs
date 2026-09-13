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

    public void Adicionar(Responsavel responsavel) => context.Responsaveis.Add(responsavel);
    public void Remover(Responsavel responsavel) => context.Responsaveis.Remove(responsavel);
}
