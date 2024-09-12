namespace NHS.CohortManager.Tests.CreateCohortDistributionTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Common;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistributionService;
using NHS.CohortManager.CohortDistribution;
using NHS.CohortManager.Tests.TestUtils;
using Model;
using Model.Enums;
using Data.Database;

[TestClass]
public class CreateCohortDistributionTests
{
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ILogger<CreateCohortDistribution>> _logger = new();
    private readonly Mock<ICohortDistributionHelper> _CohortDistributionHelper = new();
    private readonly CreateCohortDistribution _function;
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly CreateCohortDistributionRequestBody _requestBody;
    private readonly Mock<IParticipantManagerData> _participantManagerData = new();

    public CreateCohortDistributionTests()
    {
        Environment.SetEnvironmentVariable("RetrieveParticipantDataURL", "RetrieveParticipantDataURL");
        Environment.SetEnvironmentVariable("AllocateScreeningProviderURL", "AllocateScreeningProviderURL");
        Environment.SetEnvironmentVariable("TransformDataServiceURL", "TransformDataServiceURL");
        Environment.SetEnvironmentVariable("AddCohortDistributionURL", "AddCohortDistributionURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new CreateCohortDistributionRequestBody()
        {
            NhsNumber = "1234567890",
            ScreeningService = "BSS"
        };

        _function = new CreateCohortDistribution(_createResponse.Object, _logger.Object, _callFunction.Object, _CohortDistributionHelper.Object, _exceptionHandler.Object, _participantManagerData.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(_context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Invalid request body")]
    public async Task RunAsync_BadRequestBody_ReturnsBadRequest(string requestBody)
    {
        SetUpRequestBody(requestBody);
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    [DataRow(null, "BSS")]
    [DataRow("1234567890", null)]
    public async Task RunAsync_MissingFieldsOnRequestBody_ReturnsBadRequest(string nhsNumber, string screeningService)
    {
        // Arrange
        _requestBody.NhsNumber = nhsNumber;
        _requestBody.ScreeningService = screeningService;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_RetrieveParticipantDataRequestFails_ReturnsBadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _CohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Throws(new Exception("some error"));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_AllocateServiceProviderToParticipantRequestFails_ReturnsBadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        _CohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("some error"));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_TransformDataServiceRequestFails_ReturnsBadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        _CohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_AddCohortDistributionRequestFails_ReturnsBadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("AddCohortDistributionURL")), It.IsAny<string>()))
            .Throws(new Exception("an error happened"));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_AllSuccessfulRequests_ReturnsOK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _CohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _CohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _CohortDistributionHelper.Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(false));
        _CohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _CohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));

        ParticipantException(false);

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(response));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_ParticipantExceptionExpected_ReturnsOK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _CohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _CohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _CohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("AddCohortDistributionURL")), It.IsAny<string>())).Verifiable();

        ParticipantException(true);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "AddCohortDistributionURL"), It.IsAny<string>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }

    private void ParticipantException(bool hasException)
    {
        var participant = new Participant() { ExceptionFlag = hasException ? Exists.Yes.ToString() : Exists.No.ToString() };
        _participantManagerData.Setup(x => x.GetParticipant(It.IsAny<string>(),It.IsAny<string>())).Returns(participant);
    }
}
