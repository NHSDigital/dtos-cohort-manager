using System.Data.Common;
using System.Runtime.CompilerServices;
using DataServices.Database;
using DataServices.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Model;
using SQLitePCL;

namespace ComponentTests;

public class TestDatabase : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<DataServicesContext> _contextOptions;
    public TestDatabase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<DataServicesContext>()
            .UseSqlite(_connection)
            .Options;


    }

    public async Task SeedDatabase()
    {

        var factory = LoggerFactory.Create(builder => {
            builder.AddConsole();
        });
        using var context = new DataServicesContext(_contextOptions);

        await context.Database.EnsureCreatedAsync();

        var logger = factory.CreateLogger<SeedDataLoader>();
        var seedDataLoader = new SeedDataLoader(logger,context);
        try
        {
            await seedDataLoader.LoadData<BsoOrganisation>("./SeedData/BsoOrganisation.json", "BSO_ORGANISATIONS", false);
            await seedDataLoader.LoadData<BsSelectGpPractice>("./SeedData/BsSelectGpPractice.json", "BS_SELECT_GP_PRACTICE_LKP", false);
            await seedDataLoader.LoadData<BsSelectOutCode>("./SeedData/BsSelectOutCode.json", "BS_SELECT_OUTCODE_MAPPING_LKP", false);
            await seedDataLoader.LoadData<CurrentPosting>("./SeedData/CurrentPosting.json", "CURRENT_POSTING_LKP", false);
            await seedDataLoader.LoadData<ExcludedSMULookup>("./SeedData/ExcludedSMULookup.json", "EXCLUDED_SMU_LKP", false);
            await seedDataLoader.LoadData<GenderMaster>("./SeedData/GenderMaster.json", "GENDER_MASTER", false);
            await seedDataLoader.LoadData<GeneCodeLkp>("./SeedData/GeneCodeLkp.json", "GENE_CODE_LKP", false);
            await seedDataLoader.LoadData<HigherRiskReferralReasonLkp>("./SeedData/HigherRiskReferralReasonLkp.json", "HIGHER_RISK_REFERRAL_REASON_LKP", false);
            await seedDataLoader.LoadData<LanguageCode>("./SeedData/LanguageCode.json", "LANGUAGE_CODES", false);
            await seedDataLoader.LoadData<ScreeningLkp>("./SeedData/ScreeningLkp.json", "SCREENING_LKP", false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert SeedData");
        }
    }

    public DataServicesContext getDatabaseContext()
    {
        return new DataServicesContext(_contextOptions);
    }

    public void Dispose() => _connection.Dispose();
}
