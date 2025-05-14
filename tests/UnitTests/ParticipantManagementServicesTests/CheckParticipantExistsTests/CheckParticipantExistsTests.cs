namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using System.Text.Json;
using Model;
using NHS.CohortManager.ParticipantManagementService;
using NHS.CohortManager.Tests.TestUtils;
using DataServices.Client;
using System.Linq.Expressions;
using System.Collections.Specialized;
using Grpc.Core;

[TestClass]
public class CheckParticipantExistsTests
{
    private readonly Mock<ILogger<CheckParticipantExists>> _loggerMock = new();
    private readonly Mock<ICreateResponse> _createResponseMock = new();
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _dataServiceMock = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly CheckParticipantExists _sut;


    public CheckParticipantExistsTests()
    {
        _createResponseMock.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(ResponseBody);
                return response;
            });

        _createResponseMock.Setup(x => x.CreateHttpResponseWithBodyAsync(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
            .Returns(async (HttpStatusCode statusCode, HttpRequestData req, string ResponseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await response.WriteStringAsync(ResponseBody);
                return response;
            });
        
        _dataServiceMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
                        .ReturnsAsync(new List<ParticipantManagement> {new ParticipantManagement()});


        _sut = new CheckParticipantExists(_dataServiceMock.Object, _createResponseMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    [DataRow("1234567890", null)]
    [DataRow(null, "1")]
    public async Task Run_NullFields_ReturnBadRequest(string nhsNumber, string screeningId)
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", nhsNumber},
            { "ScreeningId", screeningId}
        };

        request.Setup(r => r.Query).Returns(queryParams);

        // Act
        var response = await _sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_ValidFields_ReturnOk()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "1234567890"},
            { "ScreeningId", "1"}
        };

        request.Setup(r => r.Query).Returns(queryParams);

        // Act
        var response = await _sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_ParticipantNotFound_ReturnNotFound()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "1234567890"},
            { "ScreeningId", "1"}
        };

        request.Setup(r => r.Query).Returns(queryParams);
        _dataServiceMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
                        .ReturnsAsync(new List<ParticipantManagement>());

        // Act
        var response = await _sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_ExceptionThrown_ReturnInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "1234567890"},
            { "ScreeningId", "1"}
        };

        request.Setup(r => r.Query).Returns(queryParams);
        _dataServiceMock.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
                        .Throws(new Exception());

        // Act
        var response = await _sut.Run(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
