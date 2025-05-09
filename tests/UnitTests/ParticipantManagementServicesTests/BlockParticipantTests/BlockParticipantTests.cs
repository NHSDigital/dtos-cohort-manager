namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using DataServices.Client;
using System.Linq.Expressions;
using System.Collections.Specialized;
using NHS.CohortManager.ParticipantManagementService;

[TestClass]
public class BlockParticipantTests
{
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _dsMockParticipantManagement = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _dsMockParticipantDemographic = new();
    private readonly Mock<ILogger<BlockParticipant>> _loggerMock = new();
    private readonly Mock<ICreateResponse> _createResponseMock = new();
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly BlockParticipant _sut;

    public BlockParticipantTests()
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
        
        _dsMockParticipantManagement.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>()))
                        .ReturnsAsync(new ParticipantManagement());                     


        _sut = new BlockParticipant(_dsMockParticipantManagement.Object, _dsMockParticipantDemographic.Object, _loggerMock.Object, _createResponseMock.Object, _exceptionHandler.Object);
    }

    [TestMethod]
    public async Task Run_ValidFields_ReturnOk()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "8253303483"}, //Random NHS number created using https://data-gorilla.uk/en/healthcare/nhs-number/ (No PII used)
            { "ScreeningId", "1"},
            { "DateOfBirth", "01/01/2000"},
            { "LastName", "Smith"}
        };
        _dsMockParticipantDemographic.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(new ParticipantDemographic()); 
        _dsMockParticipantManagement.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).ReturnsAsync(new ParticipantManagement());  
        _dsMockParticipantManagement.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);  

        request.Setup(r => r.Query).Returns(queryParams);

        // Act
        var response = await _sut.Run(request.Object);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_ParticipantDoesNotExistInDemographicTable_ReturnNotFound()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "8253303483"}, //Random NHS number created using https://data-gorilla.uk/en/healthcare/nhs-number/ (No PII used)
            { "ScreeningId", "1"},
            { "DateOfBirth", "01/01/2000"},
            { "LastName", "Smith"}
        };
        
        _dsMockParticipantDemographic.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync((ParticipantDemographic?)null);
        _dsMockParticipantManagement.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).ReturnsAsync(new ParticipantManagement());  
        _dsMockParticipantManagement.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(true);  

        request.Setup(r => r.Query).Returns(queryParams);

        // Act
        var response = await _sut.Run(request.Object);
        
        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.IsAny<NullReferenceException>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_UpdateFails_ReturnInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "8253303483"}, //Random NHS number created using https://data-gorilla.uk/en/healthcare/nhs-number/ (No PII used)
            { "ScreeningId", "1"},
            { "DateOfBirth", "01/01/2000"},
            { "LastName", "Smith"}
        };
        
        _dsMockParticipantDemographic.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(new ParticipantDemographic());
        _dsMockParticipantManagement.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).ReturnsAsync(new ParticipantManagement());  
        _dsMockParticipantManagement.Setup(x => x.Update(It.IsAny<ParticipantManagement>())).ReturnsAsync(false);  

        request.Setup(r => r.Query).Returns(queryParams);

        // Act
        var response = await _sut.Run(request.Object);
        
        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_NoResponseParticipantManagement_ReturnInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "8253303483"}, //Random NHS number created using https://data-gorilla.uk/en/healthcare/nhs-number/ (No PII used)
            { "ScreeningId", "1"},
            { "DateOfBirth", "01/01/2000"},
            { "LastName", "Smith"}
        };

        request.Setup(r => r.Query).Returns(queryParams);

        _dsMockParticipantDemographic.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(new ParticipantDemographic());
        
        //Simulates no response from the Participant management service.
        _dsMockParticipantManagement.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ParticipantManagement, bool>>>())).Throws(new Exception());

        // Act
        var response = await _sut.Run(request.Object);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_NoResponseParticipantDemographic_ReturnNotFound()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "8253303483"}, //Random NHS number created using https://data-gorilla.uk/en/healthcare/nhs-number/ (No PII used)
            { "ScreeningId", "1"},
            { "DateOfBirth", "01/01/2000"},
            { "LastName", "Smith"}
        };

        request.Setup(r => r.Query).Returns(queryParams);

        //Simulates no response from the Participant demographic service.
        _dsMockParticipantDemographic.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).Throws(new Exception());

        // Act
        var response = await _sut.Run(request.Object);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.IsAny<NullReferenceException>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Run_InvalidNHSNumber_ReturnInternalServerError()
    {
        // Arrange
        var request = _setupRequest.Setup("");
        var queryParams = new NameValueCollection
        {
            { "NhsNumber", "1234567890"},
            { "ScreeningId", "1"},
            { "DateOfBirth", "01/01/2000"},
            { "LastName", "Smith"}
        };

        request.Setup(r => r.Query).Returns(queryParams);

        // Act
        var response = await _sut.Run(request.Object);

        // Assert
        _exceptionHandler.Verify(x => x.CreateSystemExceptionLogFromNhsNumber(
            It.IsAny<Exception>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
            Times.Once);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}