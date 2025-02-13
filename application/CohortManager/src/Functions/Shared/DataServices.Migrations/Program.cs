namespace DataServices.Migrations;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataServices.Database;
using Common;
using Microsoft.Extensions.Hosting;

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

        try
        {
            Console.WriteLine("Applying Migrations...");
            dbContext.Database.Migrate();
            Console.WriteLine("Migrations Applied Successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration Failed: {ex.Message}");
            return 1;
        }
    }


}
