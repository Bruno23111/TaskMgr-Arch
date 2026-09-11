namespace TaskManager.CleanArch.Domain.Common;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> LimparDomainEvents()
    {
        var eventos = _domainEvents.ToArray();
        _domainEvents.Clear();
        return eventos;
    }

    protected void AdicionarDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
