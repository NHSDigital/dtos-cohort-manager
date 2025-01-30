using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using dtos_cohort_manager_specflow.Models;

namespace dtos_cohort_manager_specflow.Contexts;

public class SmokeTestsContext
{
    public string FilePath { get; set; }

    public RecordTypesEnum RecordType { get; set; }

    public List<string>? NhsNumbers { get; set; }
}