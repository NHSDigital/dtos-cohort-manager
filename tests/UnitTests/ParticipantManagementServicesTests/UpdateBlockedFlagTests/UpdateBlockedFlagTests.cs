namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using DataServices.Client;
using System.Linq.Expressions;
using System.Collections.Specialized;
using NHS.CohortManager.ParticipantManagementService;
using RulesEngine.Models;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

[TestClass]
public class UpdateBlockedFlagTests
{
    private readonly UpdateBlockedFlag _sut;
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _mockParticipantManagementClient = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _mockParticipantDemographicClient = new();
    private readonly Mock<ILogger<UpdateBlockedFlag>> _mockUpdateBlockedFlagLogger = new();
    private readonly Mock<ILogger<BlockParticipantHandler>> _mockHandlerLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly BlockParticipantHandler _blockParticipantHandler;
    private readonly Mock<IHttpClientFunction> _mockHttpClient = new();
    private readonly Mock<IOptions<UpdateBlockedFlagConfig>> _mockConfig = new();
    private Mock<HttpRequestData> _request;
    private readonly SetupRequest _setupRequest = new();

    public UpdateBlockedFlagTests()
    {
        _mockConfig.Setup(x => x.Value).Returns(new UpdateBlockedFlagConfig
        {
            ParticipantDemographicDataServiceURL = "participantManagementUrl",
            ParticipantManagementUrl = "ParticipantManagementUrl",
            ExceptionFunctionURL = "ExceptionFunctionUrl",
            ManageNemsSubscriptionSubscribeURL = "NemsSubscribeUrl",
            ManageNemsSubscriptionUnsubscribeURL = "NemsUnsubscribeUrl",
            RetrievePdsDemographicURL = "RetrievePdsDemographicUrl"
        });
        _blockParticipantHandler = new BlockParticipantHandler(_mockHandlerLogger.Object, _mockParticipantManagementClient.Object, _mockParticipantDemographicClient.Object, _mockHttpClient.Object, _mockConfig.Object);
        _sut = new UpdateBlockedFlag(_mockUpdateBlockedFlagLogger.Object, _mockCreateResponse.Object, _blockParticipantHandler);

        _mockCreateResponse.Setup(x => x.CreateHttpResponseWithBodyAsync(
                It.IsAny<HttpStatusCode>(),
                It.IsAny<HttpRequestData>(),
                It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.WriteString(responseBody);
                return Task.FromResult(response);
            });
    }

    [TestMethod]
    public async Task BlockParticipant_ExistingParticipant_ReturnsSuccess()
    {
        //arrange
        var requestBody = new BlockParticipantDto
        {
            NhsNumber = 6427635034,
            FamilyName = "Jones",
            DateOfBirth = "1923-10-12"
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(requestBody));

        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement
            {
                NHSNumber = 6427635034,
                BlockedFlag = 0,

            });
        _mockParticipantDemographicClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()))
            .ReturnsAsync(new ParticipantDemographic
            {
                NhsNumber = 6427635034,
                FamilyName = "Jones",
                DateOfBirth = "19231012"
            });

        _mockParticipantManagementClient.Setup(x => x.Update(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);
        _mockHttpClient.Setup(x => x.SendPost("NemsUnsubscribeUrl", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });


        //act
        var result = await _sut.BlockParticipant(_request.Object);

        //asset
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _mockHttpClient.Verify(x => x.SendPost("NemsUnsubscribeUrl", It.IsAny<Dictionary<string, string>>()), Times.Once);
        _mockParticipantManagementClient.Verify(x => x.Update(It.IsAny<ParticipantManagement>()), Times.Once);
        _mockHttpClient.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task BlockParticipant_NonExistentParticipant_ReturnsSuccess()
    {
        //arrange
        var requestBody = new BlockParticipantDto
        {
            NhsNumber = 6427635034,
            FamilyName = "Jones",
            DateOfBirth = "1923-10-12"
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(requestBody));

        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .Returns(Task.FromResult<ParticipantManagement>(null!));


        var pdsDemoResponse = JsonSerializer.Serialize(
            new ParticipantDemographic
            {
                NhsNumber = 6427635034,
                FamilyName = "Jones",
                DateOfBirth = "19231012"
            });

        _mockHttpClient.Setup(x => x.SendGet("RetrievePdsDemographicUrl", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(pdsDemoResponse);

        _mockParticipantManagementClient.Setup(x => x.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);


        //act
        var result = await _sut.BlockParticipant(_request.Object);

        //asset
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        _mockHttpClient.Verify(x => x.SendGet("RetrievePdsDemographicUrl", It.IsAny<Dictionary<string, string>>()), Times.Once);
        _mockParticipantManagementClient.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()));
        _mockParticipantManagementClient.Verify(x => x.Add(It.IsAny<ParticipantManagement>()), Times.Once);
        _mockParticipantManagementClient.VerifyNoOtherCalls();
        _mockHttpClient.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task BlockParticipant_InvalidNhsNumber_ReturnsFailure()
    {
        //arrange
        var requestBody = new BlockParticipantDto
        {
            NhsNumber = 6427635035,
            FamilyName = "Jones",
            DateOfBirth = "1923-10-12"
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(requestBody));

        //act
        var result = await _sut.BlockParticipant(_request.Object);

        //asset
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    [TestMethod]
    public async Task BlockParticipant_ParticipantAlreadyBlocked_ReturnsFailure()
    {
        //arrange
        var requestBody = new BlockParticipantDto
        {
            NhsNumber = 6427635034,
            FamilyName = "Jones",
            DateOfBirth = "1923-10-12"
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(requestBody));

        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement
            {
                NHSNumber = 6427635034,
                BlockedFlag = 1,

            });

        //act
        var result = await _sut.BlockParticipant(_request.Object);

        //asset
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _mockHttpClient.Verify(x => x.SendPost("NemsUnsubscribeUrl", It.IsAny<Dictionary<string, string>>()), Times.Never);
        _mockHttpClient.VerifyNoOtherCalls();
        _mockParticipantManagementClient.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()), Times.Once);
        _mockParticipantManagementClient.VerifyNoOtherCalls();

    }
    [TestMethod]
    public async Task BlockParticipant_ExistingParticipantFailsThreePointCheck_ReturnsSuccess()
    {
        //arrange
        var requestBody = new BlockParticipantDto
        {
            NhsNumber = 6427635034,
            FamilyName = "Jones",
            DateOfBirth = "1923-10-12"
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(requestBody));

        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .ReturnsAsync(new ParticipantManagement
            {
                NHSNumber = 6427635034,
                BlockedFlag = 0,

            });
        _mockParticipantDemographicClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()))
            .ReturnsAsync(new ParticipantDemographic
            {
                NhsNumber = 6427635034,
                FamilyName = "Davies",
                DateOfBirth = "19231012"
            });

        //act
        var result = await _sut.BlockParticipant(_request.Object);

        //asset
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _mockHttpClient.Verify(x => x.SendPost("NemsUnsubscribeUrl", It.IsAny<Dictionary<string, string>>()), Times.Never);
        _mockHttpClient.VerifyNoOtherCalls();
        _mockParticipantManagementClient.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()), Times.Once);
        _mockParticipantManagementClient.VerifyNoOtherCalls();
        _mockParticipantDemographicClient.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>()), Times.Once);
        _mockParticipantDemographicClient.VerifyNoOtherCalls();
    }
      [TestMethod]
    public async Task BlockParticipant_NonExistentParticipantFailsThreePointCheck_ReturnsFailure()
    {
        //arrange
        var requestBody = new BlockParticipantDto
        {
            NhsNumber = 6427635034,
            FamilyName = "Jones",
            DateOfBirth = "1923-10-12"
        };

        _request = _setupRequest.Setup(JsonSerializer.Serialize(requestBody));

        _mockParticipantManagementClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
            .Returns(Task.FromResult<ParticipantManagement>(null!));


        var pdsDemoResponse = JsonSerializer.Serialize(
            new ParticipantDemographic
            {
                NhsNumber = 6427635034,
                FamilyName = "Davies",
                DateOfBirth = "19231012"
            });

        _mockHttpClient.Setup(x => x.SendGet("RetrievePdsDemographicUrl", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(pdsDemoResponse);

        _mockParticipantManagementClient.Setup(x => x.Add(It.IsAny<ParticipantManagement>()))
            .ReturnsAsync(true);


        //act
        var result = await _sut.BlockParticipant(_request.Object);

        //asset
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _mockHttpClient.Verify(x => x.SendGet("RetrievePdsDemographicUrl", It.IsAny<Dictionary<string, string>>()), Times.Once);
        _mockParticipantManagementClient.Verify(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()));
        _mockParticipantManagementClient.VerifyNoOtherCalls();
        _mockHttpClient.VerifyNoOtherCalls();
    }



}
