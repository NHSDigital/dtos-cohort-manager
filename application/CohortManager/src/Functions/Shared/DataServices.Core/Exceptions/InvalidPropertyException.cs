namespace DataServices.Core;

public class InvalidPropertyException : Exception
{
    public InvalidPropertyException()
    {
    }
    public InvalidPropertyException(string message)
        : base(message)
    {
    }

    public InvalidPropertyException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
