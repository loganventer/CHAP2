namespace CHAP2.Domain.Events;

/// <summary>
/// Domain event raised when a chorus is created
/// </summary>
public class ChorusCreatedEvent : IDomainEvent
{
    public Guid ChorusId { get; }
    public string ChorusName { get; }
    public DateTime OccurredOn { get; }

    public ChorusCreatedEvent(Guid chorusId, string chorusName)
    {
        ChorusId = chorusId;
        ChorusName = chorusName;
        OccurredOn = DateTime.UtcNow;
    }
} 