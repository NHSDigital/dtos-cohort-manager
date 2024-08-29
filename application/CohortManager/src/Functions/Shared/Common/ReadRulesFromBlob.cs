namespace Common;

using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Model;

public class ReadRulesFromBlobStorage : IReadRulesFromBlobStorage
{

    private readonly ILogger<ReadRulesFromBlobStorage> _logger;
    public ReadRulesFromBlobStorage(ILogger<ReadRulesFromBlobStorage> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetRulesFromBlob(string blobConnectionString, string blobContainerName, string blobNameJson)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = containerClient.GetBlobClient(blobNameJson);

            // Download the blob content to a stream
            using (MemoryStream ms = new MemoryStream())
            {
                await blobClient.DownloadToAsync(ms);
                ms.Position = 0;
                using (StreamReader reader = new StreamReader(ms))
                {
                    string jsonContent = await reader.ReadToEndAsync();
                    return jsonContent;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error while getting rules from blob: {ex} {fileName}", ex.Message, blobNameJson);
            throw;
        }
    }

}

