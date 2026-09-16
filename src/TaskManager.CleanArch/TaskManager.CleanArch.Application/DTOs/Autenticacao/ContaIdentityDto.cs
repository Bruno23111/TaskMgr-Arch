namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record ContaIdentityDto(
    string UsuarioId,
    string Email,
    bool Ativo,
    IReadOnlyList<string> Papeis);
