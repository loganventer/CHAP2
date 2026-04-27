namespace CHAP2.Domain.Exceptions;

public class SetlistNotFoundException : DomainException
{
    public SetlistNotFoundException(Guid id) : base($"Setlist with ID '{id}' was not found.")
    {
    }
}
