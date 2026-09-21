using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Common;
using TaskManager.MVC.API.Contracts.Tasks;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Models;
using System.Data;

namespace TaskManager.MVC.API.Services.Tasks;

public sealed class TaskService(TaskManagerMvcDbContext dbContext, CurrentUser currentUser)
{
    public async Task<PageResponse<TaskResponse>> ListarAsync(
        int pagina,
        int tamanho,
        Guid? projectId,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pagina, tamanho);
        if (projectId.HasValue && !await dbContext.Projetos.AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            throw InvalidData("O projeto informado não existe.");
        }

        var query = dbContext.Tarefas.AsNoTracking();
        if (projectId.HasValue)
        {
            query = query.Where(task => task.ProjetoId == projectId);
        }

        var orderedQuery = query.OrderBy(task => task.Titulo);
        var total = await orderedQuery.CountAsync(cancellationToken);
        var offset = (long)(pagina - 1) * tamanho;
        var items = offset > int.MaxValue
            ? []
            : await orderedQuery
                .Skip((int)offset)
                .Take(tamanho)
                .Select(task => new TaskResponse(
                    task.Id,
                    task.ProjetoId,
                    task.StatusId,
                    task.ResponsavelId,
                    task.Titulo,
                    task.Descricao,
                    task.ConcluidaEm,
                    task.Versao))
                .ToListAsync(cancellationToken);
        return new PageResponse<TaskResponse>(items, pagina, tamanho, total);
    }

    public Task<TaskResponse?> ObterAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        dbContext.Tarefas
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => new TaskResponse(
                task.Id,
                task.ProjetoId,
                task.StatusId,
                task.ResponsavelId,
                task.Titulo,
                task.Descricao,
                task.ConcluidaEm,
                task.Versao))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TaskResponse> CriarAsync(
        TaskCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManager();
        var title = NormalizeTitle(request.Titulo);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var project = await dbContext.ProjetoAsync(request.ProjetoId, cancellationToken);
        if (project is null || project.ExcluidoEm is not null)
        {
            throw InvalidData("O projeto informado não existe.");
        }

        var initialStatusId = await dbContext.Statuses
            .Where(status => status.Protegido && status.Categoria == CategoriaStatus.Pendente)
            .Select(status => status.Id)
            .SingleAsync(cancellationToken);
        var task = new Tarefa
        {
            ProjetoId = project.Id,
            StatusId = initialStatusId,
            Titulo = title,
            Descricao = NormalizeDescription(request.Descricao)
        };
        dbContext.Tarefas.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task<TaskResponse> AtualizarAsync(
        Guid taskId,
        TaskUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await FindAsync(taskId, cancellationToken);
        await EnsureCanEditAsync(task, cancellationToken);
        EnsureVersion(task, request.Versao);
        await EnsureNotCompletedAsync(task.StatusId, cancellationToken);

        task.Titulo = NormalizeTitle(request.Titulo);
        task.Descricao = NormalizeDescription(request.Descricao);
        dbContext.Entry(task).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task ExcluirAsync(Guid taskId, int version, CancellationToken cancellationToken = default)
    {
        EnsureManager();
        if (version < 1)
        {
            throw InvalidData("A versão deve ser maior que zero.");
        }

        var task = await FindAsync(taskId, cancellationToken);
        EnsureVersion(task, version);
        await EnsureNotCompletedAsync(task.StatusId, cancellationToken);
        task.ExcluidoEm = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskResponse> AtribuirResponsavelAsync(
        Guid taskId,
        AssignResponsibleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureManager();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var responsible = await dbContext.ResponsavelAsync(request.ResponsavelId, cancellationToken);
        if (responsible is null)
        {
            throw InvalidData("O responsável informado não existe.");
        }

        var task = await FindAsync(taskId, cancellationToken);
        EnsureVersion(task, request.Versao);
        await EnsureNotCompletedAsync(task.StatusId, cancellationToken);

        var activeAccount = await dbContext.Users
            .Where(user => user.Id == responsible.UsuarioId)
            .Select(user => user.Ativo)
            .SingleAsync(cancellationToken);
        if (!responsible.Ativo || !activeAccount)
        {
            throw new ApiException(409, "RESPONSAVEL_INATIVO", "Responsável inativo.", "Não é possível atribuir uma tarefa a um responsável inativo.");
        }

        task.ResponsavelId = responsible.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task<TaskResponse> RemoverResponsavelAsync(
        Guid taskId,
        int version,
        CancellationToken cancellationToken = default)
    {
        EnsureManager();
        if (version < 1)
        {
            throw InvalidData("A versão deve ser maior que zero.");
        }

        var task = await FindAsync(taskId, cancellationToken);
        EnsureVersion(task, version);
        await EnsureNotCompletedAsync(task.StatusId, cancellationToken);
        task.ResponsavelId = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task<TaskResponse> AlterarStatusAsync(
        Guid taskId,
        ChangeTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var targetStatus = await dbContext.Statuses.SingleOrDefaultAsync(
            status => status.Id == request.StatusId,
            cancellationToken);
        if (targetStatus is null)
        {
            throw InvalidData("O status informado não existe.");
        }

        var task = await FindAsync(taskId, cancellationToken);
        await EnsureCanEditAsync(task, cancellationToken);
        EnsureVersion(task, request.Versao);

        var project = await dbContext.ProjetoAsync(task.ProjetoId, cancellationToken);
        if (project is null || project.ExcluidoEm is not null)
        {
            throw new ApiException(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "O projeto da tarefa não existe.");
        }

        if (task.StatusId == targetStatus.Id)
        {
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(task);
        }

        var currentStatus = await dbContext.Statuses.SingleAsync(status => status.Id == task.StatusId, cancellationToken);
        var transition = TaskStatusTransition.Evaluate(
            currentStatus.Categoria,
            targetStatus.Categoria,
            currentUser.IsManager);

        if (transition.RequiresActiveResponsible)
        {
            await EnsureActiveResponsibleAsync(task.ResponsavelId, cancellationToken);
        }

        if (transition.SetCompletedAt)
        {
            task.ConcluidaEm = DateTimeOffset.UtcNow;
        }
        else if (transition.ClearCompletedAt)
        {
            task.ConcluidaEm = null;
        }

        task.StatusId = targetStatus.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(task);
    }

    private async Task EnsureCanEditAsync(Tarefa task, CancellationToken cancellationToken)
    {
        if (currentUser.IsManager)
        {
            return;
        }

        var assignedUserId = task.ResponsavelId.HasValue
            ? await dbContext.Responsaveis
                .Where(responsible => responsible.Id == task.ResponsavelId)
                .Select(responsible => responsible.UsuarioId)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        if (assignedUserId != currentUser.Id)
        {
            throw new ApiException(403, "ACESSO_NEGADO", "Acesso negado.", "A tarefa não está atribuída ao usuário atual.");
        }
    }

    private async Task EnsureNotCompletedAsync(Guid statusId, CancellationToken cancellationToken)
    {
        var completed = await dbContext.Statuses
            .Where(status => status.Id == statusId)
            .Select(status => status.Categoria == CategoriaStatus.Concluida)
            .SingleAsync(cancellationToken);
        if (completed)
        {
            throw new ApiException(409, "TAREFA_CONCLUIDA", "Tarefa concluída.", "Reabra a tarefa antes de alterá-la ou excluí-la.");
        }
    }

    private async Task EnsureActiveResponsibleAsync(Guid? responsibleId, CancellationToken cancellationToken)
    {
        if (!responsibleId.HasValue)
        {
            throw ResponsibleRequired();
        }

        var responsible = await dbContext.ResponsavelAsync(responsibleId.Value, cancellationToken);
        if (responsible is null || !responsible.Ativo)
        {
            throw ResponsibleRequired();
        }

        var activeAccount = await dbContext.Users
            .Where(user => user.Id == responsible.UsuarioId)
            .Select(user => user.Ativo)
            .SingleOrDefaultAsync(cancellationToken);
        if (!activeAccount)
        {
            throw ResponsibleRequired();
        }
    }

    private async Task<Tarefa> FindAsync(Guid taskId, CancellationToken cancellationToken) =>
        await dbContext.Tarefas.SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken)
        ?? throw new ApiException(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "A tarefa informada não existe.");

    private static void EnsureVersion(Tarefa task, int version)
    {
        if (task.Versao != version)
        {
            throw new ApiException(409, "CONCORRENCIA", "Conflito de concorrência.", "A tarefa foi alterada por outra operação.");
        }
    }

    private static string NormalizeTitle(string title)
    {
        var normalized = title.Trim();
        if (normalized.Length == 0)
        {
            throw InvalidData("O título da tarefa é obrigatório.");
        }
        return normalized;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void ValidatePagination(int page, int size)
    {
        if (page < 1 || size is < 1 or > 100)
        {
            throw InvalidData("Página deve ser maior que zero e tamanho deve estar entre 1 e 100.");
        }
    }

    private static TaskResponse ToResponse(Tarefa task) => new(
        task.Id,
        task.ProjetoId,
        task.StatusId,
        task.ResponsavelId,
        task.Titulo,
        task.Descricao,
        task.ConcluidaEm,
        task.Versao);

    private static ApiException InvalidData(string detail) =>
        new(400, "DADOS_INVALIDOS", "Dados inválidos.", detail);

    private static ApiException ResponsibleRequired() =>
        new(409, "RESPONSAVEL_OBRIGATORIO", "Responsável obrigatório.", "A conclusão exige um responsável ativo.");
    private void EnsureManager()
    {
        if (!currentUser.IsManager)
            throw new ApiException(403, "ACESSO_NEGADO", "Acesso negado.", "A operaÃ§Ã£o exige o papel Gestor.");
    }
}
