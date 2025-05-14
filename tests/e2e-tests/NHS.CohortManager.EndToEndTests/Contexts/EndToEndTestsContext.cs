namespace NHS.CohortManager.EndToEndTests.Contexts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using NHS.CohortManager.EndToEndTests.Models;



public class EndToEndTestsContext
{
    public string FilePath { get; set; }

    public RecordTypesEnum RecordType { get; set; }

    public List<string>? NhsNumbers { get; set; }
}
