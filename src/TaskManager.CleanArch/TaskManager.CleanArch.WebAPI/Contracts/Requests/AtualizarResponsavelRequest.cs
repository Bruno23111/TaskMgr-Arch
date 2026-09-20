namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarResponsavelRequest(
    string Nome,
    string Email,
    string Papel,
    int Versao);
