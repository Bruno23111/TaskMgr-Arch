namespace TaskManager.CleanArch.Application.Common;

public sealed class ProjetoComTarefaEmAndamentoException() : ApplicationException(
    "PROJETO_COM_TAREFA_EM_ANDAMENTO",
    "Não é possível excluir projeto com tarefa em andamento.");
