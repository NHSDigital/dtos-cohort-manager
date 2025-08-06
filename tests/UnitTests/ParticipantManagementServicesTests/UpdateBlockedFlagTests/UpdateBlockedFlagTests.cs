namespace NHS.CohortManager.Tests.ParticipantManagementServiceTests;

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
using RulesEngine.Models;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

[TestClass]
public class UpdateBlockedFlagTests
{
    private readonly UpdateBlockedFlag _sut;
    private readonly Mock<IDataServiceClient<ParticipantManagement>> _mockParticipantManagementClient = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _mockParticipantDemographicClient = new();
    private readonly Mock<ILogger<UpdateBlockedFlag>> _mockUpdateBlockedFlagLogger = new();
    private readonly Mock<ILogger<BlockParticipantHandler>> _mockHandlerLogger = new();
    private readonly Mock<ICreateResponse> _mockCreateResponse = new();
    private readonly BlockParticipantHandler _blockParticipantHandler;
    private readonly Mock<IHttpClientFunction> _mockHttpClient = new();
    private readonly Mock<IOptions<UpdateBlockedFlagConfig>> _mockConfig = new();

    public UpdateBlockedFlagTests()
    {
        _mockConfig.Setup(x => x.Value).Returns(new UpdateBlockedFlagConfig
        {
            ParticipantDemographicDataServiceURL = "participantManagementUrl",
            ParticipantManagementUrl = "ParticipantManagementUrl",
            ExceptionFunctionURL = "ExceptionFunctionUrl",
            ManageNemsSubscriptionSubscribeURL = "NemsSubscribeUrl",
            ManageNemsSubscriptionUnsubscribeURL = "NemsUnsubscribeUrl",
            RetrievePdsDemographicURL = "RetrievePdsDemographicUrl"
        });
        _blockParticipantHandler = new BlockParticipantHandler(_mockHandlerLogger.Object, _mockParticipantManagementClient.Object, _mockParticipantDemographicClient.Object, _mockHttpClient.Object, _mockConfig.Object);
        _sut = new UpdateBlockedFlag(_mockUpdateBlockedFlagLogger.Object, _mockCreateResponse.Object, _blockParticipantHandler);
    }


}
