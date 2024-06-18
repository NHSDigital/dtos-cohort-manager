namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using processCaasFile;
using Microsoft.Azure.Functions.Worker.Http;
using Common;
using Model;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class processCaasFileTests
{
    private readonly Mock<ILogger<ProcessCaasFileFunction>> loggerMock = new();
    private readonly Mock<ICallFunction> _callFunctionMock = new();
    private Mock<HttpRequestData> _request;
    private readonly Mock<ICreateResponse> _createResponse = new();
    private readonly Mock<ICheckDemographic> _checkDemographic = new();
    private readonly SetupRequest setupRequest = new();
    private readonly Mock<ICreateBasicParticipantData> _createBasicParticipantData = new();

    public processCaasFileTests()
    {
        Environment.SetEnvironmentVariable("PMSAddParticipant", "PMSAddParticipant");
        Environment.SetEnvironmentVariable("PMSRemoveParticipant", "PMSRemoveParticipant");
        Environment.SetEnvironmentVariable("PMSUpdateParticipant", "PMSUpdateParticipant");
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_Call_AddParticipant()
    {
        //Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
                {
                    new Participant { RecordType = Actions.New },
                    new Participant { RecordType = Actions.New }
                }
        };
        var json = JsonSerializer.Serialize(cohort);

        _request = setupRequest.Setup(json);
        var sut = new ProcessCaasFileFunction(loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object);

        //Act
        var result = await sut.Run(_request.Object);

        //Assert
        _callFunctionMock.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSAddParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_Call_UpdateParticipant()
    {
        //Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
                {
                    new Participant { RecordType = Actions.Amended },
                    new Participant { RecordType = Actions.Amended }
                }
        };
        var json = JsonSerializer.Serialize(cohort);
        var sut = new ProcessCaasFileFunction(loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object);

        _request = setupRequest.Setup(json);

        //Act
        var result = await sut.Run(_request.Object);

        //Assert
        _callFunctionMock.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSUpdateParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_UpdateParticipant_throwsException()
    {
        //Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
                {
                    new() { RecordType = Actions.Amended }
                }
        };
        var exception = new Exception("Unable to call function");
        var json = JsonSerializer.Serialize(cohort);
        _request = setupRequest.Setup(json);


        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSUpdateParticipant")), It.IsAny<string>()))
            .ThrowsAsync(exception);

        var sut = new ProcessCaasFileFunction(loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object);

        // Act
        await sut.Run(_request.Object);

        // Assert
        loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_Call_DelParticipant()
    {
        //Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
                {
                    new Participant { RecordType = Actions.Removed },
                    new Participant { RecordType = Actions.Removed }
                }
        };
        var json = JsonSerializer.Serialize(cohort);
        var sut = new ProcessCaasFileFunction(loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object);

        _request = setupRequest.Setup(json);

        //Act
        var result = await sut.Run(_request.Object);

        //Assert
        _callFunctionMock.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_DelParticipant_throwsException()
    {
        //Arrange
        var cohort = new Cohort
        {
            Participants = new List<Participant>
                {
                    new Participant { RecordType = Actions.Removed }
                }
        };
        var exception = new Exception("Unable to call function");
        var json = JsonSerializer.Serialize(cohort);
        _request = setupRequest.Setup(json);


        _callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()))
            .ThrowsAsync(exception);

        var sut = new ProcessCaasFileFunction(loggerMock.Object, _callFunctionMock.Object, _createResponse.Object, _checkDemographic.Object, _createBasicParticipantData.Object);

        // Act
        await sut.Run(_request.Object);

        // Assert
        loggerMock.Verify(log =>
            log.Log(
            LogLevel.Information,
            0,
            It.Is<It.IsAnyType>((state, type) => state.ToString().Contains("Unable to call function")),
            null,
            (Func<object, Exception, string>)It.IsAny<object>()
            ));
    }
}
