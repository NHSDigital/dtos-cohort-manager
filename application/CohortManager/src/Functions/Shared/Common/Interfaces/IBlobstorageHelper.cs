namespace Common;

using Model;

public interface IBlobStorageHelper
{
    Task CopyFileToPoisonAsync(string connectionString, string fileName, string containerName);
    Task CopyFileToPoisonAsync(string connectionString, string fileName, string containerName, string poisonContainerName);

    Task<bool> UploadFileToBlobStorage(string connectionString, string containerName, BlobFile blobFile, bool overwrite = false);

    Task<BlobFile> GetFileFromBlobStorage(string connectionString, string containerName, string fileName);
}
