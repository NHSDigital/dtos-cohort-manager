namespace NHS.Screening.FileValidation;

using System.ComponentModel.DataAnnotations;

public class FileValidationConfig
{
    [Required]
    public string caasfolder_STORAGE { get; set; }
    [Required]
    public string inboundBlobName { get; set; }
}