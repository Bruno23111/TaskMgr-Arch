using System.ComponentModel.DataAnnotations;

namespace TaskManager.MVC.API.Contracts.Tasks;

public sealed record TaskCreateRequest(
    Guid ProjetoId,
    [Required, MaxLength(100)] string Titulo,
    [MaxLength(255)] string? Descricao);

public sealed record TaskUpdateRequest(
    [Required, MaxLength(100)] string Titulo,
    [MaxLength(255)] string? Descricao,
    [Range(1, int.MaxValue)] int Versao);

public sealed record AssignResponsibleRequest(
    Guid ResponsavelId,
    [Range(1, int.MaxValue)] int Versao);

public sealed record ChangeTaskStatusRequest(
    Guid StatusId,
    [Range(1, int.MaxValue)] int Versao);

public sealed record TaskResponse(
    Guid Id,
    Guid ProjetoId,
    Guid StatusId,
    Guid? ResponsavelId,
    string Titulo,
    string? Descricao,
    DateTimeOffset? ConcluidaEm,
    int Versao);
