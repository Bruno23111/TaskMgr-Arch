namespace TaskManager.CleanArch.Application.Common;

public sealed class RecursoNaoEncontradoException(string recurso) : ApplicationException(
    "RECURSO_NAO_ENCONTRADO",
    $"O recurso '{recurso}' não foi encontrado.");
