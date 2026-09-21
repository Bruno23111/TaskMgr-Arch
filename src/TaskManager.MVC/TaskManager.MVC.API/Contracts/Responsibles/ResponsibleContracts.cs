using System.ComponentModel.DataAnnotations;

namespace TaskManager.MVC.API.Contracts.Responsibles;

public sealed record ResponsibleCreateRequest(
    [Required, MaxLength(100)] string Nome,
    [Required, EmailAddress] string Email,
    [Required] string Senha,
    [Required] string Papel);

public sealed record ResponsibleUpdateRequest(
    [Required, MaxLength(100)] string Nome,
    [Required, EmailAddress] string Email,
    [Required] string Papel,
    [Range(1, int.MaxValue)] int Versao);

public sealed record DeactivateResponsibleRequest([Range(1, int.MaxValue)] int Versao);

public sealed record ResponsibleResponse(
    Guid Id,
    string UsuarioId,
    string Nome,
    string Email,
    bool Ativo,
    string Papel,
    int Versao);
