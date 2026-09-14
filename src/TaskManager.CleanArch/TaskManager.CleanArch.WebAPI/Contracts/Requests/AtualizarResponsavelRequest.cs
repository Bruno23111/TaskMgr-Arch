namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

// Contrato HTTP: não deve ser reutilizado pela camada Application.
public sealed record AtualizarResponsavelRequest(
    string Nome,
    string Email);
