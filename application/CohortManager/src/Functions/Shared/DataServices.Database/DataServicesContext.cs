namespace DataServices.Database;

using Microsoft.EntityFrameworkCore;

public class DataServicesContext : DbContext
{
    DbSet<BsSelectGpPractice> bsSelectGpPractices {get; set;}
    DbSet<BsSelectOutCode> bsSelectOutcodes {get;set;}
    DbSet<LanguageCode> languageCodes {get;set;}
    public DataServicesContext(DbContextOptions<DataServicesContext> options) : base(options)
    {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BsSelectGpPractice>()
            .ToTable("BS_SELECT_GP_PRACTICE_LKP","dbo");

        modelBuilder.Entity<BsSelectOutCode>()
            .ToTable("BS_SELECT_OUTCODE_MAPPING_LKP","dbo");

        modelBuilder.Entity<LanguageCode>()
            .ToTable("LANGUAGE_CODES","dbo");
    }

}
