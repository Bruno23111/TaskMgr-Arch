using System.ComponentModel.DataAnnotations;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarTarefaRequest(
    [property: Required, MaxLength(100)] string Titulo,
    [property: MaxLength(255)] string? Descricao,
    int Versao
    );
