using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Application.Abstractions.Services;

public interface IDomainEventDispatcher
{
    Task DespacharAsync(IReadOnlyCollection<IDomainEvent> eventos, CancellationToken cancellationToken = default);
}
