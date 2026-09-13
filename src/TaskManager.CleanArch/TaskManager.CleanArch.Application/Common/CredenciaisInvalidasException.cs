namespace TaskManager.CleanArch.Application.Common;

public sealed class CredenciaisInvalidasException()
    : ApplicationException("CREDENCIAIS_INVALIDAS", "E-mail ou senha inválidos.");
