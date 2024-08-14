namespace Common;

using Model;
using Microsoft.Azure.Functions.Worker.Http;

public interface IReadRulesFromBlobStorage
{
    Task<string> GetRulesFromBlob(string blobConnectionString, string blobContainerName, string blobNameJson);
}
