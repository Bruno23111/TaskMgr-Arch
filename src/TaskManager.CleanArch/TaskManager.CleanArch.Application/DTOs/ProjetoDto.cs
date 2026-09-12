namespace TaskManager.CleanArch.Application.DTOs;

public sealed record ProjetoDto(
    Guid Id, 
    string Nome, 
    string? Descricao, 
    int Versao);
