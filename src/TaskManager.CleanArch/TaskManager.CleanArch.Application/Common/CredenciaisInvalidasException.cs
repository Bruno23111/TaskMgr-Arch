namespace TaskManager.CleanArch.Application.Common;

public sealed class CredenciaisInvalidasException()
    : ApplicationException("NAO_AUTENTICADO", "E-mail ou senha inválidos.");
