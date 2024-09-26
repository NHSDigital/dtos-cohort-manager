namespace DataServices.Database;

using Microsoft.EntityFrameworkCore;

public class DataServicesContext : DbContext
{
    DbSet<BsSelectGpPractice> bsSelectGpPractices {get; set;}
    public DataServicesContext(DbContextOptions<DataServicesContext> options) : base(options)
    {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BsSelectGpPractice>()
            .ToTable("BS_SELECT_GP_PRACTICE_LKP","dbo");
    }

}
