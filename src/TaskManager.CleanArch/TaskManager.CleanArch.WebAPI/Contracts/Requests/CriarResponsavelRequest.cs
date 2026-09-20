namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record CriarResponsavelRequest(
    string Nome,
    string Email,
    string Senha,
    string Papel);
