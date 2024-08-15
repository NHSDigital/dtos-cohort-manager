namespace Model;

public class BlobFile
{
    public BlobFile(byte[] bytes, string fileName)
    {
        Data = new MemoryStream(bytes);
        FileName = fileName;
    }
    public BlobFile(Stream stream, string fileName)
    {
        Data = stream;
        FileName = fileName;
    }

    public Stream Data {get; set; }
    public string FileName {get; set;}
}
