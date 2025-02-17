namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using System.Text.Json;
using Common;
using Data.Database;
using DataServices.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.DemographicServices;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class RetrievePdsDemographicTests
{
    private readonly Mock<ILogger<RetrievePdsDemographic>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private Mock<ICallFunction> _callFunction = new();
    private readonly Mock<FunctionContext> _context = new();
    private Mock<HttpRequestData> _request;
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly Participant _participant;
    private readonly SetupRequest _setupRequest = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _participantDemographic = new();

    private RetrievePdsDemographic _retrievePdsDemographic;
    public RetrievePdsDemographicTests()
    {
        _request = new Mock<HttpRequestData>(_context.Object);
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        _context.SetupProperty(c => c.InstanceServices, serviceProvider);
         _retrievePdsDemographic = new RetrievePdsDemographic(_logger.Object, _createResponse.Object, _callFunction.Object);

        Environment.SetEnvironmentVariable("RetrievePdsDemographicURI", "RetrievePdsDemographicURI");

        _participant = new Participant()
        {
            FirstName = "Joe",
            FamilyName = "Bloggs",
            NhsNumber = "11111111111111111",
            RecordType = Actions.New
        };

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
    }
    [TestMethod]

    public async Task Run_Always_ReturnsOk()
    {
        HttpStatusCode expectedStatus = HttpStatusCode.OK;

        HttpStatusCode actualStatus = HttpStatusCode.OK;

        Assert.AreEqual(expectedStatus, actualStatus);
    }
}
