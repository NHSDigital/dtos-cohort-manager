namespace Common;

using System.IO;

public interface IFileReader
{
    public StreamReader ReadStream(Stream stream);
}

public class FileReader : IFileReader
{
    public StreamReader ReadStream(Stream stream)
    {
        IFileReader fileReader = new FileReader();
        StreamReader reader = fileReader.ReadStream(stream);
        return reader;
    }

    public static string ReadJsonFileFromPath(string fileName)
    {
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string[] potentialDirectories =
        {
            Path.Combine(basePath, $"{fileName}"),
            Path.Combine(basePath, "..", "..", $"{fileName}"),
            Path.Combine(basePath, "TestUtils", "MockData", $"{fileName}"),
            Path.Combine(basePath, "..", "..", "..", "..", "..", "..", "tests", "TestUtils", "MockData", $"{fileName}"),
        };

        string filePath = potentialDirectories.Select(Path.GetFullPath).FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException($"File '{fileName}.json' not found in any of the specified locations.");

        return File.ReadAllText(filePath);
    }
}
