namespace NHS.CohortManager.Tests.UnitTests.RetrieveCohortRequestAuditTests;

using System.Net;
using System.Text.Json;
using Common;
using Common.Interfaces;
using Model;
using Moq;
using NHS.CohortManager.CohortDistributionDataServices;
using NHS.CohortManager.Tests.TestUtils;
[TestClass]
public class RetrieveCohortRequestAuditTests : DatabaseTestBaseSetup<RetrieveCohortRequestAudit>
{
    private static readonly Mock<ICreateCohortDistributionData> _createCohortDistributionData = new();
    private static readonly Mock<IExceptionHandler> _handleException = new();
    private List<CohortRequestAudit> _cohortRequestAuditList;
    private static readonly Mock<IHttpParserHelper> _httpParserHelper = new();
    public RetrieveCohortRequestAuditTests() : base((conn, logger, transaction, command, response) =>
        new RetrieveCohortRequestAudit(
        logger,
        _createCohortDistributionData.Object,
        response,
        _handleException.Object,
        _httpParserHelper.Object))
    {
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _createCohortDistributionData.Reset();
        _service = new RetrieveCohortRequestAudit(
            _loggerMock.Object,
            _createCohortDistributionData.Object,
            _createResponseMock.Object,
            _handleException.Object,
            _httpParserHelper.Object
        );
        var columnToClassPropertyMapping = new Dictionary<string, string> {
            { "REQUEST_ID", "RequestId" },
            { "STATUS_CODE", "StatusCode" },
            { "CREATED_DATETIME", "CreatedDateTime" },
        };
        _cohortRequestAuditList = new List<CohortRequestAudit>
        {
            new CohortRequestAudit {
                RequestId = "testRequestId",
                StatusCode = "200",
                CreatedDateTime = DateTime.Now.ToString("yyyyMMdd")
            }
        };
        var json = JsonSerializer.Serialize(_cohortRequestAuditList);
        SetupRequest(json);
        SetupDataReader(_cohortRequestAuditList, columnToClassPropertyMapping);
    }

    [TestMethod]
    public async Task Run_RetrieveCohortRequestAuditSucceeds_ReturnsOK()
    {
        // Arrange
        var dateFrom = DateTime.Now.AddDays(-1);
        SetupRequestWithQueryParams(new Dictionary<string, string> {
            { "requestId", "testRequestId" },
            { "statusCode", "200" },
            { "dateFrom", dateFrom.ToString("yyyyMMdd") },
        });
        _createCohortDistributionData.Setup(s => s.GetCohortRequestAudit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).ReturnsAsync(_cohortRequestAuditList);

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_RetrieveCohortRequestAuditThrowsException_LogsExceptionAndReturnsInternalServerError()
    {
        // Arrange
        var dateFrom = DateTime.Now.AddDays(-1);
        SetupRequestWithQueryParams(new Dictionary<string, string> {
            { "requestId", "testRequestId" },
            { "statusCode", "200" },
            { "dateFrom", dateFrom.ToString("yyyyMMdd") },
        });
        _createCohortDistributionData.Setup(s => s.GetCohortRequestAudit(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>())).Throws(new Exception("There was an error"));

        // Act
        var result = await _service.RunAsync(_request.Object);

        // Assert
        _handleException.Verify(i => i.CreateSystemExceptionLogFromNhsNumber(
            It.Is<Exception>((v, t) => v.ToString().Contains("There was an error")),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
