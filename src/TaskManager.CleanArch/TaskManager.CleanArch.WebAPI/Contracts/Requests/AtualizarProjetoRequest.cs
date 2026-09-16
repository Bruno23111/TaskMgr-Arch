using System.ComponentModel.DataAnnotations;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarProjetoRequest(
    [property: Required, MaxLength(100)] string Nome,
    [property: MaxLength(255)] string? Descricao,
    int Versao);
