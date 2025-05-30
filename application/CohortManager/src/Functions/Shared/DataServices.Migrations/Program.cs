namespace DataServices.Migrations;

using System.Text.Json;
using System.Threading.Tasks;

using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using Common;
using DataServices.Database;
using Model;
using Model.Enums;

public class Program
{
    protected Program() {}
    public static int Main(string[] args)
    {
        List<string> configFiles = new List<string> { "appsettings.json" }; // Only used for local

        var config = ConfigurationExtension.GetConfiguration<DatabaseConfig>(null, configFiles);
        using var host = CreateHostBuilder(config).Build();

        var migrationsApplied = ApplyMigrations(host);
        if (migrationsApplied == ExitCodes.FAILURE)
        {
            return ExitCodes.FAILURE;
        }

        var seedDataLoaded = SeedData(host).Result;
        if (seedDataLoaded == ExitCodes.FAILURE)
        {
            return ExitCodes.FAILURE;
        }

        return ExitCodes.SUCCESS;

    }

    static IHostBuilder CreateHostBuilder(DatabaseConfig config) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<DataServicesContext>(options =>
                {
                    var sqlConnectionBuilder = new SqlConnectionStringBuilder(config.DtOsDatabaseConnectionString);
                    var connection = new SqlConnection(sqlConnectionBuilder.ConnectionString);

                    if(config.SQL_IDENTITY_CLIENT_ID is not null)
                    {
                        var credential = new ManagedIdentityCredential(config.SQL_IDENTITY_CLIENT_ID );
                        var token = credential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));
                        connection.AccessToken = token.Token;
                    }

                    options.UseSqlServer(connection, sqlServerOptionsAction =>
                    {
                        sqlServerOptionsAction.MigrationsAssembly("DataServices.Migrations");
                        sqlServerOptionsAction.UseNetTopologySuite();
                    });
                });
                services.AddScoped<ISeedDataLoader, SeedDataLoader>();

            });

    static int ApplyMigrations(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataServicesContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying Migrations...");
            dbContext.Database.Migrate();
            var remainingMigrations = dbContext.Database.GetPendingMigrations().ToList();
            if (remainingMigrations.Any())
            {
                Console.WriteLine("Some migrations were not applied.");
                return ExitCodes.FAILURE;
            }
            else
            {
                logger.LogInformation("Migrations Applied Successfully!");
                return ExitCodes.SUCCESS;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Migration Failed: {ex.Message}");
            return ExitCodes.FAILURE;
        }
    }

    static async Task<int> SeedData(IHost host)
    {
        using var scope = host.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DataServicesContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var SeedDataLoader = scope.ServiceProvider.GetRequiredService<ISeedDataLoader>();
        try
        {
            await SeedDataLoader.LoadData<BsoOrganisation>("./SeedData/BsoOrganisation.json","BSO_ORGANISATIONS");
            await SeedDataLoader.LoadData<BsSelectGpPractice>("./SeedData/BsSelectGpPractice.json","BS_SELECT_GP_PRACTICE_LKP",false);
            await SeedDataLoader.LoadData<BsSelectOutCode>("./SeedData/BsSelectOutCode.json","BS_SELECT_OUTCODE_MAPPING_LKP",false);
            await SeedDataLoader.LoadData<CurrentPosting>("./SeedData/CurrentPosting.json","CURRENT_POSTING_LKP",false);
            await SeedDataLoader.LoadData<ExcludedSMULookup>("./SeedData/ExcludedSMULookup.json","EXCLUDED_SMU_LKP",false);
            await SeedDataLoader.LoadData<GenderMaster>("./SeedData/GenderMaster.json","GENDER_MASTER",false);
            await SeedDataLoader.LoadData<GeneCodeLkp>("./SeedData/GeneCodeLkp.json","GENE_CODE_LKP");
            await SeedDataLoader.LoadData<GPPractice>("./SeedData/GPPractice.json","GP_PRACTICES");
            await SeedDataLoader.LoadData<HigherRiskReferralReasonLkp>("./SeedData/HigherRiskReferralReasonLkp.json","HIGHER_RISK_REFERRAL_REASON_LKP");
            await SeedDataLoader.LoadData<LanguageCode>("./SeedData/LanguageCode.json","LANGUAGE_CODES",false);
            await SeedDataLoader.LoadData<ScreeningLkp>("./SeedData/ScreeningLkp.json","SCREENING_LKP");
        }
        catch(Exception ex)
        {
            logger.LogError(ex,"Failed to insert SeedData");
            return ExitCodes.FAILURE;
        }
        return ExitCodes.SUCCESS;

    }

    static int ExtractData(IHost host)
    {
        using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataServicesContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        ExtractDataofType<BsoOrganisation>(dbContext);
        ExtractDataofType<BsSelectGpPractice>(dbContext);
        ExtractDataofType<BsSelectOutCode>(dbContext);
        ExtractDataofType<CurrentPosting>(dbContext);
        ExtractDataofType<ExcludedSMULookup>(dbContext);
        ExtractDataofType<GenderMaster>(dbContext);
        ExtractDataofType<GeneCodeLkp>(dbContext);
        ExtractDataofType<GPPractice>(dbContext);
        ExtractDataofType<HigherRiskReferralReasonLkp>(dbContext);
        ExtractDataofType<LanguageCode>(dbContext);
        ExtractDataofType<ScreeningLkp>(dbContext);

        return 0;
    }

    public static bool ExtractDataofType<TEntity>(DbContext context) where TEntity : class
    {
        var data = context.Set<TEntity>().ToList();
        var jsonString = JsonSerializer.Serialize(data);

        File.WriteAllText($"{typeof(TEntity).Name}.json", jsonString);
        return true;
    }

}
