namespace Common;

public interface IBlobStorageHelper
{
    public Task<bool> CopyFileAsync(string ConnectionString, string FileName, string ContainerName);
}
