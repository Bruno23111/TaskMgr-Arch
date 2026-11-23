using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.DTOs.Paginacao;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.UnitTests.Application.Falsos;

// Repositórios em memória com a mesma semântica dos repositórios EF Core da Infrastructure:
// registros excluídos logicamente ficam ocultos, exceto nas verificações de referência,
// que consideram também os excluídos (como o IgnoreQueryFilters das implementações reais).

internal sealed class StatusEmMemoria : IStatusRepository
{
    public List<StatusTarefa> Itens { get; } = [];

    public CategoriaStatus CategoriaDe(Guid statusId) => Itens.Single(status => status.Id == statusId).Categoria;

    public Task<StatusTarefa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.SingleOrDefault(status => status.Id == id));

    public Task<StatusTarefa?> ObterPendenteInicialAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.OrderBy(status => status.Codigo).FirstOrDefault(status => status.Categoria == CategoriaStatus.Pendente));

    public Task<IReadOnlyList<StatusTarefa>> ListarAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StatusTarefa>>(Itens.OrderBy(status => status.Codigo).ToArray());

    public Task<bool> CodigoEmUsoAsync(string codigo, Guid? ignorarStatusId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.Any(status => status.Codigo == codigo.ToUpperInvariant() && status.Id != ignorarStatusId));

    public void Adicionar(StatusTarefa status) => Itens.Add(status);

    public void Remover(StatusTarefa status) => Itens.Remove(status);
}

internal sealed class TarefasEmMemoria : ITarefaRepository
{
    public List<Tarefa> Itens { get; } = [];

    public IEnumerable<Tarefa> Visiveis => Itens.Where(tarefa => tarefa.ExcluidoEm is null);

    public Task<Tarefa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Visiveis.SingleOrDefault(tarefa => tarefa.Id == id));

    public Task<IReadOnlyList<Tarefa>> ListarVisiveisPorProjetoAsync(Guid projetoId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Tarefa>>(Visiveis.Where(tarefa => tarefa.ProjetoId == projetoId).ToArray());

    public Task<ResultadoPaginado<Tarefa>> ListarAsync(Guid? projetoId, RequisicaoPaginada paginacao, CancellationToken cancellationToken = default)
    {
        var filtradas = Visiveis.Where(tarefa => projetoId is null || tarefa.ProjetoId == projetoId).OrderBy(tarefa => tarefa.Titulo).ToArray();
        return Task.FromResult(Paginar(filtradas, paginacao));
    }

    public Task<bool> PossuiTarefasParaResponsavelAsync(Guid responsavelId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.Any(tarefa => tarefa.ResponsavelId == responsavelId));

    public Task<bool> PossuiTarefasVisiveisNaoConcluidasParaResponsavelAsync(Guid responsavelId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Visiveis.Any(tarefa => tarefa.ResponsavelId == responsavelId && tarefa.ConcluidaEm is null));

    public Task<bool> PossuiTarefasParaStatusAsync(Guid statusId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.Any(tarefa => tarefa.StatusId == statusId));

    public void Adicionar(Tarefa tarefa) => Itens.Add(tarefa);

    public void Remover(Tarefa tarefa) => Itens.Remove(tarefa);

    internal static ResultadoPaginado<T> Paginar<T>(IReadOnlyCollection<T> itens, RequisicaoPaginada paginacao) =>
        ResultadoPaginado<T>.Criar(itens.Skip(paginacao.Deslocamento).Take(paginacao.Tamanho).ToArray(), paginacao, itens.Count);
}

internal sealed class ProjetosEmMemoria(TarefasEmMemoria tarefas, StatusEmMemoria status) : IProjetoRepository
{
    public List<Projeto> Itens { get; } = [];

    private IEnumerable<Projeto> Visiveis => Itens.Where(projeto => projeto.ExcluidoEm is null);

    public Task<Projeto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Visiveis.SingleOrDefault(projeto => projeto.Id == id));

    public Task<bool> BloquearParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Visiveis.Any(projeto => projeto.Id == id));

    public Task<ResultadoPaginado<Projeto>> ListarAsync(RequisicaoPaginada paginacao, CancellationToken cancellationToken = default)
    {
        paginacao.Validar();
        return Task.FromResult(TarefasEmMemoria.Paginar(Visiveis.OrderBy(projeto => projeto.Nome).ToArray(), paginacao));
    }

    public Task<bool> PossuiTarefaEmAndamentoAsync(Guid projetoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(tarefas.Visiveis.Any(tarefa =>
            tarefa.ProjetoId == projetoId && status.CategoriaDe(tarefa.StatusId) == CategoriaStatus.EmAndamento));

    public void Adicionar(Projeto projeto) => Itens.Add(projeto);

    public void Remover(Projeto projeto) => Itens.Remove(projeto);
}

internal sealed class ResponsaveisEmMemoria : IResponsavelRepository
{
    public List<Responsavel> Itens { get; } = [];

    public Task<Responsavel?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.SingleOrDefault(responsavel => responsavel.Id == id));

    public Task<Responsavel?> ObterPorUsuarioIdAsync(string usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.SingleOrDefault(responsavel => responsavel.UsuarioId == usuarioId));

    public Task<bool> BloquearParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.Any(responsavel => responsavel.Id == id));

    public Task<IReadOnlyList<Responsavel>> ListarAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Responsavel>>(Itens.OrderBy(responsavel => responsavel.Nome).ToArray());

    public void Adicionar(Responsavel responsavel) => Itens.Add(responsavel);

    public void Remover(Responsavel responsavel) => Itens.Remove(responsavel);
}
