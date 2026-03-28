namespace CHAP2.Domain.Events;

/// <summary>
/// Domain event raised when a chorus is updated
/// </summary>
public class ChorusUpdatedEvent : IDomainEvent
{
    public Guid ChorusId { get; }
    public string ChorusName { get; }
    public DateTime OccurredOn { get; }

    public ChorusUpdatedEvent(Guid chorusId, string chorusName)
    {
        ChorusId = chorusId;
        ChorusName = chorusName;
        OccurredOn = DateTime.UtcNow;
    }
}
