using ChoETL;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NHS.CohortManager.EndToEndTests.Helpers
{
    public class ParquetHelperService
    {
        // Mapping between Parquet fields and DB columns
        private static readonly Dictionary<string, string> FieldMappings = new Dictionary<string, string>
        {
            { "NHS_NUMBER", "NHS_NUMBER" },
            { "SUPERSEDED_BY_NHS_NUMBER", "SUPERSEDED_NHS_NUMBER" },
            { "PRIMARY_CARE_PROVIDER", "PRIMARY_CARE_PROVIDER" },
            { "PRIMARY_CARE_EFFECTIVE_FROM_DATE", "PRIMARY_CARE_PROVIDER_FROM_DT" },
            { "NAME_PREFIX", "NAME_PREFIX" },
            { "GIVEN_NAME", "GIVEN_NAME" },
            { "OTHER_GIVEN_NAME", "OTHER_GIVEN_NAME" },
            { "FAMILY_NAME", "FAMILY_NAME" },
            { "PREVIOUS_FAMILY_NAME", "PREVIOUS_FAMILY_NAME" },
            { "DATE_OF_BIRTH", "DATE_OF_BIRTH" },
            { "GENDER", "GENDER" },
            { "ADDRESS_LINE_1", "ADDRESS_LINE_1" },
            { "ADDRESS_LINE_2", "ADDRESS_LINE_2" },
            { "ADDRESS_LINE_3", "ADDRESS_LINE_3" },
            { "ADDRESS_LINE_4", "ADDRESS_LINE_4" },
            { "ADDRESS_LINE_5", "ADDRESS_LINE_5" },
            { "POSTCODE", "POST_CODE" },
            { "ADDRESS_EFFECTIVE_FROM_DATE", "USUAL_ADDRESS_FROM_DT" },
            { "CURRENT_POSTING", "CURRENT_POSTING" },
            { "CURRENT_POSTING_EFFECTIVE_FROM_DATE", "CURRENT_POSTING_FROM_DT" },
            { "DATE_OF_DEATH", "DATE_OF_DEATH" },
            { "HOME_TELEPHONE_NUMBER", "TELEPHONE_NUMBER_HOME" },
            { "HOME_TELEPHONE_EFFECTIVE_FROM_DATE", "TELEPHONE_NUMBER_HOME_FROM_DT" },
            { "MOBILE_TELEPHONE_NUMBER", "TELEPHONE_NUMBER_MOB" },
            { "MOBILE_TELEPHONE_EFFECTIVE_FROM_DATE", "TELEPHONE_NUMBER_MOB_FROM_DT" },
            { "EMAIL_ADDRESS", "EMAIL_ADDRESS_HOME" },
            { "EMAIL_ADDRESS_EFFECTIVE_FROM_DATE", "EMAIL_ADDRESS_HOME_FROM_DT" },
            { "PREFERRED_LANGUAGE", "PREFERRED_LANGUAGE" },
            { "IS_INTERPRETER_REQUIRED", "INTERPRETER_REQUIRED" },
            { "REASON_FOR_REMOVAL", "REASON_FOR_REMOVAL" },
            { "REASON_FOR_REMOVAL_EFFECTIVE_FROM_DATE", "REASON_FOR_REMOVAL_FROM_DT" }
        };

        // Fields to exclude from comparison (auto-generated fields, etc.)
        private static readonly HashSet<string> ExcludeFields = new HashSet<string>
        {
            "BS_COHORT_DISTRIBUTION_ID",
            "PARTICIPANT_ID",
            "IS_EXTRACTED",
            "RECORD_INSERT_DATETIME",
            "RECORD_UPDATE_DATETIME",
            "REQUEST_ID"
        };

        /// <summary>
        /// Extract NHS numbers from a Parquet file
        /// </summary>
        /// <param name="filePath">Path to the Parquet file</param>
        /// <returns>List of NHS numbers</returns>
        public static List<string> ExtractNhsNumbersFromParquet(string filePath)
        {
            var nhsNumbers = new List<string>();
            using (var r = new ChoParquetReader<FullNHSRecord>(filePath))
            {
                nhsNumbers.AddRange(r.Select(rec => rec.NHS_NUMBER.ToString()));
            }
            return nhsNumbers;
        }


        public static async Task VerifyParquetValuesMatchDbValuesAsync(
     string filePath,
     SqlConnectionWithAuthentication sqlConnectionWithAuthentication,
     string tableName,
     ILogger logger)
        {
            // Define field mappings between Parquet and database
            var FieldMappings = new Dictionary<string, string>
    {
        { "NHS_NUMBER", "NHS_Number" },
        { "RECORD_TYPE", "RECORD_TYPE" },
        { "GIVEN_NAME", "GIVEN_NAME" },
        { "FAMILY_NAME", "FAMILY_NAME" },
        { "DATE_OF_BIRTH", "DATE_OF_BIRTH" },
        // Add other mappings as needed
    };

            // Define fields to exclude from comparison
            var ExcludeFields = new HashSet<string>
    {
        "CHANGE_TIME_STAMP",
        "SERIAL_CHANGE_NUMBER"
    };

            // Fields to ignore discrepancies
            var IgnoredFields = new HashSet<string>
    {
        "SUPERSEDED_BY_NHS_NUMBER",
        "IS_INTERPRETER_REQUIRED"
    };

            // Custom comparison logic for specific fields
            var CustomComparisons = new Dictionary<string, Func<object, object, bool>>
            {
                ["IS_INTERPRETER_REQUIRED"] = (parquetValue, dbValue) =>
                {
                    // Convert both to boolean representation
                    bool parquetBool = parquetValue is bool b ? b : Convert.ToBoolean(parquetValue);
                    bool dbBool = dbValue is int i ? i != 0 : Convert.ToBoolean(dbValue);
                    return parquetBool == dbBool;
                },
                ["SUPERSEDED_BY_NHS_NUMBER"] = (parquetValue, dbValue) =>
                {
                    // Treat null and 0 as equivalent
                    if (parquetValue == null && Convert.ToInt64(dbValue) == 0)
                        return true;
                    return false;
                }
            };

            logger.LogInformation("Validating Parquet values match database values in table {TableName}.", tableName);

            Func<Task> act = async () =>
            {
                // Read records from Parquet
                var parquetRecords = new List<FullNHSRecord>();
                using (var reader = new ChoParquetReader<FullNHSRecord>(filePath))
                {
                    parquetRecords.AddRange(reader);
                }

                logger.LogInformation("Read {Count} records from Parquet file", parquetRecords.Count);

                foreach (var record in parquetRecords)
                {
                    string nhsNumber = record.NHS_NUMBER.ToString();
                    logger.LogInformation("Comparing values for NHS number {NhsNumber}", nhsNumber);

                    // Get the corresponding record from the database using DatabaseValidationHelper
                    var dbRecord = await DatabaseValidationHelper.GetDatabaseRecordAsync(
                        sqlConnectionWithAuthentication,
                        tableName,
                        nhsNumber,
                        logger);

                    dbRecord.Should().NotBeNull($"Record for NHS number {nhsNumber} should exist in {tableName}");

                    // Compare fields
                    var discrepancies = new List<string>();
                    foreach (var mapping in FieldMappings)
                    {
                        string parquetField = mapping.Key;
                        string dbField = mapping.Value;

                        // Skip excluded fields
                        if (ExcludeFields.Contains(dbField))
                            continue;

                        // Get Parquet value using reflection
                        var parquetProperty = typeof(FullNHSRecord).GetProperty(parquetField);
                        if (parquetProperty == null)
                            continue;

                        object parquetValue = parquetProperty.GetValue(record);

                        // Skip if DB field doesn't exist
                        if (!dbRecord.ContainsKey(dbField))
                            continue;

                        object dbValue = dbRecord[dbField];

                        // Handle null values
                        if (parquetValue == null && dbValue == null)
                            continue;

                        // Check for ignored fields
                        if (IgnoredFields.Contains(parquetField))
                            continue;

                        // Perform custom comparison if available
                        if (CustomComparisons.TryGetValue(parquetField, out var customComparer))
                        {
                            if (customComparer(parquetValue, dbValue))
                                continue;
                        }

                        if (parquetValue == null && dbValue != null)
                        {
                            discrepancies.Add($"Field {parquetField}: Parquet value is null but DB value is {dbValue}");
                            continue;
                        }

                        if (parquetValue != null && dbValue == null)
                        {
                            discrepancies.Add($"Field {parquetField}: Parquet value is {parquetValue} but DB value is null");
                            continue;
                        }

                        // Compare based on type
                        bool valuesMatch = CompareValues(parquetField, parquetValue, dbValue);

                        if (!valuesMatch)
                        {
                            discrepancies.Add($"Field {parquetField}: Parquet value '{parquetValue}' doesn't match DB value '{dbValue}'");
                        }
                    }

                    // Assert no discrepancies
                    discrepancies.Should().BeEmpty($"Values for NHS number {nhsNumber} should match between Parquet and database. Discrepancies: {string.Join(", ", discrepancies)}");
                }
            };

            await act.Should().NotThrowAfterAsync(TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5));
            logger.LogInformation("Validation of Parquet values against database completed successfully.");
        }


        private static bool CompareValues(string fieldName, object parquetValue, object dbValue)
        {
            // Special handling for date fields
            if (fieldName.Contains("DATE") || fieldName.Contains("DOB") || fieldName == "DATE_OF_BIRTH")
            {
                // Try to parse dates according to the expected format
                if (parquetValue is string parquetDateStr)
                {
                    DateTime? parquetDate = ParseDateValue(parquetDateStr);

                    if (dbValue is DateTime dbDate)
                    {
                        if (parquetDate.HasValue)
                        {
                            // Compare dates without time component
                            return parquetDate.Value.Date == dbDate.Date;
                        }
                    }
                }
            }

            // Default string comparison for other fields
            return parquetValue.ToString().Trim() == dbValue.ToString().Trim();
        }


        private static DateTime? ParseDateValue(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return null;

            // Try YYYYMMDD
            if (dateStr.Length == 8 && long.TryParse(dateStr, out _))
            {
                // Use standard parsing instead of manual substring operations
                if (DateTime.TryParseExact(
                    dateStr,
                    "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime result))
                {
                    return result;
                }
            }


            if (dateStr.Length == 6 && long.TryParse(dateStr, out _))
            {
                if (DateTime.TryParseExact(
                    dateStr,
                    "yyyyMM",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime result))
                {
                    return result;
                }
            }

            if (dateStr.Length == 4 && int.TryParse(dateStr, out _))
            {
                if (DateTime.TryParseExact(
                    dateStr,
                    "yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime result))
                {
                    return result;
                }
            }


            if (DateTime.TryParse(dateStr, out DateTime standardResult))
            {
                return standardResult;
            }

            return null;
        }

        // Full record class mapping to all fields in the Parquet file
        public class FullNHSRecord
        {
            public string RECORD_TYPE { get; set; }
            public DateTime? CHANGE_TIME_STAMP { get; set; }
            public int SERIAL_CHANGE_NUMBER { get; set; }
            public long NHS_NUMBER { get; set; }
            public long? SUPERSEDED_BY_NHS_NUMBER { get; set; }
            public string PRIMARY_CARE_PROVIDER { get; set; }
            public string PRIMARY_CARE_EFFECTIVE_FROM_DATE { get; set; }
            public string CURRENT_POSTING { get; set; }
            public string CURRENT_POSTING_EFFECTIVE_FROM_DATE { get; set; }
            public string NAME_PREFIX { get; set; }
            public string GIVEN_NAME { get; set; }
            public string OTHER_GIVEN_NAME { get; set; }
            public string FAMILY_NAME { get; set; }
            public string PREVIOUS_FAMILY_NAME { get; set; }
            public string DATE_OF_BIRTH { get; set; }
            public int GENDER { get; set; }
            public string ADDRESS_LINE_1 { get; set; }
            public string ADDRESS_LINE_2 { get; set; }
            public string ADDRESS_LINE_3 { get; set; }
            public string ADDRESS_LINE_4 { get; set; }
            public string ADDRESS_LINE_5 { get; set; }
            public string POSTCODE { get; set; }
            public string PAF_KEY { get; set; }
            public string ADDRESS_EFFECTIVE_FROM_DATE { get; set; }
            public string REASON_FOR_REMOVAL { get; set; }
            public string REASON_FOR_REMOVAL_EFFECTIVE_FROM_DATE { get; set; }
            public string DATE_OF_DEATH { get; set; }
            public string DEATH_STATUS { get; set; }
            public string HOME_TELEPHONE_NUMBER { get; set; }
            public string HOME_TELEPHONE_EFFECTIVE_FROM_DATE { get; set; }
            public string MOBILE_TELEPHONE_NUMBER { get; set; }
            public string MOBILE_TELEPHONE_EFFECTIVE_FROM_DATE { get; set; }
            public string EMAIL_ADDRESS { get; set; }
            public string EMAIL_ADDRESS_EFFECTIVE_FROM_DATE { get; set; }
            public string PREFERRED_LANGUAGE { get; set; }
            public bool IS_INTERPRETER_REQUIRED { get; set; }
            public bool INVALID_FLAG { get; set; }
            public bool ELIGIBILITY { get; set; }
        }
    }
}


