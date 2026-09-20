namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record LoginRequest(
    string Email,
    string Senha
    );
