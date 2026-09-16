namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record CriarContaIdentity(
    string Email,
    string Senha,
    string Papel);
