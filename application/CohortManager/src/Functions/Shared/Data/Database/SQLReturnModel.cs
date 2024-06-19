namespace Data.Database;

public class SQLReturnModel
{
    public CommandType CommandType { get; set; }
    public string SQL { get; set; } = null!;
    public Dictionary<string, object> Parameters { get; set; } = null!;
}
