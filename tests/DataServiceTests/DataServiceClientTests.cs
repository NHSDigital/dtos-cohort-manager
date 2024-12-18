namespace DataServiceTests;

using CurrentPostingDataService;
using Common;
using DataServices.Core;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using FluentAssertions;
using System.Text.Json;
using DataServices.Client;
using System.Reflection.Metadata;

[TestClass]
public class DataServiceClientTests
{
    private readonly Mock<ILogger<DataServiceClient<ParticipantDemographic>>> _mockLogger = new();
    private readonly DataServiceResolver _dataServiceResolver;

    private readonly Mock<ICallFunction> _mockCallFunction = new();

    private const string baseUrl = "testUrl";

    public DataServiceClientTests()
    {
        _dataServiceResolver = new DataServiceResolver(new Dictionary<Type,string> {
            {typeof(ParticipantDemographic), baseUrl}
        });
    }
    [TestMethod]
    public async Task GetAll_GetAllItems_ReturnsArray()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendGet(It.IsAny<string>())).ReturnsAsync("[]");

        //act
        var result = await dataServiceClient.GetAll();


        //assert
        _mockCallFunction.Verify(i => i.SendGet(baseUrl),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task Update_SendsUpdateRequest_ReturnsTrue()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendPut(It.IsAny<string>(),It.IsAny<string>()));

        var participant = new ParticipantDemographic{
            ParticipantId = 123
        };

        //act
        var result = await dataServiceClient.Update(participant);


        //assert
        Assert.IsTrue(result);
        _mockCallFunction.Verify(i => i.SendPut(baseUrl+"/"+"123",It.IsAny<string>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }



}
