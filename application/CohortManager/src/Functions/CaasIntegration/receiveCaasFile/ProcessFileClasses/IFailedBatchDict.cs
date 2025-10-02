public interface IFailedBatchDict
{
    void AddFailedBatchDataToDict(string FileName, int retryCount);
    bool ShouldRetryFile(string filename);
    int GetRetryCount(string fileName);
    bool HasFileFailedBefore(string fileName);
    void UpdateFileFailureCount(string fileName, int failureCount);
}