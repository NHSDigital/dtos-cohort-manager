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
    private static readonly string[] TokenScopes = new[] { "https://database.windows.net/.default" };
    protected Program() { }
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

                    if (config.SQL_IDENTITY_CLIENT_ID is not null)
                    {
                        var credential = new ManagedIdentityCredential(config.SQL_IDENTITY_CLIENT_ID);
                        var token = credential.GetToken(new Azure.Core.TokenRequestContext(TokenScopes));
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
            if (remainingMigrations.Count != 0)
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
            logger.LogError(ex, "Migration Failed");
            return ExitCodes.FAILURE;
        }
    }

    static async Task<int> SeedData(IHost host)
    {
        using var scope = host.Services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var SeedDataLoader = scope.ServiceProvider.GetRequiredService<ISeedDataLoader>();
        try
        {
            await SeedDataLoader.LoadData<BsoOrganisation>("./SeedData/BsoOrganisation.json", "BSO_ORGANISATIONS");
            await SeedDataLoader.LoadData<BsSelectGpPractice>("./SeedData/BsSelectGpPractice.json", "BS_SELECT_GP_PRACTICE_LKP", false);
            await SeedDataLoader.LoadData<BsSelectOutCode>("./SeedData/BsSelectOutCode.json", "BS_SELECT_OUTCODE_MAPPING_LKP", false);
            await SeedDataLoader.LoadData<CurrentPosting>("./SeedData/CurrentPosting.json", "CURRENT_POSTING_LKP", false);
            await SeedDataLoader.LoadData<ExcludedSMULookup>("./SeedData/ExcludedSMULookup.json", "EXCLUDED_SMU_LKP", false);
            await SeedDataLoader.LoadData<GenderMaster>("./SeedData/GenderMaster.json", "GENDER_MASTER", false);
            await SeedDataLoader.LoadData<GeneCodeLkp>("./SeedData/GeneCodeLkp.json", "GENE_CODE_LKP");
            await SeedDataLoader.LoadData<HigherRiskReferralReasonLkp>("./SeedData/HigherRiskReferralReasonLkp.json", "HIGHER_RISK_REFERRAL_REASON_LKP");
            await SeedDataLoader.LoadData<LanguageCode>("./SeedData/LanguageCode.json", "LANGUAGE_CODES", false);
            await SeedDataLoader.LoadData<ScreeningLkp>("./SeedData/ScreeningLkp.json", "SCREENING_LKP");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert SeedData");
            return ExitCodes.FAILURE;
        }
        return ExitCodes.SUCCESS;

    }

    static async Task<bool> ExtractData(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataServicesContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await ExtractFor<ExcludedSMULookup>(dbContext, "ExcludedSMULookup.json");
        await ExtractFor<CurrentPosting>(dbContext, "CurrentPosting.json");
        await ExtractFor<BsSelectOutCode>(dbContext, "BsSelectOutCode.json");
        await ExtractFor<BsSelectGpPractice>(dbContext, "BsSelectGpPractice.json");
        return true;

    }

    static async Task<bool> ExtractFor<T>(DataServicesContext context, string fileName) where T : class
    {
        try
        {
            // Get all data from the DbSet<T>
            var data = await context.Set<T>().ToListAsync();

            // Serialize to JSON (pretty-printed for readability)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles // prevents circular ref issues
            };

            string json = JsonSerializer.Serialize(data, options);

            // Write to file
            await File.WriteAllTextAsync(fileName, json);

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error extracting data for {typeof(T).Name}: {ex.Message}");
            return false;
        }
    }



}
