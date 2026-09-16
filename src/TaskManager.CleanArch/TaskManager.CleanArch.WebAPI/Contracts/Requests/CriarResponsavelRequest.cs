using System.ComponentModel.DataAnnotations;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record CriarResponsavelRequest(
    [property: Required, MaxLength(100)] string Nome,
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(8)] string Senha,
    [property: Required] string Papel);
