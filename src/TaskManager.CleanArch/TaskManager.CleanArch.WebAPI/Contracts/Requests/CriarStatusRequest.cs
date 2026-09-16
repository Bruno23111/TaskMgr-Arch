using System.ComponentModel.DataAnnotations;
using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record CriarStatusRequest(
    [property: Required, MaxLength(20)] string Codigo,
    [property: Required, MaxLength(60)] string Nome,
    [property: Required] CategoriaStatus? Categoria);
