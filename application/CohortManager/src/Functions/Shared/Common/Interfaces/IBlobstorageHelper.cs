namespace Common;

using Model;

public interface IBlobStorageHelper
{
    Task<bool> CopyFileAsync(string connectionString, string fileName, string containerName);

    Task<bool> UploadFileToBlobStorage(string connectionString, string containerName, BlobFile blobFile);
}
