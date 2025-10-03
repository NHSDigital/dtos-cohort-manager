namespace NHS.CohortManager.SnapshotTests;

using System.Threading.Tasks;
using Common;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using VerifyMSTest;


[TestClass]
public class AddTests : VerifyBase
{
    private static DataServicesContext _dbContext;
    private static VerifySettings verifySettings;
    private static SnapshotTestHelper _testHelper;
    private static SnapshotTestsConfig _config;
    private static List<long?> _nhsNumbers;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        verifySettings = SnapshotTestHelper.ConfigureVerify("Add");

        _config = SnapshotTestHelper.GetConfig();

        var options = new DbContextOptionsBuilder<DataServicesContext>()
            .UseSqlServer(_config.DbConnectionString)
            .Options;

        _dbContext = new DataServicesContext(options);

        _testHelper = new SnapshotTestHelper(_config);
        _nhsNumbers = _testHelper.GetNhsNumbersFromFile();

        _testHelper.ClearDatabase(_dbContext, _nhsNumbers);
        _testHelper.UploadFileToStorage(_config.AddFileName);

        Task.Delay(_config.StartDelay).GetAwaiter().GetResult();
    }

    [TestMethod]
    [TestCategory("Add")]
    public async Task ValidateExceptionTable()
    {
        await _testHelper.ValidateWithRetries(async () => 
        {
            var exceptions = await _dbContext.exceptionManagements
                .Where(i => _nhsNumbers.Select(i => i.ToString()).ToList().Contains(i.NhsNumber))
                .OrderBy(i => i.NhsNumber).
                ThenBy(i => i.RuleId)
                .ToListAsync();
            await Verify(exceptions, verifySettings).ScrubLinesContaining("ParticipantId");
        });
    }

    [TestMethod]
    [TestCategory("Add")]
    public async Task ValidateParticipantManagementTable()
    {
        await _testHelper.ValidateWithRetries(async () =>
        {
            var participants = await _dbContext.participantManagements
                .Where(i => _nhsNumbers.Contains(i.NHSNumber))
                .OrderBy(i => i.NHSNumber)
                .ThenBy(i => i.RecordType)
                .ToListAsync();
            await Verify(participants, verifySettings);
        });
    }

    [TestMethod]
    [TestCategory("Add")]
    public async Task ValidateParticipantDemographicTable()
    {
        await _testHelper.ValidateWithRetries(async () =>
        {
            var participants = await _dbContext.participantDemographics
                .Where(i => _nhsNumbers.Contains(i.NhsNumber))
                .OrderBy(i => i.NhsNumber)
                .ToListAsync();
            await Verify(participants, verifySettings);
        });
    }

    [TestMethod]
    [TestCategory("Add")]
    public async Task ValidateCohortDistributionTable()
    {
        await _testHelper.ValidateWithRetries(async () =>
        {
            var participants = await _dbContext.cohortDistributions
                .Where(i => _nhsNumbers.Contains(i.NHSNumber))
                .OrderBy(i => i.NHSNumber)
                .ToListAsync();
            await Verify(participants, verifySettings);
        });
    }
}
