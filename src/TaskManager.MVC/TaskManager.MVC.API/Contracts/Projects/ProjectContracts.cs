namespace TaskManager.MVC.API.Contracts.Projects;

public sealed record ProjectCreateRequest(
    string Nome,
    string? Descricao);

public sealed record ProjectUpdateRequest(
    string Nome,
    string? Descricao,
    int Versao);

public sealed record ProjectResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    int Versao);
