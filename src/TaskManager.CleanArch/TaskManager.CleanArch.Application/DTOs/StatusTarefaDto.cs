using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.Application.DTOs;

public sealed record StatusTarefaDto(
    Guid Id,
    string Codigo,
    string Nome,
    CategoriaStatus Categoria,
    bool Protegido,
    int Versao);
