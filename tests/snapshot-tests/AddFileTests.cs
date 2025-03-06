namespace NHS.CohortManager.Tests.SnapshotTests;

using System.Threading.Tasks;
using Common;
using DataServices.Database;
using DiffEngine;
using Microsoft.EntityFrameworkCore;
using Model;
using VerifyMSTest;


[TestClass]
public class AddFileTests : VerifyBase
{
    private DataServicesContext _dbContext;
    private readonly VerifySettings verifySettings;
    public AddFileTests()
    {
        verifySettings = new VerifySettings();
        verifySettings.UseDirectory("AddStep1");
        verifySettings.DontScrubDateTimes();
        verifySettings.IgnoreMembers<ExceptionManagement>(i => i.ExceptionDate,i => i.ErrorRecord,i => i.DateCreated,i => i.ExceptionId );
        verifySettings.IgnoreMembers<ParticipantManagement>(i => i.ParticipantId, i => i.RecordInsertDateTime, i => i.RecordInsertDateTime);
        verifySettings.IgnoreMembers<CohortDistribution>(i => i.CohortDistributionId, i => i.ParticipantId,i => i.RecordInsertDateTime, i => i.RecordUpdateDateTime, i => i.RequestId);

        List<string> configFiles = ["appsettings.json"];
        var config = ConfigurationExtension.GetConfiguration<SnapshotTestsConfig>(null,configFiles);

        var options = new DbContextOptionsBuilder<DataServicesContext>().UseSqlServer(config.ConnectionString).Options;
        _dbContext = new DataServicesContext(options);

    }
    [TestMethod]
    public async Task ValidateExceptionTable()
    {
        var Exceptions = await _dbContext.ExceptionManagements.OrderBy(i => i.NhsNumber).ThenBy(i => i.RuleId).ToListAsync();
        await Verify(Exceptions,verifySettings);
    }
    [TestMethod]
    public async Task ValidateParticipantManagementTable()
    {
        var Participants = await _dbContext.ParticipantManagements.OrderBy(i => i.NHSNumber).ToListAsync();
        await Verify(Participants,verifySettings);
    }
    [TestMethod]
    public async Task ValidateCohortDistributionTable()
    {
        var Participants = await _dbContext.CohortDistributions.OrderBy(i => i.NHSNumber).ToListAsync();
        await Verify(Participants,verifySettings);
    }
}
