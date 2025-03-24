namespace DataServiceTests;

using CohortDistributionDataService;
using Common;
using DataServices.Core;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using FluentAssertions;
using System.Text.Json;

[TestClass]
public class CohortDistributionDataServiceTests
{
    private readonly Mock<ILogger<CohortDistributionDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<CohortDistribution>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<CohortDistribution> _dataServiceAccessor;
    private readonly List<CohortDistribution> _mockData;
    private AuthenticationConfiguration _authenticationConfiguration;

    public CohortDistributionDataServiceTests()
    {
        _mockData = new List<CohortDistribution>{
            new CohortDistribution{
                CohortDistributionId = 1
            },
            new CohortDistribution{
                CohortDistributionId = 2
            },
            new CohortDistribution{
                CohortDistributionId = 3
            }
       };
        _dataServiceAccessor = new MockDataServiceAccessor<CohortDistribution>(_mockData);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
    }

    #region Get Tests
    [TestMethod]
    public async Task RunAsync_GetAllItems_ReturnsAllItems()
    {
        // Arrange
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        List<CohortDistribution> cohortDistributionRecords = await MockHelpers.GetResponseBodyAsObject<List<CohortDistribution>>(result);
        cohortDistributionRecords.Should().BeEquivalentTo(_mockData);
    }
    [TestMethod]
    public async Task RunAsync_GetAllItemsNotAllowed_Returns401()
    {
        // Arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_GetAllItemsNoData_ReturnsNoContent()
    {
        // Arrange
        var _dataServiceAccessorEmpty = new MockDataServiceAccessor<CohortDistribution>(new List<CohortDistribution>());
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessorEmpty, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
    }
    #endregion

    #region Get By Id
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(long cohortDistributionId)
    {
        // Arrange
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, cohortDistributionId.ToString());

        // Assert
        var expectedCohortDistributionRecord = _mockData.Single(i => i.CohortDistributionId == cohortDistributionId);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<CohortDistribution>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedCohortDistributionRecord);
    }

    [DataRow(1)]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(long cohortDistributionId)
    {
        // Arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, cohortDistributionId.ToString());

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow(4)]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(long cohortDistributionId)
    {
        // Arrange
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        // Act
        var result = await function.Run(req, cohortDistributionId.ToString());

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion

    #region Post New Record
    [DataRow(4)]
    [TestMethod]
    public async Task RunAsync_AddNewRecord_ReturnsSuccessIsAdded(long cohortDistributionId)
    {
        // Arrange
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new CohortDistribution
        {
            CohortDistributionId = (int)cohortDistributionId
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var insertedCohortDistributionRecord = _mockData.Where(i => i.CohortDistributionId == cohortDistributionId).Single();
        insertedCohortDistributionRecord.Should().BeEquivalentTo(data);
    }

    [DataRow(4)]
    [TestMethod]
    public async Task RunAsync_AddNewRecordUnAuthorized_Returns401(long cohortDistributionId)
    {
        // Arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new CohortDistribution
        {
            CohortDistributionId = (int)cohortDistributionId
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_AddNewRecordInvalidData_Returns401()
    {
        // Arrange
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new
        {
            testing = "This should fail"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");

        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    #endregion

    #region Exception
    [TestMethod]
    public async Task RunAsync_GetItemByIdWithInvalidMethod_ReturnsInternalServerError()
    {
        // Arrange
        var invalidMethod = string.Empty;
        var _requestHandler = new RequestHandler<CohortDistribution>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        CohortDistributionDataService function = new CohortDistributionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", invalidMethod);

        // Act
        var result = await function.Run(req, "1");

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
    #endregion
}
