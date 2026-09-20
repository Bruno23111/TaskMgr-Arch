namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record CriarProjetoRequest(
    string Nome,
    string? Descricao
    );

