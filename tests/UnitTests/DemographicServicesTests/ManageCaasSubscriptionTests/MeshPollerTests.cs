namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;

[TestClass]
public class MeshPollerTests
{
    private readonly Mock<ILogger<MeshPoller>> _logger = new();
    private readonly Mock<IMeshOperationService> _meshOps = new();

    [TestMethod]
    public async Task ExecuteHandshake_Success_ReturnsTrue()
    {
        // Arrange
        _meshOps
            .Setup(m => m.MeshHandshakeAsync(It.IsAny<string>()))
            .ReturnsAsync(new MeshResponse<HandshakeResponse> { IsSuccessful = true, Response = new HandshakeResponse { MailboxId = "MAILBOX" } });

        var sut = new MeshPoller(_logger.Object, _meshOps.Object);

        // Act
        var result = await sut.ExecuteHandshake("MAILBOX");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ExecuteHandshake_Failure_ReturnsFalse()
    {
        // Arrange
        _meshOps
            .Setup(m => m.MeshHandshakeAsync(It.IsAny<string>()))
            .ReturnsAsync(new MeshResponse<HandshakeResponse> { IsSuccessful = false, Error = new APIErrorResponse { ErrorCode = "500", ErrorDescription = "err" } });

        var sut = new MeshPoller(_logger.Object, _meshOps.Object);

        // Act
        var result = await sut.ExecuteHandshake("MAILBOX");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ShouldExecuteHandshake_NotImplemented_Throws()
    {
        // Arrange
        var sut = new MeshPoller(_logger.Object, _meshOps.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(() => sut.ShouldExecuteHandshake("MAILBOX", "config.json"));
    }
}
