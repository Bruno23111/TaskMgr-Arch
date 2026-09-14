namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

// Dados que um Gestor pode alterar no perfil e na conta de um responsável.
public sealed record AtualizarResponsavelConta(
    string Nome,
    string Email);
