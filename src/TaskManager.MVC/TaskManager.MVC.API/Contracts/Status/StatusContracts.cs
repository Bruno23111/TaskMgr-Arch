namespace TaskManager.MVC.API.Contracts.Status;

public sealed record StatusCreateRequest(
    string Codigo,
    string Nome,
    string Categoria);

public sealed record StatusUpdateRequest(
    string Nome,
    int Versao);

public sealed record StatusResponse(
    Guid Id,
    string Codigo,
    string Nome,
    string Categoria,
    bool Protegido,
    int Versao);
