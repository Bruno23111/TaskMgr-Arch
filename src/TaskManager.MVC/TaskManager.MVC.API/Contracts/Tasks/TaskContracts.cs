namespace TaskManager.MVC.API.Contracts.Tasks;

public sealed record TaskCreateRequest(
    Guid ProjetoId,
    string Titulo,
    string? Descricao);

public sealed record TaskUpdateRequest(
    string Titulo,
    string? Descricao,
    int Versao);

public sealed record AssignResponsibleRequest(
    Guid ResponsavelId,
    int Versao);

public sealed record ChangeTaskStatusRequest(
    Guid StatusId,
    int Versao);

public sealed record TaskResponse(
    Guid Id,
    Guid ProjetoId,
    Guid StatusId,
    Guid? ResponsavelId,
    string Titulo,
    string? Descricao,
    DateTimeOffset? ConcluidaEm,
    int Versao);
