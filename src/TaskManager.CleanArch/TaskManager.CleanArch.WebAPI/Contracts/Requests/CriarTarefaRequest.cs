namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record CriarTarefaRequest(
    Guid ProjetoId, 
    string Titulo, 
    string? Descricao
    );
