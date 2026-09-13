using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.DTOs.Paginacao;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class TarefaRepository(TaskManagerCleanArchDbContext context) : ITarefaRepository
{
    public Task<Tarefa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Tarefas.SingleOrDefaultAsync(tarefa => tarefa.Id == id, cancellationToken);

    public async Task<ResultadoPaginado<Tarefa>> ListarAsync(Guid? projetoId, RequisicaoPaginada paginacao, CancellationToken cancellationToken = default)
    {
        paginacao.Validar();
        var consulta = context.Tarefas.AsNoTracking().Where(tarefa => !projetoId.HasValue || tarefa.ProjetoId == projetoId.Value)
            .OrderBy(tarefa => tarefa.Titulo);
        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta.Skip(paginacao.Deslocamento).Take(paginacao.Tamanho).ToArrayAsync(cancellationToken);
        return ResultadoPaginado<Tarefa>.Criar(itens, paginacao, total);
    }

    public Task<bool> PossuiTarefasParaResponsavelAsync(Guid responsavelId, CancellationToken cancellationToken = default) =>
        context.Tarefas.AnyAsync(tarefa => tarefa.ResponsavelId == responsavelId && tarefa.ConcluidaEm == null, cancellationToken);

    public Task<bool> PossuiTarefasParaStatusAsync(Guid statusId, CancellationToken cancellationToken = default) =>
        context.Tarefas.AnyAsync(tarefa => tarefa.StatusId == statusId, cancellationToken);

    public void Adicionar(Tarefa tarefa) => context.Tarefas.Add(tarefa);
    public void Remover(Tarefa tarefa) => context.Tarefas.Remove(tarefa);
}
