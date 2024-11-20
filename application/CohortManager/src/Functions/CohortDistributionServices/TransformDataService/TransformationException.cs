public class TransformationException : Exception
{
    public TransformationException() : base() { }

    public TransformationException(string message) : base(message) { }

    public TransformationException(string message, Exception innerException) 
        : base(message, innerException) { }
}