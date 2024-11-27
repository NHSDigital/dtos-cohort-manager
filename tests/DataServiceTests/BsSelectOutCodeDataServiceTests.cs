namespace DataServiceTests;

using BsSelectOutCodeDataService;
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
public class BsSelectOutCodeDataServiceTests
{
    private readonly Mock<ILogger<BsSelectOutCodeDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<BsSelectOutCode>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<BsSelectOutCode> _dataServiceAccessor;
    private readonly List<BsSelectOutCode> _mockData;
    private AuthenticationConfiguration _authenticationConfiguration;

    public BsSelectOutCodeDataServiceTests()
    {


       _mockData = new List<BsSelectOutCode>{
            new BsSelectOutCode{
                Outcode = "AL1",
                BSO = "ELD",
                AuditId = 1,
                AuditCreatedTimeStamp = DateTime.Now,
                AuditLastModifiedTimeStamp = DateTime.Now,
                AuditText = "From PostgreSQL"
            },
            new BsSelectOutCode{
                Outcode = "BN24",
                BSO = "GBR",
                AuditId = 1,
                AuditCreatedTimeStamp = DateTime.Now,
                AuditLastModifiedTimeStamp = DateTime.Now,
                AuditText = "From PostgreSQL"
            },
            new BsSelectOutCode{
                Outcode = "GU10",
                BSO = "HGU",
                AuditId = 1,
                AuditCreatedTimeStamp = DateTime.Now,
                AuditLastModifiedTimeStamp = DateTime.Now,
                AuditText = "From PostgreSQL"
            }
       };
       _dataServiceAccessor = new MockDataServiceAccessor<BsSelectOutCode>(_mockData);

    }
    #region Get Tests
    [TestMethod]
    public async Task RunAsync_GetAllItems_ReturnsAllItems()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        List<BsSelectOutCode> BsSelectOutCodes = await MockHelpers.GetResponseBodyAsObject<List<BsSelectOutCode>>(result);
        BsSelectOutCodes.Should().BeEquivalentTo(_mockData);
    }
    [TestMethod]
    public async Task RunAsync_GetAllItemsNotAllowed_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_GetAllItemsNoData_ReturnsNoContent()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _dataServiceAccessorEmpty = new MockDataServiceAccessor<BsSelectOutCode>(new List<BsSelectOutCode>());
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessorEmpty,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.NoContent,result.StatusCode);
    }
    #endregion
    #region Get By Id

    [DataRow("AL1")]
    [DataRow("BN24")]
    [DataRow("GU10")]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(string Outcode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,Outcode);

        //assert
        var expectedPractice = _mockData.Single(i => i.Outcode == Outcode);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<BsSelectOutCode>(result);

        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPractice);
    }
    [DataRow("AL1")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(string Outcode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,Outcode);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [DataRow("TEST")]
    [DataRow("123")]
    [DataRow("ACB123")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(string Outcode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,Outcode);

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound,result.StatusCode);
    }
    #endregion



   #region Get By Query

    [DataRow("i => i.Outcode == \"AL1\"", new string[] {"AL1"})]
    [DataRow("i => i.BSO == \"GBR\"",new string[] {"BN24"})]
    [DataRow("i => true",new string[] {"AL1","BN24","GU10"})]
    [TestMethod]
    public async Task RunAsync_GetItemByQuery_ReturnsCorrectItems(string query, string[] expectedOutcodes)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");
        req.AddQuery("query",query);
        //act
        var result = await function.Run(req,null);

        //assert
        var expectedPractices = _mockData.Where(i => expectedOutcodes.Contains(i.Outcode));
        var resultObject = await MockHelpers.GetResponseBodyAsObject<List<BsSelectOutCode>>(result);

        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPractices);
    }
    [DataRow("i => i.BSO == \"GBR\"",new string[] {"GU10"})]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryNotAllowed_Returns401(string query, string[] expectedOutcodes)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");
        req.AddQuery("query",query);
        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [DataRow("i => i.nonexistant = \"GU10\"",new string[] {"ABS123"})]
    [DataRow("error",new string[] {"ABS123"})]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryBadQuery_ReturnsBadRequest(string query, string[] expectedPracticeCodes)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");
        req.AddQuery("query",query);
        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest,result.StatusCode);
    }
    #endregion
    #region Deletes

    [DataRow("AL1")]
    [DataRow("BN24")]
    [DataRow("GU10")]
    [TestMethod]
    public async Task RunAsync_DeleteItem_SuccessfullyDeletesItem(string outcode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","DELETE");


        //act
        var result = await function.Run(req,outcode);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        var deletedItem = await _dataServiceAccessor.GetSingle(i => i.Outcode == outcode);
        deletedItem.Should().BeNull();

    }

    [DataRow("AL1")]
    [TestMethod]
    public async Task RunAsync_DeleteNotAllowed_Returns401(string outcode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","DELETE");

        //act
        var result = await function.Run(req,outcode);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [DataRow("TEST")]
    [DataRow("123")]
    [DataRow("ACB123")]
    [TestMethod]
    public async Task RunAsync_DeleteNonExistent_ReturnsNotFound(string outcode)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);

        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","DELETE");

        //act
        var result = await function.Run(req,outcode);

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound,result.StatusCode);
    }
    #endregion
    #region Post New Record
    [DataRow("AB1234","ABC")]
    [DataRow("XY6789","XYZ")]
    [TestMethod]
    public async Task RunAsync_AddNewRecord_ReturnsSuccessIsAdded(string outcode, string bso)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new BsSelectOutCode
        {
            Outcode = outcode,
            BSO = bso,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastModifiedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };


        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"POST");
        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        var insertedPractice = _mockData.Where(i => i.Outcode == outcode).Single();
        insertedPractice.Should().BeEquivalentTo(data);
    }
    [DataRow("AB1234","ABC")]
    [TestMethod]
    public async Task RunAsync_AddNewRecordUnAuthorized_Returns401(string outcode, string bso)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new BsSelectOutCode
        {
            Outcode = outcode,
            BSO = bso,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastModifiedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };

        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"POST");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_AddNewRecordInvalidData_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new{
            id = "123",
            number = 123,
            testing = "This should fail"
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"POST");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest,result.StatusCode);
    }
    #endregion
    #region PUT Requests
    [DataRow("AL1","ABC")]
    [DataRow("BN24","XYZ")]
    [TestMethod]
    public async Task RunAsync_PutUpdateRecord_ReturnsSuccessIsUpdated(string outcode, string bso)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);


        var oldData = _dataServiceAccessor.GetSingle(i =>  i.Outcode == outcode);

        var data = new BsSelectOutCode
        {
            Outcode = outcode,
            BSO = bso,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastModifiedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"PUT");

        //act
        var result = await function.Run(req,outcode);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        var updatedPractice = _mockData.Where(i => i.Outcode == outcode).Single();
        updatedPractice.Should().BeEquivalentTo(data);
        updatedPractice.Should().NotBeEquivalentTo(oldData);
    }
    [DataRow("AL1","ABC")]
    [TestMethod]
    public async Task RunAsync_PutRecordUnAuthorized_Returns401(string outcode, string bso)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new BsSelectOutCode
        {
            Outcode = outcode,
            BSO = bso,
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastModifiedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"PUT");

        //act
        var result = await function.Run(req,outcode);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_PutRecordNoSlug_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new BsSelectOutCode
        {
            Outcode = "GU1",
            BSO = "XYZ",
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastModifiedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"PUT");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest,result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_PutRecordDoesntExist_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new BsSelectOutCode
        {
            Outcode = "GU1",
            BSO = "XYZ",
            AuditId = 1,
            AuditCreatedTimeStamp = DateTime.Now,
            AuditLastModifiedTimeStamp = DateTime.Now,
            AuditText = "From PostgreSQL"
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"PUT");

        //act
        var result = await function.Run(req,"ABC1234");

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound,result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_PutRecordInvalidData_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<BsSelectOutCode>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        BsSelectOutCodeDataService function = new BsSelectOutCodeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new{
            id = "123",
            number = 123,
            testing = "This should fail"
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"PUT");

        //act
        var result = await function.Run(req,"BN24");

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest,result.StatusCode);
    }

    #endregion


}
