using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

public static class CsvHelperService
{
    public static List<string> ExtractNhsNumbersFromCsv(string filePath)
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            var records = new List<string>();
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                records.Add(csv.GetField(3));
            }
            return records;
        }
    }
}