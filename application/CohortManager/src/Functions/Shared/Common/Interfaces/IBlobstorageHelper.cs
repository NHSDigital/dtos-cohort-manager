namespace Common;

using Model;

public interface IBlobStorageHelper
{
    Task CopyFileToPoisonAsync(Uri serviceUri, string fileName, string containerName);
    Task CopyFileToPoisonAsync(Uri serviceUri, string fileName, string containerName, string poisonContainerName, bool addTimestamp = false);

    Task<bool> UploadFileToBlobStorage(Uri serviceUri, string containerName, BlobFile blobFile, bool overwrite = false);

    Task<BlobFile> GetFileFromBlobStorage(Uri serviceUri, string containerName, string fileName);
}
