namespace Model;

public class ValidationExceptionLog
{
    public bool IsFatal { get; set; }
    public bool CreatedException { get; set; }

    public ValidationExceptionLog() { }

    public ValidationExceptionLog(ValidationExceptionLog a, ValidationExceptionLog b)
    {
        IsFatal = a.IsFatal || b.IsFatal;
        CreatedException = a.CreatedException || b.CreatedException;
    }
}
