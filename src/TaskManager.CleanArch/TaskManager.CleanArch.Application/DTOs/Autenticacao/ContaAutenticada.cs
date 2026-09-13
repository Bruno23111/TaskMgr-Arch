namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record ContaAutenticada(
    string UsuarioId,
    string Email,
    IReadOnlyCollection<string> Papeis);
