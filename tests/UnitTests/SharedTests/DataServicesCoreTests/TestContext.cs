namespace NHS.CohortManager.Tests.Shared;

using Microsoft.EntityFrameworkCore;

public class TestContext : DbContext
{
    public TestContext(DbContextOptions<TestContext> options) : base(options) { }

    public DbSet<TestEntity> TestEntities {get; set;}
}