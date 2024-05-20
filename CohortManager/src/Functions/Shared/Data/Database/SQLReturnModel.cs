namespace Data.Database;
using System.Diagnostics.CodeAnalysis;

public class SQLReturnModel
{
    public CommandType commandType { get; set; }
    public string SQL { get; set; } = null!;
    public Dictionary<string, object> parameters { get; set; } = null!;
}
