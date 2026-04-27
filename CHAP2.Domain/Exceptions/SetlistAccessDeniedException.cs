namespace CHAP2.Domain.Exceptions;

public class SetlistAccessDeniedException : DomainException
{
    public SetlistAccessDeniedException(Guid setlistId, string userId)
        : base($"User '{userId}' is not allowed to access setlist '{setlistId}'.")
    {
    }
}
