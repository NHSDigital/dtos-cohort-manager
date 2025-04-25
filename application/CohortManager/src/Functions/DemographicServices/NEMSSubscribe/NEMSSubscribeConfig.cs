namespace NHS.Screening.NEMSSubscribe;

using System.ComponentModel.DataAnnotations;

public class NEMSSubscribeConfig
{
    [Required]
    public string NEMS_FHIR_ENDPOINT { get; set; }
    public string SPINE_ACCESS_TOKEN { get; set; }
    public string FROM_ASID { get; set; }
    public string TO_ASID { get; set; }
    public string Subscription_Profile { get; set; }
    public string Subscription_Criteria { get; set; }
    public string CALLBACK_ENDPOINT { get; set; }
    public string CALLBACK_AUTH_TOKEN { get; set; }
    public string RetrievePdsDemographicURL { get; set; }
    public string ExceptionFunctionURL { get; set; }
    public string ParticipantDemographicDataServiceURL { get; set; }
}
