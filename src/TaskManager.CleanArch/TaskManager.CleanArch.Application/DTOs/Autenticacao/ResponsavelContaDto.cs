namespace TaskManager.CleanArch.Application.DTOs.Autenticacao;

public sealed record ResponsavelContaDto(
    Guid Id,
    string UsuarioId,
    string Nome,
    string Email,
    string Papel,
    bool Ativo,
    int Versao);
