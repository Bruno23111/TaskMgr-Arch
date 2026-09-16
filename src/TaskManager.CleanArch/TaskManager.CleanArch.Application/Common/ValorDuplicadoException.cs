namespace TaskManager.CleanArch.Application.Common;

public sealed class ValorDuplicadoException(string mensagem)
    : ApplicationException("VALOR_DUPLICADO", mensagem);
