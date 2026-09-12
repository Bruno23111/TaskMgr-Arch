namespace TaskManager.CleanArch.Application.DTOs;

public sealed record TarefaDto(
    Guid Id,
    Guid ProjetoId,
    Guid StatusId,
    Guid? ResponsavelId,
    string Titulo,
    string? Descricao,
    DateTimeOffset? ConcluidaEm,
    int Versao);
