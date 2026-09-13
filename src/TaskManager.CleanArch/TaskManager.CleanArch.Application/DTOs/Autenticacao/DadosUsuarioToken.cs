namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record DadosUsuarioToken(
    string UsuarioId,
    string Email,
    IReadOnlyCollection<string> Papeis);
