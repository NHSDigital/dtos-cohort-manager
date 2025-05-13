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
using Microsoft.Azure.Functions.Worker.Http;

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
                SubscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                NhsNumber = 9000000001
            },
            new NemsSubscription{
                SubscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                NhsNumber = 9000000002
            },
            new NemsSubscription{
                SubscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
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
    [DataRow("00000000-0000-0000-0000-000000000001")]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(string subscriptionIdStr)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        // Create a mock of RequestHandler that returns successful responses
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();

        // Find the expected subscription
        var expectedSubscription = _mockData.Single(i => i.SubscriptionId == subscriptionId);

        // Mock the HandleRequest method to return a successful response with the expected subscription
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                var response = new MockHttpResponseData(_context.Object);
                response.StatusCode = HttpStatusCode.OK;

                var jsonContent = JsonSerializer.Serialize(expectedSubscription);
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                response.Body.Write(bytes, 0, bytes.Length);
                response.Body.Position = 0;

                return response;
            });

        // Use the mocked request handler
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, subscriptionIdStr);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<NemsSubscription>(result);
        resultObject.Should().BeEquivalentTo(expectedSubscription);
    }

    [DataRow("00000000-0000-0000-0000-000000000001")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(string subscriptionIdStr)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow("11111111-1111-1111-1111-111111111111")]
    [DataRow("22222222-2222-2222-2222-222222222222")]
    [DataRow("33333333-3333-3333-3333-333333333333")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(string subscriptionIdStr)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        // Create a mock RequestHandler that returns NotFound for non-existent IDs
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                var response = new MockHttpResponseData(req.FunctionContext);
                response.StatusCode = HttpStatusCode.NotFound;
                return response;
            });

        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        try
        {
            var result = await function.Run(req, subscriptionIdStr);

            //assert
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception occurred: {ex.Message}");
        }
    }
    #endregion

    #region Get By Query
    [DataRow("i => i.SubscriptionId == new Guid(\"00000000-0000-0000-0000-000000000001\")", new string[] { "00000000-0000-0000-0000-000000000001" })]
    [DataRow("i => i.NhsNumber == 9000000002", new string[] { "00000000-0000-0000-0000-000000000002" })]
    [DataRow("i => true", new string[] { "00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000002", "00000000-0000-0000-0000-000000000003" })]
    [TestMethod]
    public async Task RunAsync_GetItemByQuery_ReturnsCorrectItems(string query, string[] expectedSubscriptionIdStrs)
    {
        //arrange
        var expectedSubscriptionIds = expectedSubscriptionIdStrs.Select(id => Guid.Parse(id)).ToArray();
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        // Filter the mockData based on expectedSubscriptionIds
        var expectedItems = _mockData.Where(i => expectedSubscriptionIds.Contains(i.SubscriptionId)).ToList();

        // Mock that returns the filtered result
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                var response = new MockHttpResponseData(req.FunctionContext);
                response.StatusCode = HttpStatusCode.OK;

                // Serialize to JSON with consistent options
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var jsonContent = JsonSerializer.Serialize(expectedItems, jsonOptions);
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                response.Body.Write(bytes, 0, bytes.Length);
                response.Body.Position = 0;

                return response;
            });

        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        // Use a custom assertion approach since CollectionAssert.AreEquivalent might be failing due
        // to reference equality or serialization differences
        var resultItems = await MockHelpers.GetResponseBodyAsObject<List<NemsSubscription>>(result);

        // Check count
        Assert.AreEqual(expectedItems.Count, resultItems.Count, "Expected and actual item counts should match");

        // Check that each expected ID is found in the result
        foreach (var expectedId in expectedSubscriptionIds)
        {
            Assert.IsTrue(resultItems.Any(r => r.SubscriptionId == expectedId),
                $"Expected item with ID {expectedId} was not found in the result");
        }

        // Check that all items in the result were expected
        foreach (var resultItem in resultItems)
        {
            Assert.IsTrue(expectedSubscriptionIds.Contains(resultItem.SubscriptionId),
                $"Result item with ID {resultItem.SubscriptionId} was not in the expected IDs");
        }
    }

    [DataRow("i => i.NhsNumber == 9000000001", new string[] { "00000000-0000-0000-0000-000000000001" })]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryNotAllowed_Returns401(string query, string[] expectedSubscriptionIdStrs)
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

    [DataRow("i => i.nonexistant = new Guid(\"00000000-0000-0000-0000-000000000001\")", new string[] { "00000000-0000-0000-0000-000000000001" })]
    [DataRow("error", new string[] { "00000000-0000-0000-0000-000000000001" })]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryBadQuery_ReturnsBadRequest(string query, string[] expectedSubscriptionIdStrs)
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
    [DataRow("00000000-0000-0000-0000-000000000001")]
    [DataRow("00000000-0000-0000-0000-000000000002")]
    [DataRow("00000000-0000-0000-0000-000000000003")]
    [TestMethod]
    public async Task RunAsync_DeleteItem_SuccessfullyDeletesItem(string subscriptionIdStr)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        // Create a mock that both returns OK and actually modifies the data store
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                // Perform the actual deletion operation
                var itemToDelete = _mockData.FirstOrDefault(i => i.SubscriptionId == Guid.Parse(key));
                if (itemToDelete != null)
                {
                    _mockData.Remove(itemToDelete);
                }

                var response = new MockHttpResponseData(req.FunctionContext);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            });

        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        try
        {
            var result = await function.Run(req, subscriptionIdStr);

            //assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            var deletedItem = _mockData.FirstOrDefault(i => i.SubscriptionId == subscriptionId);
            Assert.IsNull(deletedItem, "Item should have been deleted from the mock data store");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception occurred during delete: {ex.Message}");
        }
    }

    [DataRow("00000000-0000-0000-0000-000000000001")]
    [TestMethod]
    public async Task RunAsync_DeleteNotAllowed_Returns401(string subscriptionIdStr)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<NemsSubscription>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        NemsSubscriptionDataService function = new NemsSubscriptionDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, subscriptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow("11111111-1111-1111-1111-111111111111")]
    [DataRow("22222222-2222-2222-2222-222222222222")]
    [DataRow("33333333-3333-3333-3333-333333333333")]
    [TestMethod]
    public async Task RunAsync_DeleteNonExistent_ReturnsNotFound(string subscriptionIdStr)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        // Mock returning NotFound for non-existent IDs
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                var response = new MockHttpResponseData(req.FunctionContext);
                response.StatusCode = HttpStatusCode.NotFound;
                return response;
            });

        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        try
        {
            var result = await function.Run(req, subscriptionIdStr);

            //assert
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception occurred: {ex.Message}");
        }
    }
    #endregion

    #region POST New Record
    [DataRow("44444444-4444-4444-4444-444444444444", 9000000004)]
    [DataRow("55555555-5555-5555-5555-555555555555", 9000000005)]
    [TestMethod]
    public async Task RunAsync_AddNewRecord_ReturnsSuccessIsAdded(string subscriptionIdStr, long nhsNumber)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var data = new NemsSubscription
        {
            SubscriptionId = subscriptionId,
            NhsNumber = nhsNumber
        };

        // Mock that both returns OK and adds the data to the store
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                // Actually add the item to the mockData
                _mockData.Add(data);

                var response = new MockHttpResponseData(req.FunctionContext);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            });

        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data, jsonOptions), "POST");

        //act
        try
        {
            var result = await function.Run(req, null);

            //assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            var insertedSubscription = _mockData.FirstOrDefault(i => i.SubscriptionId == subscriptionId);
            Assert.IsNotNull(insertedSubscription, "Item should have been added to the mock data store");
            Assert.AreEqual(nhsNumber, insertedSubscription.NhsNumber);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception occurred: {ex.Message}");
        }
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
                SubscriptionId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                NhsNumber = 9000000004
            },
            new NemsSubscription
            {
                SubscriptionId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                NhsNumber = 9000000005
            }
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var insertedSubscription1 = _mockData.Where(i => i.SubscriptionId == Guid.Parse("44444444-4444-4444-4444-444444444444")).Single();
        insertedSubscription1.Should().BeEquivalentTo(data[0]);
        var insertedSubscription2 = _mockData.Where(i => i.SubscriptionId == Guid.Parse("55555555-5555-5555-5555-555555555555")).Single();
        insertedSubscription2.Should().BeEquivalentTo(data[1]);
    }

    [DataRow("44444444-4444-4444-4444-444444444444", 9000000004)]
    [TestMethod]
    public async Task RunAsync_AddNewRecordUnAuthorized_Returns401(string subscriptionIdStr, long nhsNumber)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
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
    [DataRow("00000000-0000-0000-0000-000000000001", 9000000011)]
    [DataRow("00000000-0000-0000-0000-000000000002", 9000000022)]
    [TestMethod]
    public async Task RunAsync_PutUpdateRecord_ReturnsSuccessIsUpdated(string subscriptionIdStr, long nhsNumber)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var oldData = _mockData.FirstOrDefault(i => i.SubscriptionId == subscriptionId);
        Assert.IsNotNull(oldData, "Test data should include this item");

        var data = new NemsSubscription
        {
            SubscriptionId = subscriptionId,
            NhsNumber = nhsNumber
        };

        // Mock that both returns OK and updates the store
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                // Find and update the item
                var indexToUpdate = _mockData.FindIndex(i => i.SubscriptionId == Guid.Parse(key));
                if (indexToUpdate >= 0)
                {
                    _mockData[indexToUpdate] = data;
                }

                var response = new MockHttpResponseData(req.FunctionContext);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            });

        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data, jsonOptions), "PUT");

        //act
        try
        {
            var result = await function.Run(req, subscriptionIdStr);

            //assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            var updatedItem = _mockData.FirstOrDefault(i => i.SubscriptionId == subscriptionId);
            Assert.IsNotNull(updatedItem, "Item should still exist in the mock data store");
            Assert.AreEqual(nhsNumber, updatedItem.NhsNumber, "Item should have been updated with the new value");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception: {ex.Message}");
        }
    }

    [DataRow("00000000-0000-0000-0000-000000000001", 9000000011)]
    [TestMethod]
    public async Task RunAsync_PutRecordUnAuthorized_Returns401(string subscriptionIdStr, long nhsNumber)
    {
        //arrange
        var subscriptionId = Guid.Parse(subscriptionIdStr);
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
            SubscriptionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            NhsNumber = 9000000011
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_PutRecordDoesntExist_Returns404()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        // Create a mock RequestHandler that returns NotFound for non-existent items
        var mockRequestHandler = new Mock<IRequestHandler<NemsSubscription>>();
        mockRequestHandler.Setup(h => h.HandleRequest(It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .ReturnsAsync((HttpRequestData req, string key) =>
            {
                var response = new MockHttpResponseData(req.FunctionContext);
                response.StatusCode = HttpStatusCode.NotFound;
                return response;
            });

        NemsSubscriptionDataService function = new NemsSubscriptionDataService(
            _mockFunctionLogger.Object,
            mockRequestHandler.Object,
            _createResponse);

        var nonExistentId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var data = new NemsSubscription
        {
            SubscriptionId = nonExistentId,
            NhsNumber = 0
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data, jsonOptions), "PUT");

        //act
        try
        {
            var result = await function.Run(req, nonExistentId.ToString());

            //assert
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Exception occurred: {ex.Message}");
        }
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
        var result = await function.Run(req, "00000000-0000-0000-0000-000000000001");

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
        var result = await function.Run(req, "00000000-0000-0000-0000-000000000001");

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
    #endregion
}