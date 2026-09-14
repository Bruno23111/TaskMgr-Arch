namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record AtualizarResponsavelConta(
    string Nome,
    string Email,
    string Papel);
