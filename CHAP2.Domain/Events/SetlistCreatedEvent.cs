namespace CHAP2.Domain.Events;

public class SetlistCreatedEvent : IDomainEvent
{
    public Guid SetlistId { get; }
    public string OwnerId { get; }
    public string Name { get; }
    public DateTime OccurredOn { get; }

    public SetlistCreatedEvent(Guid setlistId, string ownerId, string name)
    {
        SetlistId = setlistId;
        OwnerId = ownerId;
        Name = name;
        OccurredOn = DateTime.UtcNow;
    }
}
