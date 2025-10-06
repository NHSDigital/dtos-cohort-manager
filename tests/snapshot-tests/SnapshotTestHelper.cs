namespace NHS.CohortManager.SnapshotTests;

using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Model;
using Newtonsoft.Json.Linq;
using ParquetSharp;
using DotNetEnv;
using Common;

public class SnapshotTestHelper
{
    private readonly SnapshotTestsConfig _config;
    private readonly ILogger<SnapshotTestHelper> _logger;

    public SnapshotTestHelper(SnapshotTestsConfig config)
    {
        _config = config;
        _logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<SnapshotTestHelper>();
    }

    public void UploadFileToStorage(string filename)
    {
        var blobServiceClient = new BlobServiceClient(_config.StorageConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_config.StorageContainerName);
        var blobClient = containerClient.GetBlobClient(_config.AddFileName);

        blobClient.UploadAsync(filename, overwrite: true).GetAwaiter().GetResult();
        _logger.LogInformation("Test file uploaded");
    }

    public void ClearDatabase(DataServicesContext dbContext, List<long?> nhsNumbers)
    {
        _logger.LogInformation("Clearing database");

        var exceptionRecords = dbContext.exceptionManagements
            .AsNoTracking()
            .Where(i => nhsNumbers.Select(i => i.ToString())
            .ToList()
            .Contains(i.NhsNumber));
        dbContext.exceptionManagements.RemoveRange(exceptionRecords);

        var cohortDistRecords = dbContext.cohortDistributions
            .AsNoTracking()
            .Where(i => nhsNumbers.Contains(i.NHSNumber));
        dbContext.cohortDistributions.RemoveRange(cohortDistRecords);

        var demographicRecords = dbContext.participantDemographics
            .AsNoTracking()
            .Where(i => nhsNumbers.Contains(i.NhsNumber));
        dbContext.participantDemographics.RemoveRange(demographicRecords);

        var managementRecords = dbContext.participantManagements
            .AsNoTracking()
            .Where(i => nhsNumbers.Contains(i.NHSNumber));
        dbContext.participantManagements.RemoveRange(managementRecords);

        dbContext.SaveChanges();
    }

    public static VerifySettings ConfigureVerify(string directoryName)
    {
        VerifySettings verifySettings = new();
        verifySettings.UseDirectory(directoryName);
        verifySettings.DontScrubDateTimes();
        verifySettings.DisableRequireUniquePrefix();
        verifySettings.IgnoreMembers<ExceptionManagement>(i => i.ExceptionDate, i => i.ErrorRecord, i => i.DateCreated, i => i.ExceptionId, i => i.DateResolved, i => i.RecordUpdatedDate);
        verifySettings.IgnoreMembers<ParticipantManagement>(i => i.ParticipantId, i => i.RecordInsertDateTime, i => i.RecordUpdateDateTime);
        verifySettings.IgnoreMembers<ParticipantDemographic>(i => i.ParticipantId, i => i.RecordInsertDateTime, i => i.RecordInsertDateTime);
        verifySettings.IgnoreMembers<CohortDistribution>(i => i.CohortDistributionId, i => i.ParticipantId, i => i.RecordInsertDateTime, i => i.RecordUpdateDateTime, i => i.ReasonForRemovalDate);

        verifySettings.ScrubLinesContaining("07888888888");
        verifySettings.ScrubLinesContaining("lambda_method");
        verifySettings.ScrubLinesContaining("RecordUpdatedDate");
        verifySettings.ScrubLinesWithReplace(line =>
        {
            var regex = new Regex(@"Participant \d+");
            line = regex.Replace(line, "Participant IGNORED");
            return line;
        });

        return verifySettings;
    }

    public async Task ValidateWithRetries(Func<Task> verifyFunction)
    {
        for (int i = 0; i < _config.MaxRetries; i++)
        {
            try
            {
                _logger.LogInformation($"Gathering test result: attempt number {i + 1}");
                await verifyFunction();
            }
            catch when (i < _config.MaxRetries - 1)
            {
                await Task.Delay(35000);
            }
        }
    }

    public List<long?> GetNhsNumbersFromFile()
    {
        var nhsNumbers = new List<long?>();

        using (var reader = new ParquetFileReader(_config.AddFileName))
        {
            using var rowGroupReader = reader.RowGroup(0);
            var columnReader = rowGroupReader.Column(3).LogicalReader<long?>();

            nhsNumbers = columnReader.ReadAll(rows: (int)rowGroupReader.MetaData.NumRows).ToList();
        }

        return nhsNumbers;
    }

    public static SnapshotTestsConfig GetConfig()
    {
        Env.Load(".env");

        List<string> configFiles = ["appsettings.json"];
        var config = ConfigurationExtension.GetConfiguration<SnapshotTestsConfig>(null, configFiles);

        string azuriteConnectionString = Environment.GetEnvironmentVariable("AZURITE_CONNECTION_STRING");
        string dbName = Environment.GetEnvironmentVariable("DB_NAME");
        string dbPassword = Environment.GetEnvironmentVariable("PASSWORD");

        config.StorageConnectionString = azuriteConnectionString.Replace("azurite", "127.0.0.1");
        config.DbConnectionString = $"Server=db,1433;Database={dbName};User Id=SA;Password={dbPassword};TrustServerCertificate=True";

        return config;
    }

}