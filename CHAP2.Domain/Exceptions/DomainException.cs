namespace CHAP2.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class ChorusNotFoundException : DomainException
{
    public ChorusNotFoundException(Guid id) : base($"Chorus with ID '{id}' was not found.")
    {
    }

    public ChorusNotFoundException(string name) : base($"Chorus with name '{name}' was not found.")
    {
    }
}

public class ChorusAlreadyExistsException : DomainException
{
    public ChorusAlreadyExistsException(string name) : base($"A chorus with the name '{name}' already exists.")
    {
    }
}

public class InvalidChorusDataException : DomainException
{
    public InvalidChorusDataException(string message) : base(message)
    {
    }
} 