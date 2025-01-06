namespace NHS.CohortManager.Tests.UnitTests.ParticipantManagementServiceTests;

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
public class UpdateParticipantFromScreeningProviderTests
{
    private readonly Mock<ILogger<RemoveParticipant>> _logger = new();
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICallFunction> _callFunction = new();
    private readonly Mock<HttpWebResponse> _webResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly CreateParticipant _createParticipant = new();
    private readonly Mock<IExceptionHandler> _handleException = new();
    private readonly SetupRequest _setupRequest = new();
    private readonly ParticipantCsvRecord _participantCsvRecord;
    private Mock<HttpRequestData> _request;
    private readonly RemoveParticipant _function;

    public UpdateParticipantFromScreeningProviderTests()
    {

        _participantCsvRecord = new ParticipantCsvRecord
        {
            FileName = "test.csv",
            Participant = new Participant()
            {
                FirstName = "Joe",
                FamilyName = "Bloggs",
                NhsNumber = "1",
                RecordType = Actions.Removed
            }
        };

        _function = new RemoveParticipant(_logger.Object, _createResponse.Object, _callFunction.Object, _checkDemographic.Object, _createParticipant, _handleException.Object);

        _createResponse.Setup(x => x.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), ""))
            .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
            {
                var response = req.CreateResponse(statusCode);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                return response;
            });
    }

    [TestMethod]
    public async Task Run_ParticipantDoesNotExist_ReturnNotFound()
    {

    }
}
