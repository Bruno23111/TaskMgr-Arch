using System.ComponentModel.DataAnnotations;

namespace TaskManager.MVC.API.Contracts.Status;

public sealed record StatusCreateRequest(
    [Required, MaxLength(20)] string Codigo,
    [Required, MaxLength(60)] string Nome,
    [Required] string Categoria);

public sealed record StatusUpdateRequest(
    [Required, MaxLength(60)] string Nome,
    [Range(1, int.MaxValue)] int Versao);

public sealed record StatusResponse(
    Guid Id,
    string Codigo,
    string Nome,
    string Categoria,
    bool Protegido,
    int Versao);
