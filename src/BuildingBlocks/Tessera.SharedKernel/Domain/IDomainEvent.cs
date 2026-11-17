using MediatR;

namespace Tessera.SharedKernel.Domain;

/// <summary>
/// Marker for something meaningful that happened inside an aggregate.
/// Implements <see cref="INotification"/> (from the dependency-free MediatR.Contracts package)
/// so the application layer can react to it in-process.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }

    DateTime OccurredAtUtc { get; }
}

public abstract record DomainEvent(DateTime OccurredAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
