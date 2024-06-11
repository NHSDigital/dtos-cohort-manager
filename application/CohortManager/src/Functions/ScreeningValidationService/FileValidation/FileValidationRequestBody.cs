namespace NHS.CohortManager.ScreeningValidationService;

public class FileValidationRequestBody
{
    public string ExceptionMessage { get; set; }
    public string FileName { get; set; }

    public FileValidationRequestBody(string exceptionMessage, string fileName)
    {
        ExceptionMessage = exceptionMessage;
        FileName = fileName;
    }
}
