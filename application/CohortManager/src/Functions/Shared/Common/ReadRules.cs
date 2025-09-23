namespace Common;

using System.Threading.Tasks;
using Interfaces;
using Microsoft.Extensions.Logging;

public class ReadRules : IReadRules
{

    private readonly ILogger<ReadRules> _logger;
    public ReadRules(ILogger<ReadRules> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetRulesFromDirectory(string jsonFileName)
    {
        try
        {
            // Get the path to the current directory where the executable is running (usually bin\Debug or bin\Release)
            var currentDirectory = Directory.GetCurrentDirectory();

            // Construct the path to the JSON file (assuming it was copied to the output directory)
            var filePath = Path.Combine(currentDirectory, jsonFileName);
             _logger.LogInformation("file path: {FilePath}", filePath);

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return string.Empty;
            }

            // Read the JSON file content as a string
            string jsonContent = await File.ReadAllTextAsync(filePath);
            return jsonContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error while getting rules from directory: {Ex} {FileName}", ex.Message, jsonFileName);
            return string.Empty;
        }
    }

}
