namespace TaskManager.MVC.API.Contracts.Responsibles;

public sealed record ResponsibleCreateRequest(
    string Nome,
    string Email,
    string Senha,
    string Papel);

public sealed record ResponsibleUpdateRequest(
    string Nome,
    string Email,
    string Papel,
    int Versao);

public sealed record DeactivateResponsibleRequest(int Versao);

public sealed record ResponsibleResponse(
    Guid Id,
    string UsuarioId,
    string Nome,
    string Email,
    bool Ativo,
    string Papel,
    int Versao);
