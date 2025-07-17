namespace Common;

public interface IStateStore
{
    public Task<T?> GetState<T>(string key);

    public Task<bool> SetState<T>(string key, T data);

}
