namespace CHAP2.Domain.Events;

/// <summary>
/// Domain event raised when a chorus is deleted
/// </summary>
public class ChorusDeletedEvent : IDomainEvent
{
    public Guid ChorusId { get; }
    public string ChorusName { get; }
    public DateTime OccurredOn { get; }

    public ChorusDeletedEvent(Guid chorusId, string chorusName)
    {
        ChorusId = chorusId;
        ChorusName = chorusName;
        OccurredOn = DateTime.UtcNow;
    }
}
