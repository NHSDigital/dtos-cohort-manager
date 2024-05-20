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
}
