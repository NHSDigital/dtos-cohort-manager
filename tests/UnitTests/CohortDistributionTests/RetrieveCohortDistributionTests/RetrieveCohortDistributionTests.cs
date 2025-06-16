namespace RetrieveCohortDistributionTests;

using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Net;
using Castle.Core.Logging;
using Common;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<ILogger<CreateCohortDistributionData>> _createCohortLogger = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionDataClient = new();
    private readonly Mock<IDataServiceClient<BsSelectRequestAudit>> _requestAuditDistributionDataClient = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();




    public RetrieveCohortDistributionTests()
    {
        _createCohortDistribution = new CreateCohortDistributionData(_createCohortLogger.Object, _cohortDistributionDataClient.Object, _requestAuditDistributionDataClient.Object);
        _sut = new RetrieveCohortDistributionData(_retrieveCohortLogger.Object, _createCohortDistribution, _createResponse.Object, _exceptionHandler.Object);

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
    public async Task Run_GetBatchNoRowCountOrRequestId_ReturnsBadRequest()
    {
        // arrange
        NameValueCollection urlQueryItems = new NameValueCollection();
        var req = _setupRequest.Setup(null, urlQueryItems);

        // act
        var result = await _sut.Run(req.Object);

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);

    }
}
