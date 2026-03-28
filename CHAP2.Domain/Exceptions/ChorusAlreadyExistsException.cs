namespace CHAP2.Domain.Exceptions;

public class ChorusAlreadyExistsException : DomainException
{
    public ChorusAlreadyExistsException(string name) : base($"A chorus with the name '{name}' already exists.")
    {
    }
}
