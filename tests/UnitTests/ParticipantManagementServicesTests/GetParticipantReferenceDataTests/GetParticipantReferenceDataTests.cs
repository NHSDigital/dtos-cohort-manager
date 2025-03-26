namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NHS.CohortManager.ParticipantManagementService;
using Common;
using Model;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;

[TestClass]
public class GetParticipantReferenceDataTests
{
    private Mock<ILogger<GetParticipantReferenceData>> _mockLogger;
    private Mock<ICreateResponse> _mockCreateResponse;
    private Mock<IDataServiceClient<GeneCodeLkp>> _mockGeneCodeClient;
    private Mock<IDataServiceClient<HigherRiskReferralReasonLkp>> _mockRiskReasonClient;
    private readonly Mock<FunctionContext> _context = new();
    private readonly HttpResponseData _mockHttpResponseData;

    private GetParticipantReferenceData _function;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<GetParticipantReferenceData>>();
        _mockCreateResponse = new Mock<ICreateResponse>();
        _mockGeneCodeClient = new Mock<IDataServiceClient<GeneCodeLkp>>();
        _mockRiskReasonClient = new Mock<IDataServiceClient<HigherRiskReferralReasonLkp>>();

        _function = new GetParticipantReferenceData(
            _mockLogger.Object,
            _mockCreateResponse.Object,
            _mockGeneCodeClient.Object,
            _mockRiskReasonClient.Object
        );
    }

    [TestMethod]
    public async Task Run_ReturnsOk_WithData()
    {
        // Arrange

        var geneCodeList = new List<GeneCodeLkp>
        {
            new GeneCodeLkp { GeneCode = "A1", GeneCodeDescription = "Gene A1" }
        };

        var riskReasonList = new List<HigherRiskReferralReasonLkp>
        {
            new HigherRiskReferralReasonLkp { HigherRiskReferralReasonCode = "HR1", HigherRiskReferralReasonCodeDescription = "Reason HR1" }
        };

        _mockGeneCodeClient.Setup(x => x.GetAll()).ReturnsAsync(geneCodeList);
        _mockRiskReasonClient.Setup(x => x.GetAll()).ReturnsAsync(riskReasonList);

        var expectedData = new ParticipantReferenceData(
            new Dictionary<string, string> { { "A1", "Gene A1" } },
            new Dictionary<string, string> { { "HR1", "Reason HR1" } }
        );

        var req = new MockHttpRequestData(_context.Object, "", "GET");
        _mockCreateResponse
            .Setup(x => x.CreateHttpResponse(HttpStatusCode.OK, req, It.IsAny<string>()))
            .Returns(_mockHttpResponseData);

        // Act
        var result = await _function.Run(req);
        var serializedData = JsonSerializer.Serialize(expectedData);



        // Assert
        Assert.AreEqual(_mockHttpResponseData, result);
        _mockCreateResponse.Verify(x =>
            x.CreateHttpResponse(HttpStatusCode.OK, req, serializedData), Times.Once);
    }

    [TestMethod]
    public async Task Run_ReturnsOk_WithEmptyLists()
    {
        // Arrange
        _mockGeneCodeClient.Setup(x => x.GetAll()).ReturnsAsync(new List<GeneCodeLkp>());
        _mockRiskReasonClient.Setup(x => x.GetAll()).ReturnsAsync(new List<HigherRiskReferralReasonLkp>());

        var expectedData = new ParticipantReferenceData(
            new Dictionary<string, string>(),
            new Dictionary<string, string>()
        );

        var req  = new MockHttpRequestData(_context.Object, "", "GET");
        _mockCreateResponse
            .Setup(x => x.CreateHttpResponse(HttpStatusCode.OK, req, It.IsAny<string>()))
            .Returns(_mockHttpResponseData);

        // Act
        var result = await _function.Run(req);
        var serializedData = JsonSerializer.Serialize(expectedData);

        // Assert
        Assert.AreEqual(_mockHttpResponseData, result);
        _mockCreateResponse.Verify(x =>
            x.CreateHttpResponse(HttpStatusCode.OK, req, serializedData), Times.Once);
    }

    [TestMethod]
    public async Task Run_ReturnsInternalServerError_OnException()
    {
        // Arrange
        _mockGeneCodeClient.Setup(x => x.GetAll()).ThrowsAsync(new Exception("Simulated Failure"));
        var req  = new MockHttpRequestData(_context.Object, "", "GET");

        _mockCreateResponse
            .Setup(x => x.CreateHttpResponse(HttpStatusCode.InternalServerError, req, null))
            .Returns(_mockHttpResponseData);

        // Act
        var result = await _function.Run(req);


        // Assert
        Assert.AreEqual(_mockHttpResponseData, result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}
