using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Status;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Services.Status;

public sealed class StatusService(TaskManagerMvcDbContext dbContext)
{
    public Task<List<StatusResponse>> ListarAsync(CancellationToken cancellationToken = default) =>
        dbContext.Statuses
            .AsNoTracking()
            .OrderBy(status => status.Codigo)
            .Select(status => new StatusResponse(
                status.Id,
                status.Codigo,
                status.Nome,
                status.Categoria == CategoriaStatus.Pendente
                    ? "PENDENTE"
                    : status.Categoria == CategoriaStatus.EmAndamento
                        ? "EM_ANDAMENTO"
                        : "CONCLUIDA",
                status.Protegido,
                status.Versao))
            .ToListAsync(cancellationToken);

    public Task<StatusResponse?> ObterAsync(Guid statusId, CancellationToken cancellationToken = default) =>
        dbContext.Statuses
            .AsNoTracking()
            .Where(status => status.Id == statusId)
            .Select(status => new StatusResponse(
                status.Id,
                status.Codigo,
                status.Nome,
                status.Categoria == CategoriaStatus.Pendente
                    ? "PENDENTE"
                    : status.Categoria == CategoriaStatus.EmAndamento
                        ? "EM_ANDAMENTO"
                        : "CONCLUIDA",
                status.Protegido,
                status.Versao))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<StatusResponse> CriarAsync(
        StatusCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var codigo = request.Codigo.Trim().ToUpperInvariant();
        var nome = request.Nome.Trim();
        var categoria = ParseCategoria(request.Categoria);
        ValidateText(codigo, nome);

        if (await dbContext.Statuses.AnyAsync(status => status.Codigo == codigo, cancellationToken))
        {
            throw DuplicateCode();
        }

        var status = new StatusTarefa
        {
            Codigo = codigo,
            Nome = nome,
            Categoria = categoria
        };

        dbContext.Statuses.Add(status);
        await SaveAsync(cancellationToken);
        return ToResponse(status);
    }

    public async Task<StatusResponse> AtualizarAsync(
        Guid statusId,
        StatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = await FindAsync(statusId, cancellationToken);
        EnsureVersion(status, request.Versao);

        var nome = request.Nome.Trim();
        ValidateText(status.Codigo, nome);
        status.Nome = nome;
        dbContext.Entry(status).State = EntityState.Modified;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(status);
    }

    public async Task ExcluirAsync(
        Guid statusId,
        int versao,
        CancellationToken cancellationToken = default)
    {
        if (versao < 1)
        {
            throw InvalidData("A versão deve ser maior que zero.");
        }

        var status = await FindAsync(statusId, cancellationToken);
        EnsureVersion(status, versao);

        if (status.Protegido)
        {
            throw new ApiException(409, "REGISTRO_PROTEGIDO", "Registro protegido.", "Um status inicial não pode ser excluído.");
        }

        var referenced = await dbContext.Tarefas
            .IgnoreQueryFilters()
            .AnyAsync(tarefa => tarefa.StatusId == statusId, cancellationToken);
        if (referenced)
        {
            throw new ApiException(409, "REGISTRO_EM_USO", "Registro em uso.", "O status está vinculado a uma ou mais tarefas.");
        }

        dbContext.Statuses.Remove(status);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<StatusTarefa> FindAsync(Guid statusId, CancellationToken cancellationToken) =>
        await dbContext.Statuses.SingleOrDefaultAsync(status => status.Id == statusId, cancellationToken)
        ?? throw new ApiException(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "O status informado não existe.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw DuplicateCode();
        }
    }

    private static CategoriaStatus ParseCategoria(string categoria) => categoria.Trim().ToUpperInvariant() switch
    {
        "PENDENTE" => CategoriaStatus.Pendente,
        "EM_ANDAMENTO" => CategoriaStatus.EmAndamento,
        "CONCLUIDA" => CategoriaStatus.Concluida,
        _ => throw InvalidData("A categoria deve ser PENDENTE, EM_ANDAMENTO ou CONCLUIDA.")
    };

    private static void EnsureVersion(StatusTarefa status, int version)
    {
        if (version != status.Versao)
        {
            throw new ApiException(409, "CONCORRENCIA", "Conflito de concorrência.", "O status foi alterado por outra operação.");
        }
    }

    private static void ValidateText(string codigo, string nome)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nome))
        {
            throw InvalidData("Código e nome são obrigatórios.");
        }
    }

    private static ApiException DuplicateCode() =>
        new(409, "VALOR_DUPLICADO", "Valor duplicado.", "Já existe um status com o código informado.");

    private static ApiException InvalidData(string detail) =>
        new(400, "DADOS_INVALIDOS", "Dados inválidos.", detail);

    private static StatusResponse ToResponse(StatusTarefa status) => new(
        status.Id,
        status.Codigo,
        status.Nome,
        status.Categoria switch
        {
            CategoriaStatus.Pendente => "PENDENTE",
            CategoriaStatus.EmAndamento => "EM_ANDAMENTO",
            CategoriaStatus.Concluida => "CONCLUIDA",
            _ => throw new InvalidOperationException("Categoria de status inválida.")
        },
        status.Protegido,
        status.Versao);
}
