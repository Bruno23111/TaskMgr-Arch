using System.ComponentModel.DataAnnotations;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarResponsavelRequest(
    [property: Required, MaxLength(100)] string Nome,
    [property: Required, EmailAddress] string Email,
    [property: Required] string Papel,
    int Versao);
