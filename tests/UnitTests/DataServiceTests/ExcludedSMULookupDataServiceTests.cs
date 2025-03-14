namespace DataServiceTests;

using ExcludedSMULookupDataService;
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
public class ExcludedSMULookupDataServiceTests
{
    private readonly Mock<ILogger<ExcludedSMULookupDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<ExcludedSMULookup>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<ExcludedSMULookup> _dataServiceAccessor;
    private readonly List<ExcludedSMULookup> _mockData;
    private AuthenticationConfiguration _authenticationConfiguration;

    public ExcludedSMULookupDataServiceTests()
    {
        _mockData = new List<ExcludedSMULookup>{
            new ExcludedSMULookup{
                GpPracticeCode = "ABC"
            },
            new ExcludedSMULookup{
                GpPracticeCode = "DEF"
            }
       };
        _dataServiceAccessor = new MockDataServiceAccessor<ExcludedSMULookup>(_mockData);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
    }

    #region Get By Id
    [DataRow("ABC")]
    [DataRow("DEF")]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(string gpPracticeCode)
    {
        // Arrange
        var _requestHandler = new RequestHandler<ExcludedSMULookup>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExcludedSMULookupDataService function = new ExcludedSMULookupDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, gpPracticeCode);

        // Assert
        var expectedPractice = _mockData.Single(i => i.GpPracticeCode == gpPracticeCode);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<ExcludedSMULookup>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPractice);
    }

    [DataRow("ABC")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(string gpPracticeCode)
    {
        // Arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<ExcludedSMULookup>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExcludedSMULookupDataService function = new ExcludedSMULookupDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, gpPracticeCode);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow("GHI")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(string gpPracticeCode)
    {
        // Arrange
        var _requestHandler = new RequestHandler<ExcludedSMULookup>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExcludedSMULookupDataService function = new ExcludedSMULookupDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, gpPracticeCode);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion

    #region Exception
    [TestMethod]
    public async Task RunAsync_GetItemByIdWithInvalidMethod_ReturnsInternalServerError()
    {
        // Arrange
        var invalidMethod = string.Empty;
        var _requestHandler = new RequestHandler<ExcludedSMULookup>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExcludedSMULookupDataService function = new ExcludedSMULookupDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", invalidMethod);

        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
    #endregion
}
