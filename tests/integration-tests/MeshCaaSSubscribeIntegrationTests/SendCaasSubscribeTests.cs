namespace MeshCaaSSubscribeIntegrationTests;

using Microsoft.Extensions.DependencyInjection;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client;
using NHS.MESH.Client.Models;
using NHS.MESH.Client.Contracts.Configurations;
using NHS.MESH.Client.Helpers;
using Common;
using Moq;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using NHS.MESH.Client.Helpers.ContentHelpers;

[TestCategory("Integration")]
[TestClass]
public sealed class SendCaasSubscribeTests
{
    private readonly IMeshOutboxService _meshOutboxService;
    private readonly IMeshInboxService _meshInboxService;
    private readonly IMeshConnectConfiguration _config;
    private readonly Mock<ILogger<MeshSendCaasSubscribe>> _mockLogger = new();
    private readonly Mock<IOptions<MeshSendCaasSubscribeConfig>> _options = new();

    private readonly MeshSendCaasSubscribe _sut;

    private const string toMailbox = "X26ABC2";
    private const string fromMailbox = "X26ABC1";
    private const string workflowId = "TEST-WORKFLOW";
    public SendCaasSubscribeTests()
    {
        var services = new ServiceCollection();

        services.AddMeshClient(options =>
        {
            options.MeshApiBaseUrl = "http://localhost:8700/messageexchange";
        })
        .AddMailbox(fromMailbox,
        new NHS.MESH.Client.Configuration.MailboxConfiguration
        {
            Password = "password",
            SharedKey = "TestKey"
        })
        .AddMailbox(toMailbox,
        new NHS.MESH.Client.Configuration.MailboxConfiguration
        {
            Password = "password",
            SharedKey = "TestKey"
        })
        .Build();

        _options.Setup(i => i.Value).Returns(new MeshSendCaasSubscribeConfig
        {
            SendCaasWorkflowId = "Workflow"
        });

        var serviceProvider = services.BuildServiceProvider();
        _meshInboxService = serviceProvider.GetService<IMeshInboxService>()!;
        _meshOutboxService = serviceProvider.GetService<IMeshOutboxService>()!;
        _config = serviceProvider.GetService<IMeshConnectConfiguration>()!;

        _sut = new(_mockLogger.Object, _meshOutboxService, _options.Object);
    }

    [TestMethod]
    public async Task SendSubscriptionRequest_SendsNormalNhsNumber_ReturnsMessageId()
    {
        // arrange
        long nhsNumber = 9995534991;

        // act
        var messageId = await _sut.SendSubscriptionRequest(nhsNumber, toMailbox, fromMailbox);

        // assert
        Assert.IsNotNull(messageId);

        // act - validate message recieved
        var getMessagesResult = await _meshInboxService.GetMessagesAsync(toMailbox);

        // assert - File is in mesh
        Assert.IsTrue(getMessagesResult.Response.Messages.Contains(messageId));

        // act - download message and decompress message
        var message = await _meshInboxService.GetMessageByIdAsync(toMailbox, messageId);
        var fileContent = GZIPHelpers.DeCompressBuffer(message.Response.FileAttachment.Content);

        // asset - ensure message contains expected parquet file
        ParquetAsserts.ContainsExpectedNhsNumber(fileContent, nhsNumber);

    }
}
