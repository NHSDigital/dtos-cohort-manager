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
    DbSet<ParticipantManagement> participantManagements { get; set; }
    DbSet<ParticipantDemographic> participantDemographics { get; set; }
    DbSet<GeneCodeLkp> geneCodeLkps { get; set; }
    DbSet<HigherRiskReferralReasonLkp> higherRiskReferralReasonLkps { get; set; }
    DbSet<ExceptionManagement> exceptionManagements { get; set; }
    DbSet<CohortDistribution> cohortDistributions { get; set; }
    DbSet<BsSelectRequestAudit> bsSelectRequestAudits { get; set; }
    DbSet<ScreeningLkp> screeningLkps { get; set; }
    DbSet<BsoOrganisation> bsoOrganisations { get; set; }
    DbSet<GenderMaster> genderMasters { get; set; }
    DbSet<NemsSubscription> nemsSubscriptions { get; set; }
    DbSet<ServicenowCase> servicenowCases { get; set; }

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
            .ToTable("PARTICIPANT_MANAGEMENT", "dbo")
            .HasIndex(i => new { i.NHSNumber, i.ScreeningId }, "ix_PARTICIPANT_MANAGEMENT_screening_nhs")
            .IsUnique();

        modelBuilder.Entity<ParticipantDemographic>()
            .ToTable("PARTICIPANT_DEMOGRAPHIC", "dbo")
            .HasIndex(i => new { i.NhsNumber }, "Index_PARTICIPANT_DEMOGRAPHIC_NhsNumber")
            .IsUnique();

        modelBuilder.Entity<GeneCodeLkp>()
            .ToTable("GENE_CODE_LKP", "dbo");

        modelBuilder.Entity<HigherRiskReferralReasonLkp>()
            .ToTable("HIGHER_RISK_REFERRAL_REASON_LKP", "dbo");


        modelBuilder.Entity<ExceptionManagement>()
            .ToTable("EXCEPTION_MANAGEMENT", "dbo")
            .HasIndex(i => new { i.NhsNumber, i.ScreeningName }, "IX_EXCEPTIONMGMT_NHSNUM_SCREENINGNAME");

        modelBuilder.Entity<CohortDistribution>()
            .ToTable("BS_COHORT_DISTRIBUTION", "dbo")
            .HasIndex(c => new { c.NHSNumber }, "IX_BS_COHORT_DISTRIBUTION_NHSNUMBER");

        modelBuilder.Entity<CohortDistribution>()
            .HasIndex(c => new { c.IsExtracted, c.RequestId }, "IX_BSCOHORT_IS_EXTACTED_REQUESTID");

        modelBuilder.Entity<BsSelectRequestAudit>()
            .ToTable("BS_SELECT_REQUEST_AUDIT", "dbo");

        modelBuilder.Entity<ScreeningLkp>()
            .ToTable("SCREENING_LKP", "dbo");

        modelBuilder.Entity<BsoOrganisation>()
            .ToTable("BSO_ORGANISATIONS", "dbo");

        modelBuilder.Entity<GenderMaster>()
            .ToTable("GENDER_MASTER", "dbo");

        modelBuilder.Entity<NemsSubscription>()
            .ToTable("NEMS_SUBSCRIPTION", "dbo");

        modelBuilder.Entity<ServicenowCase>()
            .ToTable("SERVICENOW_CASES", "dbo");
    }
}
