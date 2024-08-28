using ChoETL;
using System.Collections.Generic;

public static class ParquetHelperService
{
    public static List<string> ExtractNhsNumbersFromParquet(string filePath)
    {
        var nhsNumbers = new List<string>();
        using (var r = new ChoParquetReader(filePath))
        {
            foreach (dynamic rec in r)
            {
                nhsNumbers.Add(rec.NHS_NUMBER);
            }
        }
        return nhsNumbers;
    }
}