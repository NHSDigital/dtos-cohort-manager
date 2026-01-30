namespace ComponentTests;
using Allure.NUnit;
using DataServices.Database;
using Hl7.Fhir.Utility;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Operation.Buffer;
using NUnit.Framework.Internal;
using NHS.CohortManager.CohortDistributionServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AspectInjector.Broker;
using Microsoft.Extensions.Logging;

[AllureNUnit]
public class DistributeParticipantComponentTests
{
    private TestDatabase testDatabase;
    private DataServicesContext db;
    private DistributeParticipant distributeParticipant;

    private IServiceScope scope;

    public DistributeParticipantComponentTests()
    {
        var host = HostBuilder.CreateHostBuilder().Build();

        scope = host.Services.CreateScope();
        testDatabase = new TestDatabase();
        DistributeParticipantConfig config = new DistributeParticipantConfig
        {

        };
        distributeParticipant = new DistributeParticipant(scope.ServiceProvider.GetRequiredService<ILogger<DistributeParticipant>>(),)
    }
    [SetUp]
    public async Task Setup()
    {
       await testDatabase.SeedDatabase();
       db = testDatabase.getDatabaseContext();


    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
    [Test]
    public void Test2()
    {
        Assert.Pass();
    }
    [Test]
    public async Task DBTest()
    {
        var currentpostincnt = await db.currentPostings.CountAsync();
        Assert.That(currentpostincnt > 0);
    }



    [OneTimeTearDown]
    public void teardown()
    {
        scope.Dispose();
        testDatabase.Dispose();
    }
    [TearDown]
    public void singleTearDown()
    {
        db.Dispose();
    }
}
