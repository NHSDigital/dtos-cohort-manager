namespace NHS.Screening.ReceiveCaasFile;

using System.Text.RegularExpressions;

public class FileNameParser
{
    private const string _fileNameRegex = @"^(.+)_-_(\w+)\.parquet$";

    private readonly Match match;

    public FileNameParser(string fileName)
    {
        match = Regex.Match(fileName, _fileNameRegex, RegexOptions.IgnoreCase, matchTimeout: new TimeSpan(0, 0, 30));
    }

    public bool IsValid => match.Success;

    public string GetScreeningService()
    {
        Group g = match.Groups[2];
        return g.Captures[0].ToString();
    }

}
