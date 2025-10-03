namespace NHS.CohortManager.SnapshotTests;

using System.ComponentModel.DataAnnotations;

// Todo: get connection strings from .env file
public class SnapshotTestsConfig
{
    public string DbConnectionString {get;set;}
    public string StorageConnectionString { get; set; }
    [Required]
    public string StorageContainerName { get; set; }
    [Required]
    public string AddFileName {get; set;}
    [Required]
    public string AmendFile1Name { get; set; }
    [Required]
    public string AmendFile2Name { get; set; }
    [Required]
    public int MaxRetries { get; set; }
    [Required]
    public int StartDelay { get; set; }
}
