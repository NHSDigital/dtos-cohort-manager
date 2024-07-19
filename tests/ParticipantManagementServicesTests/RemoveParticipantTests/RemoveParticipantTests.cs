namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

using System.Net;
using System.Text.Json;
using Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.ParticipantManagementService;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class RemoveParticipantTests
{
    private readonly Mock<ILogger<RemoveParticipant>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly Mock<ICreateParticipant> _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private Mock<HttpRequestData> _request;
    private readonly RemoveParticipant _function;

    public RemoveParticipantTests()
    {
        Environment.SetEnvironmentVariable("markParticipantAsIneligible", "markParticipantAsIneligible");
        Environment.SetEnvironmentVariable("DemographicURIGet", "DemographicURIGet");

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                FirstName = "Joe",
                Surname = "Bloggs",
                NhsNumber = "1",
                RecordType = Actions.Removed
            }
        };

        _function = new RemoveParticipant(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });
    }

    [TestMethod]
    public async Task Run_return_ParticipantRemovedSuccessfully_OK()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var sut = new RemoveParticipant(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.OK);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        // Act
        var result = await _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }


    [TestMethod]
    public async Task Run_BadRequestReturnedFromRemoveDataService_InternalServerError()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var sut = new RemoveParticipant(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

        _request = _setupRequest.Setup(json);

        _webResponse.Setup(x => x.StatusCode).Returns(HttpStatusCode.BadRequest);
        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
            .Returns(Task.FromResult<HttpWebResponse>(_webResponse.Object));

        // Act
        var result = await _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [TestMethod]
    public async Task Run_AnErrorIsThrown_BadRequest()
    {
        // Arrange
        var json = JsonSerializer.Serialize(_participantCsvRecord);
        var sut = new RemoveParticipant(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant.Object, _handleException.Object);

        _request = _setupRequest.Setup(json);

        _checkDemographic.Setup(x => x.GetDemographicAsync(It.IsAny<string>(), It.Is<string>(s => s.Contains("DemographicURIGet"))))
            .Returns(Task.FromResult<Demographic>(new Demographic()));

        _callFunction.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("markParticipantAsIneligible")), It.IsAny<string>()))
        .Throws(new Exception("there has been a problem"));

        // Act
        var result = await _function.Run(_request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
