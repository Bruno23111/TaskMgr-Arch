namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record CriarResponsavelConta(
    string Nome,
    string Email,
    string Senha,
    string Papel);
