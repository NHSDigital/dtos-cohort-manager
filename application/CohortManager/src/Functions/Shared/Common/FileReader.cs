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
        Path.Combine(basePath, $"{fileName}.json"),
        Path.Combine(basePath, "..", "..", $"{fileName}.json"),
        Path.Combine(basePath, "TestUtils", "MockData", $"{fileName}.json"),
        Path.Combine(basePath, "..", "..", "..", "..", "..", "..", "tests", "TestUtils", "MockData", $"{fileName}.json"),
    };

        string filePath = potentialDirectories
            .Select(Path.GetFullPath)
            .FirstOrDefault(File.Exists);

        if (filePath == null)
        {
            throw new FileNotFoundException($"File '{fileName}.json' not found in any of the specified locations.");
        }

        return File.ReadAllText(filePath);
    }
}
