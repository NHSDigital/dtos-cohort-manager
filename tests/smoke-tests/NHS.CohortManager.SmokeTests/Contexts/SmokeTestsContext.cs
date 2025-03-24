using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using NHS.CohortManager.SmokeTests.Models;

namespace NHS.CohortManager.SmokeTests.Contexts;

public class SmokeTestsContext
{
    public string FilePath { get; set; }

    public RecordTypesEnum RecordType { get; set; }

    public List<string>? NhsNumbers { get; set; }
}