using CHAP2.Domain.Entities;
using CHAP2.Domain.Events;

namespace CHAP2.Application.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
} 