namespace NHS.CohortManager.Tests.CreateCohortDistributionTests;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using Model;
using Common;
using Microsoft.Extensions.Logging;
using NHS.CohortManager.CohortDistributionService;
using NHS.CohortManager.CohortDistribution;
using Common.Interfaces;

[TestClass]
public class CreateCohortDistributionTests
{
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ILogger<CreateCohortDistribution>> _logger = new();
    private readonly CreateCohortDistribution _function;
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<HttpRequestData> _request;
    private readonly CreateCohortDistributionRequestBody _requestBody;
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<ICreateCohortDistributionData> _createCohortDistributionData = new();

    public CreateCohortDistributionTests()
    {
        Environment.SetEnvironmentVariable("AllocateScreeningProviderURL", "AllocateScreeningProviderURL");
        Environment.SetEnvironmentVariable("TransformDataServiceURL", "TransformDataServiceURL");

        var screeningService = "BSS";
        var nhsNumber = "1234567890";

        _request = new Mock<HttpRequestData>(_context.Object);

        _requestBody = new CreateCohortDistributionRequestBody(nhsNumber, screeningService);

        _function = new CreateCohortDistribution(_createResponse.Object, _logger.Object, _createCohortDistributionData.Object);

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
    public async Task Run_Should_Return_OK_When_Request_Body_Valid()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_requestBody);
        SetUpRequestBody(json);

        _createCohortDistributionData.Setup(x => x.GetCohortParticipant(It.IsAny<string>())).Returns(new CohortDistributionParticipant());
        _createCohortDistributionData.Setup(x => x.AllocateCohortParticipantServiceProvider(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>())).Returns(Task.FromResult(It.IsAny<string>()));
        _createCohortDistributionData.Setup(x => x.TransformCohortParticipant(It.IsAny<CohortDistributionParticipant>(), It.IsAny<string>())).Returns(Task.FromResult(new CohortDistributionParticipant()));

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("AllocateScreeningProviderURL")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var result = await _function.RunAsync(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    private void SetUpRequestBody(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        _request.Setup(r => r.Body).Returns(bodyStream);
    }
}
