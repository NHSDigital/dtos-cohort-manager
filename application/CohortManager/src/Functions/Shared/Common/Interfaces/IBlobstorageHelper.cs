namespace Common;

public interface IBlobStorageHelper
{
    public Task<bool> CopyFileAsync(string connectionString, string fileName, string containerName);
}
