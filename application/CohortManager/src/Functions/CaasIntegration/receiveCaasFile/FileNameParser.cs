namespace NHS.Screening.ReceiveCaasFile;


using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

public class FileNameParser
{
    private const string _fileNameRegex = @"^.*_-_(\w{1,})_\d{14}_n([1-9]\d*|0)\.csv$";

    private readonly Match match;

    public FileNameParser(string fileName)
    {
        match = Regex.Match(fileName,_fileNameRegex,RegexOptions.IgnoreCase,matchTimeout: new TimeSpan(0,0,30));
    }

    public bool IsValid =>  match.Success;

    public int? FileCount(){
        Group g = match.Groups[2];


        if(int.TryParse(g.Captures[0].ToString(), out var numberOfRecords))
        {
            return numberOfRecords;
        }

        return null;
    }

    public string GetScreeningService(){
        Group g = match.Groups[1];
        return g.Captures[0].ToString();
    }

}
