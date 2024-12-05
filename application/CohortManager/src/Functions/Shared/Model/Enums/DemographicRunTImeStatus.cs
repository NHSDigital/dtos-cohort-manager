namespace Model.Enums;

public enum WorkflowStatus
{
    Unknown = -1,
    Running = 0,
    Completed = 1,
    ContinuedAsNew = 2,
    Failed = 3,
    Canceled = 4,
    Terminated = 5,
    Pending = 6
}
