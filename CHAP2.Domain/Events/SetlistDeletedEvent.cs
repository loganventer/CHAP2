namespace CHAP2.Domain.Events;

public class SetlistDeletedEvent : IDomainEvent
{
    public Guid SetlistId { get; }
    public string OwnerId { get; }
    public DateTime OccurredOn { get; }

    public SetlistDeletedEvent(Guid setlistId, string ownerId)
    {
        SetlistId = setlistId;
        OwnerId = ownerId;
        OccurredOn = DateTime.UtcNow;
    }
}
