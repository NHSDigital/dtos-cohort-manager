namespace NHS.CohortManager.Tests.UnitTests.AddCohortDistributionDataTests;

using System.Linq.Expressions;
using System.Threading.Tasks;
using Data.Database;
using DataServices.Client;
using Model;
using Moq;

[TestClass]
public class AddCohortDistributionTests
{
    private readonly CreateCohortDistributionData _createCohortDistributionData;
    private readonly Guid _requestId = Guid.NewGuid();
    private List<CohortDistribution> _cohortDistributionList;
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionDataServiceClient = new();
    private readonly Mock<IDataServiceClient<BsSelectRequestAudit>> _bsSelectRequestAuditDataServiceClient = new();

    public AddCohortDistributionTests()
    {
        _createCohortDistributionData = new CreateCohortDistributionData(_cohortDistributionDataServiceClient.Object, _bsSelectRequestAuditDataServiceClient.Object);
    }

    [TestMethod]
    public async Task ExtractCohortDistributionParticipants_ValidRequest_ReturnsListOfParticipants()
    {
        // Arrange
        var listOfValues = new List<CohortDistribution>()
        {
            new CohortDistribution
            {
                ParticipantId = 1,
                RecordInsertDateTime = DateTime.UtcNow.Date
            }
        };

        var rowCount = 1;
        _cohortDistributionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>())).ReturnsAsync(listOfValues);
        _cohortDistributionDataServiceClient.Setup(x => x.Update(It.IsAny<CohortDistribution>())).ReturnsAsync(true);
        _cohortDistributionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new CohortDistribution());
        // Act
        var result = await _createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);

        // Assert
        Assert.AreEqual("1", result.FirstOrDefault()?.ParticipantId);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task ExtractCohortDistributionParticipants_AfterExtraction_MarksBothParticipantsAsExtracted()
    {
        // Arrange
        _cohortDistributionList = new List<CohortDistribution>
        {
            new CohortDistribution
            {
                ParticipantId = 1,
                IsExtracted = 1
            },
            new CohortDistribution
            {
                ParticipantId = 2,
                IsExtracted = 1
            }
        };
        var rowCount = 2;
        _cohortDistributionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>())).ReturnsAsync(_cohortDistributionList);
        _cohortDistributionDataServiceClient.Setup(x => x.Update(It.IsAny<CohortDistribution>())).ReturnsAsync(true);
        _cohortDistributionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new CohortDistribution());

        // Act
        var result = await _createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);

        // Assert

        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual("1", result[0].IsExtracted);
        Assert.AreEqual("2", result[1].ParticipantId);
        Assert.AreEqual("1", result[1].IsExtracted);
    }

    [TestMethod]
    public async Task GetParticipant_NoParticipants_ReturnsEmptyCollection()
    {
        // Arrange
        var rowCount = 0;

        // Act
        var result = await _createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetCohortDistributionParticipantsByRequestId_RequestId_ReturnsMatchingParticipants()
    {
        // Act
        _cohortDistributionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>())).ReturnsAsync(new List<CohortDistribution>());
        var validRequestIdResult = await _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);
        var inValidRequestIdResult = await _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(Guid.Empty);

        // Assert
        Assert.AreEqual(0, inValidRequestIdResult.Count);
    }

    [TestMethod]
    public async Task GetCohortDistributionParticipantsByRequestId_NoParticipants_ReturnsEmptyList()
    {
        // Arrange

        // Act
        var result = await _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }
    [TestMethod]
    public async Task GetCohortDistributionParticipantsByRequestId_ValidRequestId_ReturnsParticipants()
    {
        var requestId = new Guid();
        var listOfValues = new List<CohortDistribution>()
        {
            new CohortDistribution
            {
                ParticipantId = 1,
                RecordInsertDateTime = DateTime.UtcNow.Date,
                RequestId = requestId
            }
        };

        _cohortDistributionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>())).ReturnsAsync(listOfValues);
        // Act
        var result = await _createCohortDistributionData.GetCohortDistributionParticipantsByRequestId(_requestId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual(requestId.ToString(), result[0].RequestId);
    }
}
