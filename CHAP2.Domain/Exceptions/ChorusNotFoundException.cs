namespace CHAP2.Domain.Exceptions;

public class ChorusNotFoundException : DomainException
{
    public ChorusNotFoundException(Guid id) : base($"Chorus with ID '{id}' was not found.")
    {
    }

    public ChorusNotFoundException(string name) : base($"Chorus with name '{name}' was not found.")
    {
    }
}
