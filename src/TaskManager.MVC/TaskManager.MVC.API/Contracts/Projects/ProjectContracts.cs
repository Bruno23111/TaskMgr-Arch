using System.ComponentModel.DataAnnotations;

namespace TaskManager.MVC.API.Contracts.Projects;

public sealed record ProjectCreateRequest(
    [Required, MaxLength(100)] string Nome,
    [MaxLength(255)] string? Descricao);

public sealed record ProjectUpdateRequest(
    [Required, MaxLength(100)] string Nome,
    [MaxLength(255)] string? Descricao,
    [Range(1, int.MaxValue)] int Versao);

public sealed record ProjectResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    int Versao);
