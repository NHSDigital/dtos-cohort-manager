namespace Common;
/// <summary>
/// IStateStore is an interface used to store basic key value state for holding state for an azure function
/// </summary>
public interface IStateStore
{
    /// <summary>
    /// Gets State for a given key
    /// </summary>
    /// <typeparam name="T"> Data type to be returned for a given state item</typeparam>
    /// <param name="key"></param>
    /// <returns>Data Held within that state</returns>
    public Task<T?> GetState<T>(string key);

    /// <summary>
    /// Sets the State for a given key
    /// </summary>
    /// <typeparam name="T"> Data type to be returned for a given state item<</typeparam>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <returns>Return a bool confirming the state was set</returns>
    public Task<bool> SetState<T>(string key, T data);

}
