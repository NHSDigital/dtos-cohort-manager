namespace DataServices.Core;

public class MultipleRecordsFoundException : Exception
{
    public MultipleRecordsFoundException()
    {
    }
    public MultipleRecordsFoundException(string message)
        : base(message)
    {
    }

    public MultipleRecordsFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
