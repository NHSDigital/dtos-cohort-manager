namespace NHS.CohortManager.Tests.ServiceProviderAllocationServiceTests;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.CohortManager.ServiceProviderAllocationService;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class AllocateServiceProviderToParticipantByServiceTests
{

    private readonly Mock<ILogger<AllocateServiceProviderToParticipantByService>> _logger = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> _context = new();
    private Mock<HttpRequestData> _request;
    private readonly ServiceCollection _serviceCollection = new();
    private readonly Mock<ICreateResponse> _response = new();
    private AllocateServiceProviderToParticipantByService _function;
    private AllocationConfigRequestBody _cohortDistributionData;
    private readonly SetupRequest _setupRequest = new();

    public AllocateServiceProviderToParticipantByServiceTests()
    {
        Environment.SetEnvironmentVariable("CreateValidationExceptionURL", "CreateValidationExceptionURL");

        _request = new Mock<HttpRequestData>(_context.Object);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        _context.SetupProperty(c => c.InstanceServices, serviceProvider);

        _function = new AllocateServiceProviderToParticipantByService(_logger.Object, _response.Object, _callFunction.Object);

        _request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var request = new Mock<HttpResponseData>(_context.Object);
            request.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            request.SetupProperty(r => r.StatusCode);
            request.SetupProperty(r => r.Body, new MemoryStream());
            return request.Object;
        });

        _response.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString(responseBody);
            return response;
        });

    }

    [TestMethod]
    public async Task Run_With_Required_Allocation_Information_Should_Return_Success_And_Service_Provider()
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = "1234567890",
            Postcode = "NE63",
            ScreeningService = "BSS"
        };

        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);

        _request = _setupRequest.Setup(allocationData);

        // Act
        var result = await _function.Run(_request.Object);
        string responseBody = await AssertionHelper.ReadResponseBodyAsync(result);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual("BS Select - NE63", responseBody);
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "CreateValidationExceptionURL"), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Run_With_No_Allocation_Information_Should_Return_BadRequest_And_Create_Validation_Entry()
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = "1234567890",
            Postcode = null,
            ScreeningService = null
        };

        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);

        _request = _setupRequest.Setup(allocationData);

        // Act
        var result = await _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "CreateValidationExceptionURL"), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    public async Task Run_With_Missing_Postcode_Should_Return_BadRequest_And_Create_Validation_Entry()
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = "1234567890",
            Postcode = null,
            ScreeningService = "BSS"
        };

        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);

        _request = _setupRequest.Setup(allocationData);

        // Act
        var result = await _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "CreateValidationExceptionURL"), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    public async Task Run_With_Missing_Screening_Service_Should_Return_BadRequest_And_Create_Validation_Entry()
    {
        //Arrange
        _cohortDistributionData = new AllocationConfigRequestBody
        {
            NhsNumber = "1234567890",
            Postcode = "NE63",
            ScreeningService = null
        };

        var allocationData = JsonSerializer.Serialize(_cohortDistributionData);

        _request = _setupRequest.Setup(allocationData);

        // Act
        var result = await _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        _callFunction.Verify(call => call.SendPost(It.Is<string>(s => s == "CreateValidationExceptionURL"), It.IsAny<string>()), Times.Once());
    }

}
