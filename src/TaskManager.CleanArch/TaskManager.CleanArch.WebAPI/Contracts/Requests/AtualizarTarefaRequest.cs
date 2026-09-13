namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarTarefaRequest(
    string Titulo, 
    string? Descricao, 
    int Versao
    );
