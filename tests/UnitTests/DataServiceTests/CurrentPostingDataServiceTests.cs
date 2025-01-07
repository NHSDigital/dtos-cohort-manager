namespace DataServiceTests;

using CurrentPostingDataService;
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
public class CurrentPostingDataServiceTests
{
    private readonly Mock<ILogger<CurrentPostingDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<CurrentPosting>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly MockDataServiceAccessor<CurrentPosting> _dataServiceAccessor;
    private readonly List<CurrentPosting> _mockData;
    private AuthenticationConfiguration _authenticationConfiguration;

    public CurrentPostingDataServiceTests()
    {


       _mockData = new List<CurrentPosting>{
            new CurrentPosting{
                Posting = "BAA",
                InUse = "Y",
                IncludedInCohort = "Y",
                PostingCategory = "ENGLAND"
            },
            new CurrentPosting{
                Posting = "SGA",
                InUse = "Y",
                IncludedInCohort = "N",
                PostingCategory = "WALES"
            },
            new CurrentPosting{
                Posting = "SUN",
                InUse = "Y",
                IncludedInCohort = "Y",
                PostingCategory = "ENGLAND"
            },
       };
       _dataServiceAccessor = new MockDataServiceAccessor<CurrentPosting>(_mockData);

    }
    #region Get Tests
    [TestMethod]
    public async Task RunAsync_GetAllItems_ReturnsAllItems()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        List<CurrentPosting> CurrentPostings = await MockHelpers.GetResponseBodyAsObject<List<CurrentPosting>>(result);
        CurrentPostings.Should().BeEquivalentTo(_mockData);
    }
    [TestMethod]
    public async Task RunAsync_GetAllItemsNotAllowed_Returns401()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
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
        var _dataServiceAccessorEmpty = new MockDataServiceAccessor<CurrentPosting>(new List<CurrentPosting>());
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessorEmpty,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.NoContent,result.StatusCode);
    }
    #endregion
    #region Get By Id

    [DataRow("BAA")]
    [DataRow("SGA")]
    [DataRow("SUN")]
    [TestMethod]
    public async Task RunAsync_GetItemById_ReturnsCorrectItems(string posting)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,posting);

        //assert
        var expectedPractice = _mockData.Single(i => i.Posting == posting);
        var resultObject = await MockHelpers.GetResponseBodyAsObject<CurrentPosting>(result);

        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPractice);
    }
    [DataRow("BAA")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNotAllowed_Returns401(string posting)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,posting);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [DataRow("TEST")]
    [DataRow("123")]
    [DataRow("ACB123")]
    [TestMethod]
    public async Task RunAsync_GetItemByIdNonExistent_ReturnsNotFound(string posting)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,posting);

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound,result.StatusCode);
    }
    #endregion



   #region Get By Query

    [DataRow("i => i.Posting == \"BAA\"", new string[] {"BAA"})]
    [DataRow("i => i.PostingCategory == \"WALES\"",new string[] {"SGA"})]
    [DataRow("i => true",new string[] {"BAA","SGA","SUN"})]
    [TestMethod]
    public async Task RunAsync_GetItemByQuery_ReturnsCorrectItems(string query, string[] expectedPostings)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");
        req.AddQuery("query",query);
        //act
        var result = await function.Run(req,null);

        //assert
        var expectedPosings = _mockData.Where(i => expectedPostings.Contains(i.Posting));
        var resultObject = await MockHelpers.GetResponseBodyAsObject<List<CurrentPosting>>(result);

        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        resultObject.Should().BeEquivalentTo(expectedPosings);
    }
    [DataRow("i => i.Posting == \"BAA\"", new string[] {"BAA"})]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryNotAllowed_Returns401(string query, string[] expectedPostings)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");
        req.AddQuery("query",query);
        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [DataRow("i => i.nonexistant = \"BAA\"",new string[] {"ABS123"})]
    [DataRow("error",new string[] {"ABS123"})]
    [TestMethod]
    public async Task RunAsync_GetItemByQueryBadQuery_ReturnsBadRequest(string query, string[] expectedPostings)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");
        req.AddQuery("query",query);
        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.BadRequest,result.StatusCode);
    }
    #endregion
    #region Deletes

    [DataRow("BAA")]
    [DataRow("SGA")]
    [DataRow("SUN")]
    [TestMethod]
    public async Task RunAsync_DeleteItem_SuccessfullyDeletesItem(string posting)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","DELETE");


        //act
        var result = await function.Run(req,posting);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        var deletedItem = await _dataServiceAccessor.GetSingle(i => i.Posting == posting);
        deletedItem.Should().BeNull();

    }

    [DataRow("BAA")]
    [TestMethod]
    public async Task RunAsync_DeleteNotAllowed_Returns401(string posting)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","DELETE");

        //act
        var result = await function.Run(req,posting);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [DataRow("TEST")]
    [DataRow("123")]
    [DataRow("ACB123")]
    [TestMethod]
    public async Task RunAsync_DeleteNonExistent_ReturnsNotFound(string posting)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);

        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","DELETE");

        //act
        var result = await function.Run(req,posting);

        //assert
        Assert.AreEqual(HttpStatusCode.NotFound,result.StatusCode);
    }
    #endregion
    #region Post New Record
    [DataRow("AAA","Y","Y","ENGLAND")]
    [DataRow("BBB","N","N","WALES")]
    [TestMethod]
    public async Task RunAsync_AddNewRecord_ReturnsSuccessIsAdded(string posting, string inUse, string includedInCohort, string postingCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new CurrentPosting
        {
            Posting = posting,
            InUse = inUse,
            IncludedInCohort = includedInCohort,
            PostingCategory = postingCategory
        };


        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"POST");
        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        var insertedPractice = _mockData.Where(i => i.Posting == posting).Single();
        insertedPractice.Should().BeEquivalentTo(data);
    }
    [DataRow("BBB","N","N","WALES")]
    [TestMethod]
    public async Task RunAsync_AddNewRecordUnAuthorized_Returns401(string posting, string inUse, string includedInCohort, string postingCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new CurrentPosting
        {
            Posting = posting,
            InUse = inUse,
            IncludedInCohort = includedInCohort,
            PostingCategory = postingCategory
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

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

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
    [DataRow("BAA","N","N","WALES")]
    [DataRow("SGA","N","Y","ENGLAND")]
    [TestMethod]
    public async Task RunAsync_PutUpdateRecord_ReturnsSuccessIsUpdated(string posting, string inUse, string includedInCohort, string postingCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);


        var oldData = _dataServiceAccessor.GetSingle(i =>  i.Posting == posting);

        var data = new CurrentPosting
        {
            Posting = posting,
            InUse = inUse,
            IncludedInCohort = includedInCohort,
            PostingCategory = postingCategory
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"PUT");

        //act
        var result = await function.Run(req,posting);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        var updatedPractice = _mockData.Where(i => i.Posting == posting).Single();
        updatedPractice.Should().BeEquivalentTo(data);
        updatedPractice.Should().NotBeEquivalentTo(oldData);
    }
    [DataRow("SGA","N","Y","ENGLAND")]
    [TestMethod]
    public async Task RunAsync_PutRecordUnAuthorized_Returns401(string posting, string inUse, string includedInCohort, string postingCategory)
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new CurrentPosting
        {
            Posting = posting,
            InUse = inUse,
            IncludedInCohort = includedInCohort,
            PostingCategory = postingCategory
        };
        var req = new MockHttpRequestData(_context.Object,JsonSerializer.Serialize(data),"PUT");

        //act
        var result = await function.Run(req,posting);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }
    [TestMethod]
    public async Task RunAsync_PutRecordNoSlug_Returns400()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

        var data = new CurrentPosting
        {
            Posting = "BAA",
            InUse = "Y",
            IncludedInCohort = "Y",
            PostingCategory = "ENGLAND"
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

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var data = new CurrentPosting
        {
            Posting = "BAA",
            InUse = "Y",
            IncludedInCohort = "Y",
            PostingCategory = "ENGLAND"
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

        var _requestHandler =  new RequestHandler<CurrentPosting>(_dataServiceAccessor,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        CurrentPostingDataService function = new CurrentPostingDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);

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
