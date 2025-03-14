namespace DataServiceTests;

using ScreeningLkpDataService;
using Common;
using DataServices.Core;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using FluentAssertions;

[TestClass]
public class ScreeningLkpDataServiceTests
{
    private readonly Mock<ILogger<ScreeningLkpDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<ScreeningLkp>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<ScreeningLkp> _dataServiceAccessor;
    private readonly List<ScreeningLkp> _mockData;
    private readonly AuthenticationConfiguration _authenticationConfiguration;

    public ScreeningLkpDataServiceTests()
    {
        _mockData = new List<ScreeningLkp>{
            new ScreeningLkp{
                ScreeningId = 1,
                ScreeningName = "Test screening name 1",
                ScreeningWorkflowId = "1"
            }
       };
        _dataServiceAccessor = new MockDataServiceAccessor<ScreeningLkp>(_mockData);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
    }

    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsOKStatusAndCorrectItem()
    {
        // Arrange
        var _requestHandler = new RequestHandler<ScreeningLkp>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ScreeningLkpDataService function = new ScreeningLkpDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, "1");

        // Assert
        var expectedScreening = _mockData.Single(i => i.ScreeningWorkflowId == "1");
        var resultObject = await MockHelpers.GetResponseBodyAsObject<ScreeningLkp>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedScreening);
    }

    [TestMethod]
    public async Task RunAsync_GetItemByIdWithInvalidMethod_ReturnsInternalServerError()
    {
        // Arrange
        var invalidMethod = string.Empty;
        var _requestHandler = new RequestHandler<ScreeningLkp>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ScreeningLkpDataService function = new ScreeningLkpDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", invalidMethod);

        // Act
        var result = await function.Run(req, "1");

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
}
