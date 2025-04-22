namespace Model;
using Model.Enums;

public class NemsSubscriptionRequest
{
    public string NhsNumber { get; set; }
}

public class NemsSubscriptionResponse
{
    public string SubscriptionId { get; set; }
    public string NhsNumber { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
