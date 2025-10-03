namespace NHS.CohortManager.SnapshotTests;

using System.Threading.Tasks;
using Common;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using VerifyMSTest;


[TestClass]
public class AmendStep2Tests : VerifyBase
{
    private static DataServicesContext _dbContext;
    private static VerifySettings verifySettings;
    private static SnapshotTestHelper _testHelper;
    private static SnapshotTestsConfig _config;
    private static List<long?> _nhsNumbers;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        verifySettings = SnapshotTestHelper.ConfigureVerify("AmendStep2");

        _config = SnapshotTestHelper.GetConfig();

        var options = new DbContextOptionsBuilder<DataServicesContext>()
            .UseSqlServer(_config.DbConnectionString)
            .Options;

        _dbContext = new DataServicesContext(options);

        _testHelper = new SnapshotTestHelper(_config);
        _nhsNumbers = _testHelper.GetNhsNumbersFromFile();

        _testHelper.UploadFileToStorage(_config.AmendFile2Name);

        Task.Delay(_config.StartDelay).GetAwaiter().GetResult();
    }

    [TestMethod]
    [TestCategory("AmendStep2")]
    public async Task ValidateExceptionTable()
    {
        await _testHelper.ValidateWithRetries(async () =>
        {
            var exceptions = await _dbContext.exceptionManagements
                .Where(i => _nhsNumbers.Select(i => i.ToString()).ToList().Contains(i.NhsNumber))
                .OrderBy(i => i.NhsNumber)
                .ThenBy(i => i.RuleId)
                .ThenBy(i => i.RuleDescription)
                .ToListAsync();
            await Verify(exceptions, verifySettings).ScrubLinesContaining("ParticipantId");
        });
    }

    [TestMethod]
    [TestCategory("AmendStep2")]
    public async Task ValidateParticipantManagementTable()
    {
        await _testHelper.ValidateWithRetries(async () =>
        {
            var participants = await _dbContext.participantManagements
                .Where(i => _nhsNumbers.Contains(i.NHSNumber))
                .OrderBy(i => i.NHSNumber)
                .ThenBy(i => i.RecordType)
                .ThenBy(i => i.EligibilityFlag)
                .ThenBy(i => i.ExceptionFlag)
                .ThenBy(i => i.ReasonForRemovalDate)
                .ThenBy(i => i.ReasonForRemoval)
                .ThenBy(i => i.ScreeningId)
                .ToListAsync();
            await Verify(participants, verifySettings);
        });
    }

    [TestMethod]
    [TestCategory("AmendStep2")]
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
    [TestCategory("AmendStep2")]
    public async Task ValidateCohortDistributionTable()
    {
        await _testHelper.ValidateWithRetries(async () =>
        {
            var participants = await _dbContext.cohortDistributions
                .Where(i => _nhsNumbers.Contains(i.NHSNumber))
                .OrderBy(i => i.NHSNumber)
                .ThenBy(i => i.NamePrefix)
                .ThenBy(i => i.PreviousFamilyName)
                .ThenBy(i => i.AddressLine1)
                .ThenBy(i => i.AddressLine2)
                .ThenBy(i => i.AddressLine3)
                .ThenBy(i => i.AddressLine4)
                .ThenBy(i => i.AddressLine5)
                .ThenBy(i => i.OtherGivenName)
                .ThenBy(i => i.Gender)
                .ThenBy(i => i.GivenName)
                .ThenBy(i => i.PrimaryCareProvider)
                .ThenBy(i => i.DateOfDeath)
                .ThenBy(i => i.EmailAddressHome)
                .ThenBy(i => i.TelephoneNumberMob)
                .ThenBy(i => i.TelephoneNumberHome)
                .ThenBy(i => i.ReasonForRemoval)
                .ThenBy(i => i.FamilyName)
                .ToListAsync();
            await Verify(participants, verifySettings);
        });
    }
}
