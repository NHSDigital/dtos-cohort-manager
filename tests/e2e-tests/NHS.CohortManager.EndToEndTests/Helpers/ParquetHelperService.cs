namespace NHS.CohortManager.EndToEndTests.Helpers;

using System.Collections.Generic;
using System.Linq;
using ChoETL;



    public static class ParquetHelperService
    {
        public static List<string> ExtractNhsNumbersFromParquet(string filePath)
        {
            var nhsNumbers = new List<string>();
            using (var r = new ChoParquetReader<NHSRecord>(filePath))
            {
                nhsNumbers.AddRange(r.Select(rec => rec.NHS_NUMBER.ToString()));
            }
            return nhsNumbers;

        }
    }

    public class NHSRecord
    {
        public long NHS_NUMBER { get; set; }
    }


