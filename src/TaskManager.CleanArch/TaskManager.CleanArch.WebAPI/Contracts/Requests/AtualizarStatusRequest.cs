using System.ComponentModel.DataAnnotations;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record AtualizarStatusRequest(
    [property: Required, MaxLength(60)] string Nome,
    int Versao);
