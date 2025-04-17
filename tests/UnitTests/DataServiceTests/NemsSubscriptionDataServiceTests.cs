namespace DataServiceTests;

using NemsSubscriptionDataService;
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
public class NemsSubscriptionDataServiceTests
{
    private readonly Mock<ILogger<NemsSubscriptionDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<NemsSubscription>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<NemsSubscription> _dataServiceAccessor;
    private readonly List<NemsSubscription> _mockData;
    private AuthenticationConfiguration _authenticationConfiguration;

    public NemsSubscriptionDataServiceTests()
    {
        _mockData = new List<NemsSubscription>{
            new NemsSubscription{
                SubscriptionId = 1001,
                NhsNumber = 9000000001
            },
            new NemsSubscription{
                SubscriptionId = 1002,
                NhsNumber = 9000000002
            },
            new NemsSubscription{
                SubscriptionId = 1003,
                NhsNumber = 9000000003
            }
       };
        _dataServiceAccessor = new MockDataServiceAccessor<NemsSubscription>(_mockData);
    }

    #region Get Tests
    [TestMethod]
    public async Task RunAsync_GetAllItems_ReturnsAllItems()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        List<NemsSubscription> nemsSubscriptions = await MockHelpers.GetResponseBodyAsObject<List<NemsSubscription>>(result);
        nemsSubscriptions.Should().BeEquivalentTo(_mockData);
    }

    [TestMethod]
    public async Task RunAsync_GetAllItemsNotAllowed_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_GetAllItemsNoData_ReturnsNoContent()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _dataServiceAccessorEmpty = new MockDataServiceAccessor<NemsSubscription>(new List<NemsSubscription>());
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessorEmpty, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
    }
    #endregion

    #region Get By Id
    [DataRow(1001)]
    [DataRow(1002)]
    [DataRow(1003)]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(long subscriptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        var expectedSubscription = _mockData.Single(i => i.SubscriptionId == subscriptionId);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<NemsSubscription>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedSubscription);
    }

    [DataRow(1001)]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(long subscriptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow(9999)]
    [DataRow(8888)]
    [DataRow(7777)]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(long subscriptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion

    #region Get By Query
    [DataRow("i => i.SubscriptionId == \"1001\"", new long[] { 1001 })]
    [DataRow("i => i.NhsNumber == 9000000002", new long[] { 1002 })]
    [DataRow("i => true", new long[] { 1001, 1002, 1003 })]
    [TestMethod]
    public async Task RunAsync_GetItemByQuery_ReturnsCorrectItems(string query, long[] expectedSubscriptionIds)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        
        //act
        var result = await function.Run(req, null);

        //assert
        var expectedSubscriptions = _mockData.Where(i => expectedSubscriptionIds.Contains(i.SubscriptionId));
        var resultObject = await MockHelpers.GetResponseBodyAsObject<List<NemsSubscription>>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedSubscriptions);
    }

    [DataRow("i => i.NhsNumber == 9000000001", new long[] { 1001 })]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryNotAllowed_Returns401(string query, long[] expectedSubscriptionIds)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow("i => i.nonexistant = 1001", new long[] { 1001 })]
    [DataRow("error", new long[] { 1001 })]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryBadQuery_ReturnsBadRequest(string query, long[] expectedSubscriptionIds)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [DataRow("i => i.Status = \"Active\"")]
    [TestMethod]
    public async Task RunAsyncGetItemByQueryExpectsSingle_ReturnsBadRequest(string query)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        req.AddQuery("single", "true");
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    #endregion

    #region Deletes
    [DataRow(1001)]
    [DataRow(1002)]
    [DataRow(1003)]
    [TestMethod]
    public async Task RunAsync_DeleteItem_SuccessfullyDeletesItem(long subscriptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var deletedItem = await _dataServiceAccessor.GetSingle(i => i.SubscriptionId == subscriptionId);
        deletedItem.Should().BeNull();
    }

    [DataRow(1001)]
    [TestMethod]
    public async Task RunAsync_DeleteNotAllowed_Returns401(long subscriptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow(9999)]
    [DataRow(8888)]
    [DataRow(7777)]
    [TestMethod]
    public async Task RunAsync_DeleteNonExistent_ReturnsNotFound(long subscriptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion

    #region POST New Record
    [DataRow(1004, 9000000004)]
    [DataRow(1005, 9000000005)]
    [TestMethod]
    public async Task RunAsync_AddNewRecord_ReturnsSuccessIsAdded(long subscriptionId, long nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new NemsSubscription
        {
            SubscriptionId = subscriptionId,
            NhsNumber = nhsNumber
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var insertedSubscription = _mockData.Where(i => i.SubscriptionId == subscriptionId).Single();
        insertedSubscription.Should().BeEquivalentTo(data);
    }

    [TestMethod]
    public async Task RunAsync_AddArrayNewRecord_ReturnsSuccessIsAdded()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new List<NemsSubscription> { 
            new NemsSubscription
            {
                SubscriptionId = 1004,
                NhsNumber = 9000000004
            },
            new NemsSubscription
            {
                SubscriptionId = 1005,
                NhsNumber = 9000000005
            }
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var insertedSubscription1 = _mockData.Where(i => i.SubscriptionId == 1004).Single();
        insertedSubscription1.Should().BeEquivalentTo(data[0]);
        var insertedSubscription2 = _mockData.Where(i => i.SubscriptionId == 1005).Single();
        insertedSubscription2.Should().BeEquivalentTo(data[1]);
    }

    [DataRow(1004, 9000000004)]
    [TestMethod]
    public async Task RunAsync_AddNewRecordUnAuthorized_Returns401(long subscriptionId, long nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new NemsSubscription
        {
            SubscriptionId = subscriptionId,
            NhsNumber = nhsNumber
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_AddNewRecordInvalidData_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new
        {
            id = "123",
            number = 123,
            testing = "This should fail"
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    #endregion

    #region PUT Requests
    [DataRow(1001, 9000000011)]
    [DataRow(1002, 9000000022)]
    [TestMethod]
    public async Task RunAsync_PutUpdateRecord_ReturnsSuccessIsUpdated(long subscriptionId, long nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var oldData = await _dataServiceAccessor.GetSingle(i => i.SubscriptionId == subscriptionId);

        var data = new NemsSubscription
        {
            SubscriptionId = subscriptionId,
            NhsNumber = nhsNumber
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");
        
        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var updatedSubscription = _mockData.Where(i => i.SubscriptionId == subscriptionId).Single();
        updatedSubscription.Should().BeEquivalentTo(data);
        updatedSubscription.Should().NotBeEquivalentTo(oldData);
    }

    [DataRow(1001, 9000000011)]
    [TestMethod]
    public async Task RunAsync_PutRecordUnAuthorized_Returns401(long subscriptionId, long nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new NemsSubscription
        {
            SubscriptionId = subscriptionId,
            NhsNumber = nhsNumber
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");
        
        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_PutRecordNoSlug_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new NemsSubscription
        {
            SubscriptionId = 1001,
            NhsNumber = 9000000011
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");
        
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_PutRecordDoesntExist_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new NemsSubscription
        {
            SubscriptionId = 001,
            NhsNumber = 0
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");
        
        //act
        var result = await function.Run(req, "9999");

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_PutRecordInvalidData_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new
        {
            id = "123",
            number = 123,
            testing = "This should fail"
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");
        
        //act
        var result = await function.Run(req, "1001");

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    #endregion

    #region Exception
    [TestMethod]
    public async Task RunAsync_GetItemByIdWithInvalidMethod_ReturnsInternalServerError()
    {
        // Arrange
        var invalidMethod = string.Empty;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, string.Empty, invalidMethod);

        // Act
        var result = await function.Run(req, "1001");

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
    #endregion
}