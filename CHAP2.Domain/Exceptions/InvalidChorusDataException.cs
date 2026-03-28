namespace CHAP2.Domain.Exceptions;

public class InvalidChorusDataException : DomainException
{
    public InvalidChorusDataException(string message) : base(message)
    {
    }
}
