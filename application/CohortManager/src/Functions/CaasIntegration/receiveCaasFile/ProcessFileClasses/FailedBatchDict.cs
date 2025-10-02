using Common;
using Microsoft.Azure.Functions.Worker;

public class FailedBatchDict : IFailedBatchDict
{
    private Dictionary<string, int> RetryDictionary;

    private static readonly int RetryCount = 3;
    public FailedBatchDict()
    {
        RetryDictionary = new Dictionary<string, int>();
    }

    public void AddFailedBatchDataToDict(string FileName, int retryCount)
    {
        RetryDictionary.Add(FileName, retryCount);
    }

    public int GetRetryCount(string fileName)
    {
        if (RetryDictionary.TryGetValue(fileName, out int fileRetryCount))
        {
            return fileRetryCount;
        }
        return 0;
    }

    public bool HasFileFailedBefore(string fileName)
    {
        return RetryDictionary.ContainsKey(fileName);
    }

    public void UpdateFileFailureCount(string fileName, int failureCount)
    {
        RetryDictionary[fileName] = failureCount;
    }


    public bool ShouldRetryFile(string filename)
    {
        if (RetryDictionary.Count < 1)
        {
            return true;
        }

        if (RetryDictionary.TryGetValue(filename, out int fileRetryCount))
        {
            if (fileRetryCount < RetryCount)
            {
                return true;
            }
            RetryDictionary.Remove(filename);
            return false;
        }

        return true;
    }




}