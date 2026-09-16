using System.ComponentModel.DataAnnotations;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Senha
    );
