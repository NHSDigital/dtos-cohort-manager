namespace DataServiceTests;

using System.Runtime.CompilerServices;
using BsSelectGpPracticeDataService;
using Common;
using DataServices.Core;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using System.Linq.Expressions;
using Microsoft.Azure.Functions.Worker;
using System.Collections;
using FluentAssertions;

[TestClass]
public class BsSelectGpPracticeDataServiceTests
{
    private readonly Mock<ILogger<BsSelectGpPracticeDataService>> _mockFunctionLogger = new();
    private readonly Mock<ILogger<RequestHandler<BsSelectGpPractice>>> _mockRequestHandlerLogger = new();
    private readonly CreateResponse _createResponse = new();
    private readonly Mock<FunctionContext> _context = new();
    private readonly Mock<IDataServiceAccessor<BsSelectGpPractice>> _dataServiceAccessor = new();
    private readonly IRequestHandler<BsSelectGpPractice> _requestHandler;
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

    }
    [TestMethod]
    public async Task RunAsync_GetAllItems_ReturnsAllItems()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.AllowAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor.Object,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        _dataServiceAccessor.Setup(i => i.GetRange(It.IsAny<Expression<Func<BsSelectGpPractice,bool>>>())).ReturnsAsync(_mockData);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode);
        List<BsSelectGpPractice> bsSelectGpPractices = await MockHelpers.GetResponseBodyAsObject<List<BsSelectGpPractice>>(result);
        bsSelectGpPractices.Should().BeEquivalentTo(_mockData);
    }
    [TestMethod]
    public async Task RunAsync_GetAllItemsNotAllowed_Returns403()
    {
        //arrange
        _authenticationConfiguration = DataServiceTestHelper.DenyAllAccessConfig;
        var _requestHandler =  new RequestHandler<BsSelectGpPractice>(_dataServiceAccessor.Object,_mockRequestHandlerLogger.Object,_authenticationConfiguration);
        _dataServiceAccessor.Setup(i => i.GetRange(It.IsAny<Expression<Func<BsSelectGpPractice,bool>>>())).ReturnsAsync(_mockData);
        BsSelectGpPracticeDataService function = new BsSelectGpPracticeDataService(_mockFunctionLogger.Object,_requestHandler,_createResponse);
        var req = new MockHttpRequestData(_context.Object,"","GET");

        //act
        var result = await function.Run(req,null);

        //assert
        Assert.AreEqual(HttpStatusCode.Unauthorized,result.StatusCode);
    }

}
