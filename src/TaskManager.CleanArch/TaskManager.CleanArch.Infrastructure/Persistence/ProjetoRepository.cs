using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.DTOs.Paginacao;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class ProjetoRepository(TaskManagerCleanArchDbContext context) : IProjetoRepository
{
    public Task<Projeto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Projetos.SingleOrDefaultAsync(projeto => projeto.Id == id, cancellationToken);

    public async Task<bool> BloquearParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var projetoBloqueado = await context.Database
            .SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"Projetos\" WHERE \"Id\" = {id} AND \"ExcluidoEm\" IS NULL FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        return projetoBloqueado != Guid.Empty;
    }

    public async Task<ResultadoPaginado<Projeto>> ListarAsync(RequisicaoPaginada paginacao, CancellationToken cancellationToken = default)
    {
        paginacao.Validar();
        var consulta = context.Projetos.AsNoTracking().OrderBy(projeto => projeto.Nome);
        var total = await consulta.CountAsync(cancellationToken);
        var itens = await consulta.Skip(paginacao.Deslocamento).Take(paginacao.Tamanho).ToArrayAsync(cancellationToken);
        return ResultadoPaginado<Projeto>.Criar(itens, paginacao, total);
    }

    public Task<bool> PossuiTarefaEmAndamentoAsync(Guid projetoId, CancellationToken cancellationToken = default) =>
        (from tarefa in context.Tarefas
         join status in context.Statuses on tarefa.StatusId equals status.Id
         where tarefa.ProjetoId == projetoId && status.Categoria == Domain.Enums.CategoriaStatus.EmAndamento
         select tarefa).AnyAsync(cancellationToken);

    public void Adicionar(Projeto projeto) => context.Projetos.Add(projeto);
    public void Remover(Projeto projeto) => context.Projetos.Remove(projeto);
}
