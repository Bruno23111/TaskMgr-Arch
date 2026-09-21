namespace TaskManager.MVC.API.Contracts.Common;

public sealed record PageResponse<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int Tamanho,
    int Total);
