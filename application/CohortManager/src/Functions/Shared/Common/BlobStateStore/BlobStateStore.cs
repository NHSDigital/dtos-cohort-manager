namespace Common;

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using NHS.Screening.ReceiveCaasFile;

public class BlobStateStore : IStateStore
{
    private readonly ILogger<BlobStateStore> _logger;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly BlobStateStoreConfig _config;

    public BlobStateStore(ILogger<BlobStateStore> logger, IBlobStorageHelper blobStorageHelper, IOptions<BlobStateStoreConfig> config)
    {
        _logger = logger;
        _blobStorageHelper = blobStorageHelper;
        _config = config.Value;
    }
    public async Task<T?> GetState<T>(string key)
    {
        var fileName = $"{key}.json";
        var state = await _blobStorageHelper.GetFileFromBlobStorage(_config.AzureWebJobsStorage, _config.StateBlobContainerName, fileName);
        if (state == null)
        {
            _logger.LogWarning("State File: {StateFileName} Doesn't exist", fileName);
            return default;
        }

        using (StreamReader reader = new StreamReader(state.Data))
        {
            state.Data.Seek(0, SeekOrigin.Begin);
            string jsonData = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(jsonData);
        }

    }

    public async Task<bool> SetState<T>(string key, T data)
    {
        var fileName = $"{key}.json";
        try
        {
            string jsonString = JsonSerializer.Serialize(data);
            using (var stream = GenerateStreamFromString(jsonString))
            {
                var blobFile = new BlobFile(stream, fileName);
                var result = await _blobStorageHelper.UploadFileToBlobStorage(_config.AzureWebJobsStorage, _config.StateBlobContainerName, blobFile, true);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable To set Config State");
            return false;
        }

    }
    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

}
