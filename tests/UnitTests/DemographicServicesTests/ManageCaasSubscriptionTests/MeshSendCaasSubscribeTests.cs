namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;

[TestClass]
public class MeshSendCaasSubscribeTests
{
    private readonly Mock<ILogger<MeshSendCaasSubscribe>> _logger = new();
    private readonly Mock<IMeshOutboxService> _meshOutbox = new();

    private MeshSendCaasSubscribe CreateSut(string workflowId = "WF-CAAS-SUB")
    {
        var cfg = Options.Create(new MeshSendCaasSubscribeConfig { SendCaasWorkflowId = workflowId });
        return new MeshSendCaasSubscribe(_logger.Object, _meshOutbox.Object, cfg);
    }

    [TestMethod]
    public async Task SendSubscriptionRequest_Success_SendsExpectedAttachment_AndReturnsMessageId()
    {
        // Arrange
        string? capturedFrom = null, capturedTo = null, capturedWorkflow = null;
        FileAttachment? capturedFile = null;
        _meshOutbox
            .Setup(m => m.SendCompressedMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FileAttachment>(), null, null, false))
            .Callback((string from, string to, string workflow, FileAttachment file, string? _, string? __, bool ___) =>
            {
                capturedFrom = from; capturedTo = to; capturedWorkflow = workflow; capturedFile = file;
            })
            .ReturnsAsync(new MeshResponse<SendMessageResponse>
            {
                IsSuccessful = true,
                Response = new SendMessageResponse { MessageId = "MSG123" }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SendSubscriptionRequest(9000000009L, "TO_BOX", "FROM_BOX");

        // Assert
        Assert.AreEqual("MSG123", result);
        Assert.AreEqual("FROM_BOX", capturedFrom);
        Assert.AreEqual("TO_BOX", capturedTo);
        Assert.AreEqual("WF-CAAS-SUB", capturedWorkflow);
        Assert.IsNotNull(capturedFile);
        Assert.AreEqual("CaaSSubscribe.parquet", capturedFile!.FileName);
        Assert.AreEqual("application/octet-stream", capturedFile.ContentType);
        Assert.IsNotNull(capturedFile.Content);
        Assert.IsTrue(capturedFile.Content.Length > 0);
    }

    [TestMethod]
    public async Task SendSubscriptionRequest_Failure_ReturnsNull()
    {
        // Arrange
        _meshOutbox
            .Setup(m => m.SendCompressedMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FileAttachment>(), null, null, false))
            .ReturnsAsync(new MeshResponse<SendMessageResponse>
            {
                IsSuccessful = false,
                Error = new APIErrorResponse { ErrorCode = "500", ErrorDescription = "boom" }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.SendSubscriptionRequest(9000000009L, "TO_BOX", "FROM_BOX");

        // Assert
        Assert.IsNull(result);
    }
}

