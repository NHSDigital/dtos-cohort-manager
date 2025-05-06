namespace NHS.CohortManager.Tests.UnitTests.NEMSSubscribeTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Task = System.Threading.Tasks.Task;
using Hl7.Fhir.Model;
using System.IO;
using System.Text;
using Microsoft.Extensions.Options;
using System.Collections.Specialized;
using System.Collections.Generic;
using System;
using Model;
using Common;
using DataServices.Client;
using NHS.CohortManager.DemographicServices;
using NHS.Screening.NEMSSubscribe;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class NEMSSubscribeTests
{
    private Mock<ILogger<NEMSSubscribe>> _loggerMock;
    private Mock<IDataServiceClient<NemsSubscription>> _dataServiceClientMock;
    private Mock<IHttpClientFunction> _httpClientMock;
    private Mock<ICreateResponse> _createResponseMock;
    private Mock<HttpRequestData> _httpRequestMock;
    private Mock<HttpResponseData> _httpResponseMock;
    private NEMSSubscribeConfig _config;

    private NEMSSubscribe _function;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<NEMSSubscribe>>();
        _dataServiceClientMock = new Mock<IDataServiceClient<NemsSubscription>>();
        _httpClientMock = new Mock<IHttpClientFunction>();
        _createResponseMock = new Mock<ICreateResponse>();
        _httpRequestMock = new Mock<HttpRequestData>();
        _httpResponseMock = new Mock<HttpResponseData>();

        _config = new NEMSSubscribeConfig
        {
            RetrievePdsDemographicURL = "http://fake-pds-url",
            NEMS_FHIR_ENDPOINT = "http://nems-fhir",
            SPINE_ACCESS_TOKEN = "access_token",
            FROM_ASID = "from_asid",
            TO_ASID = "to_asid",
            CALLBACK_ENDPOINT = "http://callback",
            CALLBACK_AUTH_TOKEN = "callback_auth",
            Subscription_Criteria = "https://fhir.nhs.uk/Id/nhs-number",
            Subscription_Profile = "https://fhir.nhs.uk/StructureDefinition/Subscription"
        };

        var configOptions = Options.Create(_config);

        _function = new NEMSSubscribe(
            _loggerMock.Object,
            _dataServiceClientMock.Object,
            _httpClientMock.Object,
            _createResponseMock.Object,
            configOptions
        );
    }

    [TestMethod]
    public async Task Run_ValidNhsNumberExistsInPds_AllStepsSucceed()
    {
        // Arrange
        string nhsNumber = "9999999999";

        // Prepare the query collection mock to simulate incoming request with NHS number
        var queryCollection = new NameValueCollection { { "nhsNumber", nhsNumber } };
        _httpRequestMock.Setup(r => r.Query).Returns(queryCollection);

        _httpClientMock.Setup(c => c.SendGetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                       .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Prepare mock response for the subscription creation on NEMS
        var fakeHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{ \"id\": \"123\" }", Encoding.UTF8, "application/json")
        };

        _httpClientMock.Setup(c => c.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(fakeHttpResponseMessage);

        // Mock the database save method
        _dataServiceClientMock.Setup(d => d.Add(It.IsAny<NemsSubscription>())).ReturnsAsync(true);

        // Mock the HTTP response response with the subscription ID
        _createResponseMock.Setup(r => r.CreateHttpResponse(HttpStatusCode.OK, _httpRequestMock.Object, "123"))
                           .Returns(_httpResponseMock.Object);

        // Act
        var result = await _function.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(_httpResponseMock.Object, result);
    }

    [TestMethod]
    public async Task Run_PdsValidationFails_ReturnsBadRequest()
    {
        // Arrange
        string nhsNumber = "9999999999";

        var queryCollection = new NameValueCollection { { "nhsNumber", nhsNumber } };
        _httpRequestMock.Setup(r => r.Query).Returns(queryCollection);

        _createResponseMock.Setup(r => r.CreateHttpResponse(HttpStatusCode.BadRequest, _httpRequestMock.Object, null))
                           .Returns(_httpResponseMock.Object);

        var mockFunction = new Mock<NEMSSubscribe>(
            _loggerMock.Object,
            _dataServiceClientMock.Object,
            _httpClientMock.Object,
            _createResponseMock.Object,
            Options.Create(_config)
        );

        // Mock ValidateAgainstPds
        mockFunction.Setup(f => f.ValidateAgainstPds(It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await mockFunction.Object.Run(_httpRequestMock.Object);

        // Assert
        Assert.AreEqual(_httpResponseMock.Object, result);
    }



    [TestMethod]
    public async Task Run_WhenNemsApiCallFails_ReturnsInternalServerError()
    {
        string nhsNumber = "9999999999";

        var queryCollection = new NameValueCollection { { "nhsNumber", nhsNumber } };
        _httpRequestMock.Setup(r => r.Query).Returns(queryCollection);

        _httpClientMock.Setup(c => c.SendGetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                       .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _httpClientMock.Setup(c => c.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        _createResponseMock.Setup(r => r.CreateHttpResponse(HttpStatusCode.InternalServerError, _httpRequestMock.Object, "Failed to create subscription in NEMS."))
                           .Returns(_httpResponseMock.Object);

        var result = await _function.Run(_httpRequestMock.Object);

        Assert.AreEqual(_httpResponseMock.Object, result);
    }

    [TestMethod]
    public async Task Run_WhenDatabaseSaveFails_ReturnsInternalServerError()
    {
        string nhsNumber = "9999999999";

        var queryCollection = new NameValueCollection { { "nhsNumber", nhsNumber } };
        _httpRequestMock.Setup(r => r.Query).Returns(queryCollection);

        _httpClientMock.Setup(c => c.SendGetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                       .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var fakeHttpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{ \"id\": \"123\" }", Encoding.UTF8, "application/json")
        };
        _httpClientMock.Setup(c => c.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                       .ReturnsAsync(fakeHttpResponse);

        // Mock Add for saving subscription in database (this will return false to simulate a failure)
        _dataServiceClientMock.Setup(d => d.Add(It.IsAny<NemsSubscription>()))
                              .ReturnsAsync(false);

        _createResponseMock.Setup(r => r.CreateHttpResponse(HttpStatusCode.InternalServerError, _httpRequestMock.Object, "Subscription created but failed to store locally."))
                           .Returns(_httpResponseMock.Object);

        var result = await _function.Run(_httpRequestMock.Object);
        Assert.AreEqual(_httpResponseMock.Object, result);
    }

}
