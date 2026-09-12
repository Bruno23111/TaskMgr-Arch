namespace TaskManager.CleanArch.Application.Common;

public sealed class ConflitoDeConcorrenciaException() : ApplicationException(
    "CONCORRENCIA",
    "A versão informada não corresponde ao estado atual do recurso.");
