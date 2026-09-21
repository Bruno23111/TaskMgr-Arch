using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Common;
using TaskManager.MVC.API.Contracts.Projects;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Models;
using System.Data;

namespace TaskManager.MVC.API.Services.Projects;

public sealed class ProjectService(TaskManagerMvcDbContext dbContext, CurrentUser currentUser)
{
    public async Task<PageResponse<ProjectResponse>> ListarAsync(
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1 || tamanho is < 1 or > 100)
        {
            throw InvalidData("Página deve ser maior que zero e tamanho deve estar entre 1 e 100.");
        }

        var query = dbContext.Projetos.AsNoTracking().OrderBy(project => project.Nome);
        var total = await query.CountAsync(cancellationToken);
        var offset = (long)(pagina - 1) * tamanho;
        var items = offset > int.MaxValue
            ? []
            : await query
                .Skip((int)offset)
                .Take(tamanho)
                .Select(project => new ProjectResponse(project.Id, project.Nome, project.Descricao, project.Versao))
                .ToListAsync(cancellationToken);

        return new PageResponse<ProjectResponse>(items, pagina, tamanho, total);
    }

    public Task<ProjectResponse?> ObterAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.Projetos
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => new ProjectResponse(project.Id, project.Nome, project.Descricao, project.Versao))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProjectResponse> CriarAsync(
        ProjectCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManager();
        var name = NormalizeName(request.Nome);
        var description = NormalizeDescription(request.Descricao);
        var project = new Projeto { Nome = name, Descricao = description };
        dbContext.Projetos.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(project);
    }

    public async Task<ProjectResponse> AtualizarAsync(
        Guid projectId,
        ProjectUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManager();
        RequestValidation.Version(request.Versao);
        var name = NormalizeName(request.Nome);
        var description = NormalizeDescription(request.Descricao);
        var project = await FindAsync(projectId, cancellationToken);
        EnsureVersion(project, request.Versao);
        project.Nome = name;
        project.Descricao = description;
        dbContext.Entry(project).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(project);
    }

    public async Task ExcluirAsync(
        Guid projectId,
        int version,
        CancellationToken cancellationToken = default)
    {
        EnsureManager();
        if (version < 1)
        {
            throw InvalidData("A versão deve ser maior que zero.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var project = await dbContext.ProjetoAsync(projectId, cancellationToken);
        if (project is null || project.ExcluidoEm is not null)
        {
            throw NotFound();
        }

        EnsureVersion(project, version);
        var hasInProgressTask = await (
            from task in dbContext.Tarefas
            join status in dbContext.Statuses on task.StatusId equals status.Id
            where task.ProjetoId == projectId && status.Categoria == CategoriaStatus.EmAndamento
            select task.Id).AnyAsync(cancellationToken);
        if (hasInProgressTask)
        {
            throw new ApiException(
                409,
                "PROJETO_COM_TAREFA_EM_ANDAMENTO",
                "Projeto com tarefa em andamento.",
                "O projeto possui ao menos uma tarefa em andamento.");
        }

        var deletedAt = DateTimeOffset.UtcNow;
        var tasks = await dbContext.Tarefas
            .Where(task => task.ProjetoId == projectId)
            .ToListAsync(cancellationToken);
        foreach (var task in tasks)
        {
            task.ExcluidoEm = deletedAt;
        }

        project.ExcluidoEm = deletedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Projeto> FindAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Projetos.SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken)
        ?? throw NotFound();

    private static void EnsureVersion(Projeto project, int version)
    {
        if (project.Versao != version)
        {
            throw new ApiException(409, "CONCORRENCIA", "Conflito de concorrência.", "O projeto foi alterado por outra operação.");
        }
    }

    private static string NormalizeName(string? name) =>
        RequestValidation.RequiredText(name, "nome", 100);

    private static string? NormalizeDescription(string? description) =>
        RequestValidation.OptionalText(description, "descricao", 255);

    private static ProjectResponse ToResponse(Projeto project) =>
        new(project.Id, project.Nome, project.Descricao, project.Versao);

    private static ApiException InvalidData(string detail) =>
        new(400, "DADOS_INVALIDOS", "Dados inválidos.", detail);

    private static ApiException NotFound() =>
        new(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "O projeto informado não existe.");
    private void EnsureManager()
    {
        if (!currentUser.IsManager)
            throw new ApiException(403, "ACESSO_NEGADO", "Acesso negado.", "A operaÃ§Ã£o exige o papel Gestor.");
    }
}
