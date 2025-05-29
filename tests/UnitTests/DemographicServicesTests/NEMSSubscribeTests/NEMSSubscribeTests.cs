namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using DataServices.Client;
using NHS.CohortManager.DemographicServices;
using NHS.CohortManager.Tests.TestUtils;
using System.Data;

[TestClass]
public class NEMSSubscribeTests : DatabaseTestBaseSetup<NEMSSubscribe>
{
    private static readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private static readonly Mock<ICreateResponse> _response = new();
    private static readonly Mock<IDataServiceClient<NemsSubscription>> _nemsSubscriptionClient = new();
    private static readonly Mock<IOptions<NEMSSubscribeConfig>> _config = new();
    private readonly string _validNhsNumber = "3112728165";

    public NEMSSubscribeTests() : base((conn, logger, transaction, command, response) =>
    new NEMSSubscribe(
        logger,
        _nemsSubscriptionClient.Object,
        _httpClientFunction.Object,
        response,
        _config.Object))
    {
        CreateHttpResponseMock();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        var testConfig = new NEMSSubscribeConfig
        {
            RetrievePdsDemographicURL = "RetrievePdsDemographicURL"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        _httpClientFunction.Reset();
        _nemsSubscriptionClient.Reset();

        _service = new NEMSSubscribe(
            _loggerMock.Object,
            _nemsSubscriptionClient.Object,
            _httpClientFunction.Object,
            _createResponseMock.Object,
            _config.Object);

        _request = SetupRequest(string.Empty);
    }


    [TestMethod]
    public async Task Run_ValidNhsNumberforNemsSubscribe_ReturnsOk()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });

        _httpClientFunction.Setup(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        _nemsSubscriptionClient.Setup(x => x.Add(It.IsAny<NemsSubscription>())).ReturnsAsync(true);

        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        _nemsSubscriptionClient.Verify(x => x.Add(It.IsAny<NemsSubscription>()), Times.Once());
        Assert.AreEqual(HttpStatusCode.OK, result?.StatusCode);
    }

    [DataRow("")]
    [DataRow(null)]
    [DataRow("0000000000")]
    [TestMethod]
    public async Task Run_InvalidNhsNumberforNemsSubscribe_ReturnsInternalServerError(string invalidNhsNumber)
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", invalidNhsNumber } });
        _httpClientFunction.Setup(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _nemsSubscriptionClient.Setup(x => x.Add(It.IsAny<NemsSubscription>())).ReturnsAsync(true);
        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        Assert.AreEqual(HttpStatusCode.InternalServerError, result?.StatusCode);
    }

    [TestMethod]
    public async Task Run_DatabaseSaveFailsforNemsSubscribe_ReturnsInternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _nemsSubscriptionClient.Setup(x => x.Add(It.IsAny<NemsSubscription>())).ReturnsAsync(false);
        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        _nemsSubscriptionClient.Verify(x => x.Add(It.IsAny<NemsSubscription>()), Times.Never());
        Assert.AreEqual(HttpStatusCode.InternalServerError, result?.StatusCode);
    }

    [TestMethod]
    public async Task Run_NemsApiCallFailsforNemsSubscribe_ReturnsInternalServerError()
    {
        // Arrange
        SetupRequestWithQueryParams(new Dictionary<string, string> { { "nhsNumber", _validNhsNumber } });
        _httpClientFunction.Setup(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        _nemsSubscriptionClient.Setup(x => x.Add(It.IsAny<NemsSubscription>())).ReturnsAsync(true);
        // Act
        var result = await _service.Run(_request.Object);

        // Assert
        _httpClientFunction.Verify(x => x.PostNemsGet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        Assert.AreEqual(HttpStatusCode.InternalServerError, result?.StatusCode);
    }

}
