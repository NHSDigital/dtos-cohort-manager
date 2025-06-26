namespace RetrieveCohortDistributionTests;

using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Net;
using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.CohortDistributionDataServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class RetrieveCohortDistributionTests
{

    private readonly SetupRequest _setupRequest = new();
    private readonly RetrieveCohortDistributionData _sut;
    private readonly ICreateCohortDistributionData _createCohortDistribution;
    private readonly Mock<ILogger<RetrieveCohortDistributionData>> _retrieveCohortLogger = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionDataClient = new();
    private readonly Mock<IDataServiceClient<BsSelectRequestAudit>> _requestAuditDistributionDataClient = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly Mock<IOptions<RetrieveCohortDistributionConfig>> _config = new();

    public RetrieveCohortDistributionTests()
    {

        _config.Setup(i => i.Value).Returns(new RetrieveCohortDistributionConfig());
        _createCohortDistribution = new CreateCohortDistributionData(_cohortDistributionDataClient.Object, _requestAuditDistributionDataClient.Object);
        _sut = new RetrieveCohortDistributionData(_retrieveCohortLogger.Object, _createCohortDistribution, _createResponse.Object, _exceptionHandler.Object, _config.Object);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string?>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string? ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                if (string.IsNullOrEmpty(ResponseBody))
                {
                    response.WriteString(ResponseBody);
                }
                return response;
            });

    }
    [TestMethod]
    public async Task Run_GetNextBatchOfParticipantsNoRequestId_ReturnsOkParticipant()
    {
        // arrange
        NameValueCollection urlQueryItems = new NameValueCollection();
        urlQueryItems["rowCount"] = "1";
        var req = _setupRequest.Setup(null, urlQueryItems);

        var cohortDistributionParticipant = new CohortDistribution { NHSNumber = 123456789 };
        var participantsList = new List<CohortDistribution>
        {
            cohortDistributionParticipant
        };

        _cohortDistributionDataClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participantsList);

        _cohortDistributionDataClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(cohortDistributionParticipant);

        _cohortDistributionDataClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync(true);

        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

    }
    [TestMethod]
    public async Task Run_GetNextBatchOfParticipantsNoRequestId_ReturnsNoContent()
    {
        // arrange
        NameValueCollection urlQueryItems = new NameValueCollection();
        urlQueryItems["rowCount"] = "1";
        var req = _setupRequest.Setup(null, urlQueryItems);


        var participantsList = new List<CohortDistribution>
        { };

        _cohortDistributionDataClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participantsList);

        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);

    }
    [TestMethod]
    public async Task Run_GetBatchBadRequestId_ReturnsBadRequest()
    {
        // arrange
        NameValueCollection urlQueryItems = new NameValueCollection();
        urlQueryItems["rowCount"] = "1";
        urlQueryItems["requestId"] = "invalid-guid-is-this";
        var req = _setupRequest.Setup(null, urlQueryItems);
        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);

    }
    [TestMethod]
    public async Task Run_GetBatchNoRowCountOrRequestId_ReturnsOkData()
    {
        // arrange
        NameValueCollection urlQueryItems = new NameValueCollection();
        var req = _setupRequest.Setup(null, urlQueryItems);

        var cohortDistributionParticipant = new CohortDistribution { NHSNumber = 123456789 };
        var participantsList = new List<CohortDistribution>
        {
            cohortDistributionParticipant
        };

        _cohortDistributionDataClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participantsList);

        _cohortDistributionDataClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(cohortDistributionParticipant);

        _cohortDistributionDataClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync(true);


        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

    }
    [TestMethod]
    public async Task Run_GetBatchWithRequestIdNextBatchNotNull_ReturnsSuccessful()
    {
        // arrange
        NameValueCollection urlQueryItems = new NameValueCollection();
        urlQueryItems["rowCount"] = "1";
        urlQueryItems["requestId"] = "8f6282aa-1a5f-43fa-bac4-5d5aa532eded";
        var req = _setupRequest.Setup(null, urlQueryItems);

        var okStatusCode = ((int)HttpStatusCode.OK).ToString();
        var nextRequestId = Guid.Parse("8fd9612e-3316-4e0e-9dc9-5a98ce45ae6c");

        _requestAuditDistributionDataClient
            .Setup(i => i.GetSingleByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>()))
            .ReturnsAsync(new BsSelectRequestAudit
            {
                StatusCode = okStatusCode,
                CreatedDateTime = new DateTime(2024, 12, 25, 10, 0, 0)
            });

        _requestAuditDistributionDataClient
            .Setup(i => i.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>()))
            .ReturnsAsync(new List<BsSelectRequestAudit>
            {
                new BsSelectRequestAudit{
                    RequestId = nextRequestId,
                    StatusCode = okStatusCode,
                    CreatedDateTime = new DateTime(2024, 12, 29, 10, 0, 0)
                }
            });

        _cohortDistributionDataClient
            .Setup(i => i.GetByFilter(x => x.RequestId == nextRequestId))
            .ReturnsAsync(new List<CohortDistribution>
            {
                new CohortDistribution
                {
                    RequestId = nextRequestId,
                    NHSNumber = 123
                }
            });

        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

    }
    [TestMethod]
    public async Task Run_GetBatchWithRequestIdNotFound_ReturnsBadRequest()
    {
        // arrange

        var requestId = Guid.Parse("8f6282aa-1a5f-43fa-bac4-5d5aa532eded");
        NameValueCollection urlQueryItems = new NameValueCollection();
        urlQueryItems["rowCount"] = "1";
        urlQueryItems["requestId"] = requestId.ToString();
        var req = _setupRequest.Setup(null, urlQueryItems);

        _requestAuditDistributionDataClient
            .Setup(i => i.GetSingleByFilter(x => x.RequestId == requestId))
            .Returns(Task.FromResult<BsSelectRequestAudit>(null));


        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);

    }
    [TestMethod]
    public async Task Run_GetBatchWithRequestIdNextBatchNull_ReturnsSuccessful()
    {
        // arrange
        NameValueCollection urlQueryItems = new NameValueCollection();
        urlQueryItems["rowCount"] = "1";
        urlQueryItems["requestId"] = "8f6282aa-1a5f-43fa-bac4-5d5aa532eded";
        var req = _setupRequest.Setup(null, urlQueryItems);

        var okStatusCode = ((int)HttpStatusCode.OK).ToString();

        _requestAuditDistributionDataClient
            .Setup(i => i.GetSingleByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>()))
            .ReturnsAsync(new BsSelectRequestAudit
            {
                StatusCode = okStatusCode,
                CreatedDateTime = new DateTime(2024, 12, 25, 10, 0, 0)
            });

        _requestAuditDistributionDataClient
            .Setup(i => i.GetByFilter(It.IsAny<Expression<Func<BsSelectRequestAudit, bool>>>()))
            .ReturnsAsync(new List<BsSelectRequestAudit>());

        var cohortDistributionParticipant = new CohortDistribution { NHSNumber = 123456789 };
        var participantsList = new List<CohortDistribution>
        {
            cohortDistributionParticipant
        };
        _cohortDistributionDataClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync(true);


        _cohortDistributionDataClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(cohortDistributionParticipant);


        _cohortDistributionDataClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participantsList);

        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }
}
