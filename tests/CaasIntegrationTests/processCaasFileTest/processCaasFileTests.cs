namespace processCaasFileTest;

using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using processCaasFile;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Model;

[TestClass]
public class processCaasFileTests
{
    private readonly Mock<ILogger<ProcessCaasFileFunction>> loggerMock = new();
    private readonly Mock<ICallFunction> callFunctionMock = new();
    private readonly ServiceCollection serviceCollection = new();
    private readonly Mock<FunctionContext> context = new();
    private readonly Mock<HttpRequestData> request;
    private readonly Mock<ICreateResponse> createResponse = new();

    public processCaasFileTests()
    {
        Environment.SetEnvironmentVariable("PMSAddParticipant", "PMSAddParticipant");
        Environment.SetEnvironmentVariable("PMSRemoveParticipant", "PMSRemoveParticipant");
        Environment.SetEnvironmentVariable("PMSUpdateParticipant", "PMSUpdateParticipant");

        request = new Mock<HttpRequestData>(context.Object);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        context.SetupProperty(c => c.InstanceServices, serviceProvider);
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_Call_AddParticipant()
    {
        //Arrange
        var cohort = new Cohort
        {
            cohort = new List<Participant>
                {
                    new Participant { Action = "ADD" },
                    new Participant { Action = "ADD" }
                }
        };
        var json = JsonSerializer.Serialize(cohort);

        setupRequest(json);
        var sut = new ProcessCaasFileFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        callFunctionMock.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSAddParticipant")), It.IsAny<string>()),
            Times.Exactly(2)); // Expected to be called twice for each participant
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_Call_UpdateParticipant()
    {
        //Arrange
        var cohort = new Cohort
        {
            cohort = new List<Participant>
                {
                    new Participant { Action = "UPDATE" },
                    new Participant { Action = "UPDATE" }
                }
        };
        var json = JsonSerializer.Serialize(cohort);
        var sut = new ProcessCaasFileFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

        setupRequest(json);

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        callFunctionMock.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSUpdateParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_UpdateParticipant_throwsException()
    {
        //Arrange
        var cohort = new Cohort
        {
            cohort = new List<Participant>
                {
                    new() { Action = "UPDATE" }
                }
        };
        var exception = new Exception("Unable to call function");
        var json = JsonSerializer.Serialize(cohort);
        setupRequest(json);


        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSUpdateParticipant")), It.IsAny<string>()))
            .ThrowsAsync(exception);

        var sut = new ProcessCaasFileFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

        // Act
        await sut.Run(request.Object);

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
            cohort = new List<Participant>
                {
                    new Participant { Action = "DEL" },
                    new Participant { Action = "DEL" }
                }
        };
        var json = JsonSerializer.Serialize(cohort);
        var sut = new ProcessCaasFileFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

        setupRequest(json);

        //Act
        var result = await sut.Run(request.Object);

        //Assert
        callFunctionMock.Verify(
            x => x.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task Run_Should_Log_RecordsReceived_And_DelParticipant_throwsException()
    {
        //Arrange
        var cohort = new Cohort
        {
            cohort = new List<Participant>
                {
                    new Participant { Action = "DEL" }
                }
        };
        var exception = new Exception("Unable to call function");
        var json = JsonSerializer.Serialize(cohort);
        setupRequest(json);


        callFunctionMock.Setup(call => call.SendPost(It.Is<string>(s => s.Contains("PMSRemoveParticipant")), It.IsAny<string>()))
            .ThrowsAsync(exception);

        var sut = new ProcessCaasFileFunction(loggerMock.Object, callFunctionMock.Object, createResponse.Object);

        // Act
        await sut.Run(request.Object);

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

    private void setupRequest(string json)
    {
        var byteArray = Encoding.ASCII.GetBytes(json);
        var bodyStream = new MemoryStream(byteArray);

        request.Setup(r => r.Body).Returns(bodyStream);
        request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

    }


}
