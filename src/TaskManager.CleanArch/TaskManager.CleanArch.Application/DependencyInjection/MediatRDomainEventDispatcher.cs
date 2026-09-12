using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Application.DependencyInjection;

public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DespacharAsync(
        IReadOnlyCollection<IDomainEvent> eventos,
        CancellationToken cancellationToken = default)
    {
        foreach (var evento in eventos)
            await publisher.Publish(
                new NotificacaoDeEventoDeDominio(evento),
                cancellationToken);
    }
}

public sealed record NotificacaoDeEventoDeDominio(IDomainEvent Evento) : INotification;
