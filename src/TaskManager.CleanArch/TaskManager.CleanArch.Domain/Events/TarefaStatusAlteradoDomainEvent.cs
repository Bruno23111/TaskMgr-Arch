using TaskManager.CleanArch.Domain.Common;
using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.Domain.Events;

public sealed record TarefaStatusAlteradoDomainEvent(
    Guid TarefaId,
    Guid ProjetoId,
    Guid StatusAnteriorId,
    Guid NovoStatusId,
    CategoriaStatus CategoriaAnterior,
    CategoriaStatus NovaCategoria,
    DateTimeOffset OcorridoEm) : IDomainEvent;
