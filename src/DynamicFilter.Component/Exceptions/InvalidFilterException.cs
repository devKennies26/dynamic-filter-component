namespace DynamicFilter.Component.Exceptions;

public class InvalidFilterException : ArgumentException
{
    public InvalidFilterException(string message) : base(message)
    {
    }
}