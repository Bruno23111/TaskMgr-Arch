using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Services.Tasks;

public readonly record struct TaskStatusTransition(bool RequiresActiveResponsible, bool SetCompletedAt, bool ClearCompletedAt)
{
    public static TaskStatusTransition Evaluate(CategoriaStatus current, CategoriaStatus target, bool isManager)
    {
        var allowed = (current, target) switch
        {
            (CategoriaStatus.Pendente, CategoriaStatus.Pendente) => true,
            (CategoriaStatus.Pendente, CategoriaStatus.EmAndamento) => true,
            (CategoriaStatus.EmAndamento, CategoriaStatus.Pendente) => true,
            (CategoriaStatus.EmAndamento, CategoriaStatus.EmAndamento) => true,
            (CategoriaStatus.EmAndamento, CategoriaStatus.Concluida) => true,
            (CategoriaStatus.Concluida, CategoriaStatus.Pendente) => true,
            (CategoriaStatus.Concluida, CategoriaStatus.Concluida) => true,
            _ => false
        };
        if (!allowed)
            throw new ApiException(409, "TRANSICAO_INVALIDA", "Transição inválida.", "A transição de status solicitada não é permitida.");
        if (current == CategoriaStatus.Concluida && target == CategoriaStatus.Pendente && !isManager)
            throw new ApiException(403, "ACESSO_NEGADO", "Acesso negado.", "Somente um Gestor pode reabrir uma tarefa.");

        return new(
            target == CategoriaStatus.Concluida,
            current != CategoriaStatus.Concluida && target == CategoriaStatus.Concluida,
            current == CategoriaStatus.Concluida && target == CategoriaStatus.Pendente);
    }
}
