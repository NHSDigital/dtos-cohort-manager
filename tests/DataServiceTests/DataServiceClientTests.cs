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
        result.Should().BeEmpty();
        _mockCallFunction.Verify(i => i.SendGet(baseUrl),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task Update_SendsUpdateRequest_ReturnsTrue()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendPut(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK,"[]"));

        var participant = new ParticipantDemographic{
            ParticipantId = 123
        };

        //act
        var result = await dataServiceClient.Update(participant);

        //assert
        result.Should().BeTrue();
        _mockCallFunction.Verify(i => i.SendPut(baseUrl+"/"+"123",It.IsAny<string>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task Update_SendsUpdateRequest_ReturnsFalse()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendPut(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(MockHelpers.CreateMockHttpResponseData(HttpStatusCode.NotFound,"[]"));

        var participant = new ParticipantDemographic{
            ParticipantId = 123
        };

        //act
        var result = await dataServiceClient.Update(participant);

        //assert
        result.Should().BeFalse();
        _mockCallFunction.Verify(i => i.SendPut(baseUrl+"/"+"123",It.IsAny<string>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task GetByFilter_SendsGetByFilterRequest_ReturnsArray()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendGet(It.IsAny<string>(),It.IsAny<Dictionary<string,string>>())).ReturnsAsync("[]");

        var participant = new ParticipantDemographic{
            ParticipantId = 123
        };

        //act
        var result = await dataServiceClient.GetByFilter(i => i.ParticipantId == 123);

        //assert
        result.Should().BeEmpty();
        _mockCallFunction.Verify(i => i.SendGet(baseUrl,It.IsAny<Dictionary<string,string>>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task GetSingle_SendsGetSingleRequest_ReturnsParticipantDemographic()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendGet(It.IsAny<string>())).ReturnsAsync("{}");

        var participant = new ParticipantDemographic{
            ParticipantId = 123
        };

        //act
        var result = await dataServiceClient.GetSingle("123");

        //assert
        result.Should().BeAssignableTo<ParticipantDemographic>();
        _mockCallFunction.Verify(i => i.SendGet(baseUrl+"/"+"123"),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task GetSingle_SendsBadSingleRequest_ReturnsNull()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendGet(It.IsAny<string>())).Throws(MockHelpers.CreateMockWebException(HttpStatusCode.NotFound));


        //act
        var result = await dataServiceClient.GetSingle("123");

        //assert
        result.Should().BeNull();
        _mockCallFunction.Verify(i => i.SendGet(baseUrl+"/"+"123"),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Delete_SendsDeleteRequest_ReturnsBoolean(bool response)
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendDelete(It.IsAny<string>())).ReturnsAsync(response);


        //act
        var result = await dataServiceClient.Delete("123");

        //assert
        result.Should().Be(response);
        _mockCallFunction.Verify(i => i.SendDelete(baseUrl+"/"+"123"),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task AddRange_SendsAddRange_ReturnsTrue()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendPost(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK));

        var participants = new List<ParticipantDemographic>{
            new ParticipantDemographic{ParticipantId = 123},
            new ParticipantDemographic{ParticipantId = 456}
        };

        //act
        var result = await dataServiceClient.AddRange(participants);

        //assert
        result.Should().BeTrue();
        _mockCallFunction.Verify(i => i.SendPost(baseUrl,It.IsAny<string>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task AddRange_SendsAddRange_ReturnsFalse()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendPost(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(MockHelpers.CreateMockHttpResponseData(HttpStatusCode.InternalServerError));

        var participants = new List<ParticipantDemographic>{
            new ParticipantDemographic{ParticipantId = 123},
            new ParticipantDemographic{ParticipantId = 456}
        };

        //act
        var result = await dataServiceClient.AddRange(participants);

        //assert
        result.Should().BeFalse();
        _mockCallFunction.Verify(i => i.SendPost(baseUrl,It.IsAny<string>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task Add_SendsAddSingle_ReturnsTrue()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendPost(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(MockHelpers.CreateMockHttpResponseData(HttpStatusCode.OK));

        var participant = new ParticipantDemographic{ParticipantId = 123};

        //act
        var result = await dataServiceClient.Add(participant);

        //assert
        result.Should().BeTrue();
        _mockCallFunction.Verify(i => i.SendPost(baseUrl,It.IsAny<string>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }
    [TestMethod]
    public async Task Add_SendsAddSingle_ReturnsFalse()
    {
        //arrange
        DataServiceClient<ParticipantDemographic> dataServiceClient = new DataServiceClient<ParticipantDemographic>(_mockLogger.Object,_dataServiceResolver,_mockCallFunction.Object);
        _mockCallFunction.Setup(i => i.SendPost(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(MockHelpers.CreateMockHttpResponseData(HttpStatusCode.InternalServerError));

        var participant = new ParticipantDemographic{ParticipantId = 123};

        //act
        var result = await dataServiceClient.Add(participant);

        //assert
        result.Should().BeFalse();
        _mockCallFunction.Verify(i => i.SendPost(baseUrl,It.IsAny<string>()),Times.Once);
        _mockCallFunction.VerifyNoOtherCalls();
    }

}
