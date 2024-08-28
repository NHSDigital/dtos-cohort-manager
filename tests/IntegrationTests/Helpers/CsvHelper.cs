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

    public static List<Dictionary<string, string>> ReadCsv(string filePath)
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            var records = new List<Dictionary<string, string>>();
            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            while (csv.Read())
            {
                var record = new Dictionary<string, string>();
                foreach (var header in headers)
                {
                    record[header] = csv.GetField(header);
                }
                records.Add(record);
            }

            return records;
        }
    }
}