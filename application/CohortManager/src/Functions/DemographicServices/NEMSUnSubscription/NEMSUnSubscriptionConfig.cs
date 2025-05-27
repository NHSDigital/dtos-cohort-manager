namespace NHS.Screening.NEMSUnSubscription;

using System.ComponentModel.DataAnnotations;

public class NEMSUnSubscriptionConfig
{
    [Required]
    public string NhsNumber { get; set; }
    public string NemsDeleteEndpoint { get; set; }
}
