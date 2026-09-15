namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarProjetoRequest(string Nome, string? Descricao, int Versao);
