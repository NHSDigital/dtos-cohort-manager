﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using dtos_cohort_manager_e2e_tests.Models;

namespace dtos_cohort_manager_e2e_tests.Contexts;

public class SmokeTestsContext
{
    public string FilePath { get; set; }

    public RecordTypesEnum RecordType { get; set; }

    public List<string>? NhsNumbers { get; set; }
}
