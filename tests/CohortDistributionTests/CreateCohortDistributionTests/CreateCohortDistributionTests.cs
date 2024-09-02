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

        _function = new CreateCohortDistribution(_createResponse.Object, _logger.Object, _callFunction.Object, _CohortDistributionHelper.Object, _exceptionHandler.Object);

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
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Empty()
    {
        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Body_Invalid()
    {
        // Arrange
        SetUpRequestBody("Invalid request body");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Missing_ScreeningService()
    {
        // Arrange
        _requestBody.ScreeningService = null;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_Request_Missing_NhsNumber()
    {
        // Arrange
        _requestBody.NhsNumber = null;
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_BadRequest_When_RetrieveParticipantData_Fails()
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
    public async Task Run_Should_Return_BadRequest_When_AllocateServiceProviderToParticipant_Fails()
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
    public async Task Run_Should_Return_BadRequest_When_TransformDataService_Fails()
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
    public async Task Run_Should_Return_BadRequest_When_AddCohortDistribution_Fails()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);
        FailedFunctionRequest("AddCohortDistributionURL");

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_Should_Return_Created_When_All_Requests_Are_Successful()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _CohortDistributionHelper.Setup(x => x.TransformParticipantAsync(It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _CohortDistributionHelper.Setup(x => x.RetrieveParticipantDataAsync(It.IsAny<CreateCohortDistributionRequestBody>())).Returns(Task.FromResult(new CohortDistributionParticipant()));
        _CohortDistributionHelper.Setup(x => x.ValidateCohortDistributionRecordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CohortDistributionParticipant>())).Returns(Task.FromResult(true));
        _CohortDistributionHelper.Setup(x => x.AllocateServiceProviderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(""));

        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(response));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }

    private void FailedFunctionRequest(string url, string responseData = null)
    {
        var response = MockHelpers.CreateMockHttpResponseData(HttpStatusCode.InternalServerError, responseData);

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains(url)), It.IsAny<string>()))
            .Throws(new Exception("an error happened"));
    }
}
