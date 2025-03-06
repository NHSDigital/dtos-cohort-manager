using System.ComponentModel.DataAnnotations;

namespace NHS.CohortManager.Tests.SnapshotTests;

public class SnapshotTestsConfig
{
    [Required]
    public string ConnectionString {get;set;}
}
