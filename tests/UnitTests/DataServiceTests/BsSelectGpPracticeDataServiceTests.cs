namespace DataServiceTests;

using BsSelectGpPracticeDataService;
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
public class BsSelectGpPracticeDataServiceTests
{
    private readonly Mock<ILogger<BsSelectGpPracticeDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<BsSelectGpPractice>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<BsSelectGpPractice> _dataServiceAccessor;
    private readonly List<BsSelectGpPractice> _mockData;
    private AuthenticationConfiguration _authenticationConfiguration;

    public BsSelectGpPracticeDataServiceTests()
    {


        _mockData = new List<BsSelectGpPractice>{
            new BsSelectGpPractice{
                GpPracticeCode = "G82650",
                BsoCode = "GCT",
                CountryCategory = "ENGLAND",
                AuditId = 1,
                AuditCreatedTimeStamp = DateTime.Now,
                AuditLastUpdatedTimeStamp = DateTime.Now,
                AuditText = "From PostgreSQL"
            },
            new BsSelectGpPractice{
                GpPracticeCode = "E85121",
                BsoCode = "ECX",
                CountryCategory = "ENGLAND",
                AuditId = 1,
                AuditCreatedTimeStamp = DateTime.Now,
                AuditLastUpdatedTimeStamp = DateTime.Now,
                AuditText = "From PostgreSQL"
            },
            new BsSelectGpPractice{
                GpPracticeCode = "F83043",
                BsoCode = "FLO",
                CountryCategory = "ENGLAND",
                AuditId = 1,
                AuditCreatedTimeStamp = DateTime.Now,
                AuditLastUpdatedTimeStamp = DateTime.Now,
                AuditText = "From PostgreSQL"
            }
       };
        _dataServiceAccessor = new MockDataServiceAccessor<BsSelectGpPractice>(_mockData);

    }
    #region Get Tests
    [TestMethod]
    public async Task RunAsync_GetAllItems_ReturnsAllItems()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        List<BsSelectGpPractice> bsSelectGpPractices = await MockHelpers.GetResponseBodyAsObject<List<BsSelectGpPractice>>(result);
        bsSelectGpPractices.Should().BeEquivalentTo(_mockData);
    }
    [TestMethod]
    public async Task RunAsync_GetAllItemsNotAllowed_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
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
        var _dataServiceAccessorEmpty = new MockDataServiceAccessor<BsSelectGpPractice>(new List<BsSelectGpPractice>());
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessorEmpty, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
    }
    #endregion
    #region Get By Id

    [DataRow("G82650")]
    [DataRow("E85121")]
    [DataRow("F83043")]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(string gpPracticeCode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        var expectedPractice = _mockData.Single(i => i.GpPracticeCode == gpPracticeCode);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<BsSelectGpPractice>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPractice);
    }
    [DataRow("F83043")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(string gpPracticeCode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }
    [DataRow("TEST")]
    [DataRow("123")]
    [DataRow("ACB123")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(string gpPracticeCode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");

        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion



    #region Get By Query

    [DataRow("i => i.GpPracticeCode == \"G82650\"", new string[] { "G82650" })]
    [DataRow("i => i.BsoCode == \"ECX\"", new string[] { "E85121" })]
    [DataRow("i => true", new string[] { "E85121", "G82650", "F83043" })]
    [TestMethod]
    public async Task RunAsync_GetItemByQuery_ReturnsCorrectItems(string query, string[] expectedPracticeCodes)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        //act
        var result = await function.Run(req, null);

        //assert
        var expectedPractices = _mockData.Where(i => expectedPracticeCodes.Contains(i.GpPracticeCode));
        var resultObject = await MockHelpers.GetResponseBodyAsObject<List<BsSelectGpPractice>>(result);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPractices);
    }
    [DataRow("i => i.BsoCode == \"ECX\"", new string[] { "E85121" })]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryNotAllowed_Returns401(string query, string[] expectedPracticeCodes)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }
    [DataRow("i => i.nonexistant = \"ECX\"", new string[] { "E85121" })]
    [DataRow("error", new string[] { "E85121" })]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryBadQuery_ReturnsBadRequest(string query, string[] expectedPracticeCodes)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    [DataRow("i => i.CountryCategory = \"ENGLAND\"")]
    [TestMethod]
    public async Task RunAsyncGetItemByQueryExpectsSingle_ReturnsBadRequest(string query)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "GET");
        req.AddQuery("query", query);
        req.AddQuery("single","true");
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
    #endregion
    #region Deletes

    [DataRow("G82650")]
    [DataRow("E85121")]
    [DataRow("F83043")]
    [TestMethod]
    public async Task RunAsync_DeleteItem_SuccessfullyDeletesItem(string gpPracticeCode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");


        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var deletedItem = await _dataServiceAccessor.GetSingle(i => i.GpPracticeCode == gpPracticeCode);
        deletedItem.Should().BeNull();

    }

    [DataRow("F83043")]
    [TestMethod]
    public async Task RunAsync_DeleteNotAllowed_Returns401(string gpPracticeCode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }
    [DataRow("TEST")]
    [DataRow("123")]
    [DataRow("ACB123")]
    [TestMethod]
    public async Task RunAsync_DeleteNonExistent_ReturnsNotFound(string gpPracticeCode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);

        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);
        var req = new MockHttpRequestData(_context.Object, "", "DELETE");

        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    #endregion
    #region POST New Record
    [DataRow("AB1234", "ABC", "ENGLAND")]
    [DataRow("XY6789", "XYZ", "ENGLAND")]
    [TestMethod]
    public async Task RunAsync_AddNewRecord_ReturnsSuccessIsAdded(string gpPracticeCode, string bsoCode, string countryCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new BsSelectGpPractice
        {
            GpPracticeCode = gpPracticeCode,
            BsoCode = bsoCode,
            CountryCategory = countryCategory,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };


        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var insertedPractice = _mockData.Where(i => i.GpPracticeCode == gpPracticeCode).Single();
        insertedPractice.Should().BeEquivalentTo(data);
    }
    [TestMethod]
    public async Task RunAsync_AddArrayNewRecord_ReturnsSuccessIsAdded()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new List<BsSelectGpPractice>{ new BsSelectGpPractice
        {
            GpPracticeCode = "ABC123",
            BsoCode = "ABC",
            CountryCategory = "ENGLAND",
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        },
        new BsSelectGpPractice
        {
            GpPracticeCode = "XYZ987",
            BsoCode = "XYZ",
            CountryCategory = "ENGLAND",
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        }
        }
        ;


        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "POST");
        //act
        var result = await function.Run(req, null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var insertedPractice = _mockData.Where(i => i.GpPracticeCode == "ABC123").Single();
        insertedPractice.Should().BeEquivalentTo(data[0]);
        var insertedPractice2 = _mockData.Where(i => i.GpPracticeCode == "XYZ987").Single();
        insertedPractice2.Should().BeEquivalentTo(data[1]);
    }
    [DataRow("XY6789", "XYZ", "ENGLAND")]
    [TestMethod]
    public async Task RunAsync_AddNewRecordUnAuthorized_Returns401(string gpPracticeCode, string bsoCode, string countryCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new BsSelectGpPractice
        {
            GpPracticeCode = gpPracticeCode,
            BsoCode = bsoCode,
            CountryCategory = countryCategory,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
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

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

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
    [DataRow("G82650", "ABC", "ENGLAND")]
    [DataRow("E85121", "XYZ", "ENGLAND")]
    [TestMethod]
    public async Task RunAsync_PutUpdateRecord_ReturnsSuccessIsUpdated(string gpPracticeCode, string bsoCode, string countryCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);


        var oldData = _dataServiceAccessor.GetSingle(i => i.GpPracticeCode == gpPracticeCode);

        var data = new BsSelectGpPractice
        {
            GpPracticeCode = gpPracticeCode,
            BsoCode = bsoCode,
            CountryCategory = countryCategory,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        var updatedPractice = _mockData.Where(i => i.GpPracticeCode == gpPracticeCode).Single();
        updatedPractice.Should().BeEquivalentTo(data);
        updatedPractice.Should().NotBeEquivalentTo(oldData);
    }
    [DataRow("G82650", "XYZ", "ENGLAND")]
    [TestMethod]
    public async Task RunAsync_PutRecordUnAuthorized_Returns401(string gpPracticeCode, string bsoCode, string countryCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new BsSelectGpPractice
        {
            GpPracticeCode = gpPracticeCode,
            BsoCode = bsoCode,
            CountryCategory = countryCategory,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, gpPracticeCode);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_PutRecordNoSlug_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new BsSelectGpPractice
        {
            GpPracticeCode = "G82650",
            BsoCode = "XYZ",
            CountryCategory = "ENGLAND",
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
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

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new BsSelectGpPractice
        {
            GpPracticeCode = "G82650",
            BsoCode = "XYZ",
            CountryCategory = "ENGLAND",
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastUpdatedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, "ABC1234");

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_PutRecordInvalidData_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler = new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor, _mockRequestHandlerLogger.Object, _authenticationConfiguration);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object, _requestHandler, _createResponse);

        var data = new
        {
            id = "123",
            number = 123,
            testing = "This should fail"
        };
        var req = new MockHttpRequestData(_context.Object, JsonSerializer.Serialize(data), "PUT");

        //act
        var result = await function.Run(req, "G82650");

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }

    #endregion


}
