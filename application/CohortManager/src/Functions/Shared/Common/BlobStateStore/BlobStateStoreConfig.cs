namespace Common;

using System.ComponentModel.DataAnnotations;

public class BlobStateStoreConfig
{
    [Required]
    public required string AzureWebJobsStorage { get; set; }
    [Required]
    public required string StateBlobContainerName { get; set; }
}
