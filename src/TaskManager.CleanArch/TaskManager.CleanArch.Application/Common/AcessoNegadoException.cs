namespace TaskManager.CleanArch.Application.Common;

public sealed class AcessoNegadoException() : ApplicationException(
    "ACESSO_NEGADO",
    "O usuário não tem permissão para esta operação.");
