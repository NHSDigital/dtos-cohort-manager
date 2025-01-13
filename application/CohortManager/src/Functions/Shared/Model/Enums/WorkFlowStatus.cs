namespace Model.Enums;


/// <summary>
/// this enum is used to get the status of any given status response from the demographic durable function
/// </summary>
public enum WorkFlowStatus
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
