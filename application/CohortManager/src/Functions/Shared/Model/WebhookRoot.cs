namespace Model;

public class demographicFunctionResponse
{
    public string id { get; set; }
    public string purgeHistoryDeleteUri { get; set; }
    public string sendEventPostUri { get; set; }
    public string statusQueryGetUri { get; set; }
    public string terminatePostUri { get; set; }
    public string suspendPostUri { get; set; }
    public string resumePostUri { get; set; }
}

public class RuntimeStatus
{
    public string runtimeStatus { get; set; }
    public DateTime createdTime { get; set; }
    public DateTime lastUpdatedTime { get; set; }
}

public class WebhookResponse
{
    public string name { get; set; }
    public string instanceId { get; set; }
    public string runtimeStatus { get; set; }
    public string input { get; set; }
    public object customStatus { get; set; }
    public bool output { get; set; }
    public DateTime createdTime { get; set; }
    public DateTime lastUpdatedTime { get; set; }
}
