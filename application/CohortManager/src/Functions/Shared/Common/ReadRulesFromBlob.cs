namespace Common;

using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Model;

public class ReadRulesFromBlobStorage : IReadRulesFromBlobStorage
{
    public async Task<string> GetRulesFromBlob(string blobConnectionString, string blobContainerName, string blobNameJson)
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

}

