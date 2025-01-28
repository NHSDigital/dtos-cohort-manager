namespace DataServices.Database;

using Model;
using Microsoft.EntityFrameworkCore;

public class DataServicesContext : DbContext
{
    DbSet<BsSelectGpPractice> bsSelectGpPractices { get; set; }
    DbSet<BsSelectOutCode> bsSelectOutcodes { get; set; }
    DbSet<LanguageCode> languageCodes { get; set; }
    DbSet<CurrentPosting> currentPostings { get; set; }
    DbSet<ExcludedSMULookup> excludedSMULookups { get; set; }

    DbSet<ExceptionManagement> exceptionManagements { get; set; }

    DbSet<GPPractice> gPPractices { get; set; }
    public DataServicesContext(DbContextOptions<DataServicesContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BsSelectGpPractice>()
            .ToTable("BS_SELECT_GP_PRACTICE_LKP", "dbo");

        modelBuilder.Entity<BsSelectOutCode>()
            .ToTable("BS_SELECT_OUTCODE_MAPPING_LKP", "dbo");

        modelBuilder.Entity<LanguageCode>()
            .ToTable("LANGUAGE_CODES", "dbo");

        modelBuilder.Entity<CurrentPosting>()
            .ToTable("CURRENT_POSTING_LKP", "dbo");

        modelBuilder.Entity<ExcludedSMULookup>()
            .ToTable("EXCLUDED_SMU_LKP", "dbo");

        modelBuilder.Entity<ParticipantManagement>()
            .ToTable("PARTICIPANT_MANAGEMENT", "dbo");

        modelBuilder.Entity<ParticipantDemographic>()
            .ToTable("PARTICIPANT_DEMOGRAPHIC", "dbo");

        modelBuilder.Entity<ExceptionManagement>()
                    .ToTable("EXCEPTION_MANAGEMENT", "dbo");

        modelBuilder.Entity<GPPractice>()
            .ToTable("GP_PRACTICES", "dbo");

    }

}
