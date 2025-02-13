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
    DbSet<BsSelectRequestAudit> bsSelectRequestAudits {get;set;}

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

        modelBuilder.Entity<GeneCodeLkp>()
            .ToTable("GENE_CODE_LKP", "dbo");

        modelBuilder.Entity<HigherRiskReferralReasonLkp>()
            .ToTable("HIGHER_RISK_REFERRAL_REASON_LKP", "dbo");
        modelBuilder.Entity<ExceptionManagement>()
            .ToTable("EXCEPTION_MANAGEMENT", "dbo");

        modelBuilder.Entity<GPPractice>()
            .ToTable("GP_PRACTICES", "dbo");

        modelBuilder.Entity<CohortDistribution>()
            .ToTable("BS_COHORT_DISTRIBUTION", "dbo");
        
        modelBuilder.Entity<BsSelectRequestAudit>()
            .ToTable("BS_SELECT_REQUEST_AUDIT","dbo");

        modelBuilder.Entity<ScreeningLkp>()
            .ToTable("SCREENING_LKP", "dbo");
    }
}
