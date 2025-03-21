using NHS.CohortManager.SmokeTests.Config;
public class AzureSettings
{
    public bool IsCloudEnvironment { get; set; }
}

public class AppSettings
{
    public ConnectionStrings ConnectionStrings { get; set; }
    public AzureSettings AzureSettings { get; set; }
    public string? ManagedIdentityClientId { get; set; }
    public FilePaths FilePaths { get; set; }
    public string BlobContainerName { get; set; }
    public string AzureWebJobsStorage { get; set; }
}

public class ConnectionStrings
{
    public string DtOsDatabaseConnectionString { get; set; }
}
