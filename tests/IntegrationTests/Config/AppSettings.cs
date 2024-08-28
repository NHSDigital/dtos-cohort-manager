public class AppSettings
{
    public ConnectionStrings ConnectionStrings { get; set; }
    public FilePaths FilePaths { get; set; }
    public string BlobContainerName { get; set; }
    public string AzureWebJobsStorage { get; set; }
}