namespace DataServiceTests;

using ParticipantManagementDataService;
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
public class ExceptionManagementDataServiceTests
{
    private readonly Mock<ILogger<ExceptionManagementDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<ExceptionManagement>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<ExceptionManagement> _dataServiceAccessor;
    private readonly List<ExceptionManagement> _mockData;
    private AuthenticationConfiguration _authenticationConfiguration;

    public ExceptionManagementDataServiceTests()
    {
        _mockData = new List<ExceptionManagement>{
            new ExceptionManagement{
                ExceptionId = 1,
                NhsNumber = "1",
                FileName = "fileName1"
            },
            new ExceptionManagement{
                ExceptionId = 2,
                NhsNumber = "2",
                FileName = "fileName2"
            },
            new ExceptionManagement{
                ExceptionId = 3,
                NhsNumber = "3",
                FileName = "fileName3"
            },
       };
        _dataServiceAccessor = new MockDataServiceAccessor<ExceptionManagement>(_mockData);
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
    }

    #region Get Tests
    [TestMethod]
    public async Task RunAsync_GetAllItems_ReturnsAllItems()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        List<ExceptionManagement> exceptions = await MockHelpers.GetResponseBodyAsObject<List<ExceptionManagement>>(result);
        exceptions.Should().BeEquivalentTo(_mockData);
    }

    [TestMethod]
    public async Task RunAsync_GetAllItemsNotAllowed_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
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
        var _dataServiceAccessorEmpty = new MockDataServiceAccessor<ExceptionManagement>(new List<ExceptionManagement>());
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessorEmpty, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
    }
    #endregion

    #region Get By Id
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(int exceptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, exceptionId.ToString());

        //assert
        var expectedPractice = _mockData.Single(i => i.ExceptionId == exceptionId);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<ExceptionManagement>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPractice);
    }

    [DataRow(1)]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(int exceptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, exceptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow(4)]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(int exceptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, exceptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion

    #region Get By Query
    [DataRow("i => i.NhsNumber == \"1\"", new string[] { "1" })]
    [TestMethod]
    public async Task RunAsync_GetItemByQuery_ReturnsCorrectItems(string query, string[] expectedExceptions)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);

        //act
        var result = await function.Run(req, null);

        //assert
        var expectedResult = _mockData.Where(i => expectedExceptions.Contains(i.NhsNumber));
        var resultObject = await MockHelpers.GetResponseBodyAsObject<List<ExceptionManagement>>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedResult);
    }

    [DataRow("i => i.NhsNumber == \"1\"")]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryNotAllowed_Returns401(string query)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow("i => i.nonExistant = \"1\"")]
    [DataRow("error")]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryBadQuery_ReturnsBadRequest(string query)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    #endregion

    #region Deletes
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestMethod]
    public async Task RunAsync_DeleteItem_SuccessfullyDeletesItem(int exceptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, exceptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var deletedItem = await _dataServiceAccessor.GetSingle(i => i.ExceptionId == exceptionId);
        deletedItem.Should().BeNull();
    }

    [DataRow(1)]
    [TestMethod]
    public async Task RunAsync_DeleteNotAllowed_Returns401(int exceptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, exceptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [DataRow(4)]
    [TestMethod]
    public async Task RunAsync_DeleteNonExistent_ReturnsNotFound(int exceptionId)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);

        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, exceptionId.ToString());

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion

    #region Post New Record
    [DataRow("fileName4", "4")]
    [TestMethod]
    public async Task RunAsync_AddNewRecord_ReturnsSuccessIsAdded(string fileName, string nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new ExceptionManagement
        {
            FileName = fileName,
            NhsNumber = nhsNumber
        };

        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var insertedPractice = _mockData.Where(i => i.FileName == fileName).Single();
        insertedPractice.Should().BeEquivalentTo(data);
    }

    [DataRow("fileName4", "4")]
    [TestMethod]
    public async Task RunAsync_AddNewRecordUnAuthorized_Returns401(string fileName, string nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;

        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new ExceptionManagement
        {
            FileName = fileName,
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
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

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
    [DataRow("fileName1", "1")]
    [TestMethod]
    public async Task RunAsync_PutUpdateRecord_ReturnsSuccessIsUpdated(string fileName, string nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var oldData = _dataServiceAccessor.GetSingle(i => i.NhsNumber == nhsNumber);

        var data = new ExceptionManagement
        {
            FileName = fileName,
            NhsNumber = nhsNumber
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, nhsNumber);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var updatedPractice = _mockData.Where(i => i.NhsNumber == nhsNumber).Single();
        updatedPractice.Should().BeEquivalentTo(data);
        updatedPractice.Should().NotBeEquivalentTo(oldData);
    }

    [DataRow("fileName1", "1")]
    [TestMethod]
    public async Task RunAsync_PutRecordUnAuthorized_Returns401(string fileName, string nhsNumber)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new ExceptionManagement
        {
            FileName = fileName,
            NhsNumber = nhsNumber
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, fileName);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_PutRecordNoSlug_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new ExceptionManagement
        {
            FileName = "fileName1",
            NhsNumber = "1"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_PutRecordDoesNotExist_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new ExceptionManagement
        {
            FileName = "fileName1",
            NhsNumber = "1"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, "4");

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task RunAsync_PutRecordInvalidData_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new
        {
            id = "123",
            number = 123,
            testing = "This should fail"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, "BN24");

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
        var _requestHandler = new RequestHandler<ExceptionManagement>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        ExceptionManagementDataService function = new ExceptionManagementDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", invalidMethod);

        // Act
        var result = await function.Run(req, null);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }
    #endregion
}
