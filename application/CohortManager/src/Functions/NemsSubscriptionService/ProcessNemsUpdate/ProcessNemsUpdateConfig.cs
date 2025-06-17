namespace NHS.Screening.ProcessNemsUpdate;

using System.ComponentModel.DataAnnotations;

public class ProcessNemsUpdateConfig
{
    [Required]
    public string RetrievePdsDemographicURL { get; set; }
}
