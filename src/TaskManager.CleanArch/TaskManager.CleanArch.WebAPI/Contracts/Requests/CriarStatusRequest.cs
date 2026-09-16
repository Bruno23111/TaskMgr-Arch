using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.WebAPI.Contracts.Requests;

public sealed record CriarStatusRequest(string Codigo, string Nome, CategoriaStatus Categoria);
