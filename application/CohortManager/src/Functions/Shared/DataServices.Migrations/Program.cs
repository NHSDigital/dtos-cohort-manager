namespace DataServices.Migrations;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataServices.Database;
using Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using System.Text.Json;

public class Program
{
    public static int Main(string[] args)
    {

        List<string> configFiles = ["appsettings.json"];
        var config = ConfigurationExtension.GetConfiguration<DatabaseConfig>(null,configFiles);
        using var host = CreateHostBuilder(config.ConnectionString).Build();
        return ApplyMigrations(host);


    }
    static IHostBuilder CreateHostBuilder(string connectionString) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<DataServicesContext>(options =>
                    options.UseSqlServer(connectionString, x => x.MigrationsAssembly("DataServices.Migrations")));

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
            logger.LogInformation("Migrations Applied Successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError($"Migration Failed: {ex.Message}");
            return 1;
        }
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

        File.WriteAllText($"{typeof(TEntity).FullName}.json", jsonString);
        return true;
    }






}
