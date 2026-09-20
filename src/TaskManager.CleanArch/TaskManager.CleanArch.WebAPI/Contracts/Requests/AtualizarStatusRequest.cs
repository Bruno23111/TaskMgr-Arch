namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarStatusRequest(
    string Nome,
    int Versao);
