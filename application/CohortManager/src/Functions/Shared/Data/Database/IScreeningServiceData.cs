namespace Data.Database;

using Model;

public interface IScreeningServiceData
{
    ScreeningService GetScreeningServiceByAcronym(string screeningAcronym);
    ScreeningService GetScreeningServiceByWorkflowId(string WorkflowID);
}
