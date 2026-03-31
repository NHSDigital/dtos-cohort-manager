namespace NHS.CohortManager.Tests.UnitTests.AuditServicesTests;

using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DataServices.Database;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;
using NHS.CohortManager.AuditServices;

[TestClass]
public class AuditWriterFunctionTests
{
    private Mock<DataServicesContext> _mockDbContext;
    private Mock<BlobServiceClient> _mockBlobService;
    private Mock<BlobContainerClient> _mockContainerClient;
    private Mock<BlobClient> _mockBlobClient;
    private Mock<ILogger<AuditWriterFunction>> _mockLogger;
    private Mock<FunctionContext> _mockFunctionContext;
    private AuditWriterFunction _sut;
    private List<ParticipantAuditLog> _addedEntities;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [TestInitialize]
    public void Setup()
    {
        _addedEntities = new List<ParticipantAuditLog>();

        _mockDbContext = new Mock<DataServicesContext>(
            new Microsoft.EntityFrameworkCore.DbContextOptions<DataServicesContext>());

        var mockDbSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<ParticipantAuditLog>>();
        mockDbSet.Setup(x => x.Add(It.IsAny<ParticipantAuditLog>()))
            .Callback<ParticipantAuditLog>(entity => _addedEntities.Add(entity));

        _mockDbContext.Setup(x => x.Set<ParticipantAuditLog>()).Returns(mockDbSet.Object);
        _mockDbContext.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        _mockBlobService = new Mock<BlobServiceClient>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();
        _mockLogger = new Mock<ILogger<AuditWriterFunction>>();
        _mockFunctionContext = new Mock<FunctionContext>();

        _mockBlobService
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_mockContainerClient.Object);

        _mockContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_mockBlobClient.Object);

        _mockBlobClient
            .Setup(x => x.Uri)
            .Returns(new Uri("https://storage.blob.core.windows.net/audit-request-snapshots/test.json"));

        _sut = new AuditWriterFunction(_mockDbContext.Object, _mockBlobService.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task Run_ValidMessage_WritesAuditLogToDatabase()
    {
        // Arrange
        var audit = CreateAuditMessage();
        var messageText = JsonSerializer.Serialize(audit, JsonOptions);

        // Act
        await _sut.Run(messageText, _mockFunctionContext.Object);

        // Assert
        Assert.AreEqual(1, _addedEntities.Count);
        var saved = _addedEntities[0];
        Assert.AreEqual(audit.NhsNumber, saved.NhsNumber);
        Assert.AreEqual(audit.CorrelationId, saved.CorrelationId);
        Assert.AreEqual((int)audit.Source, saved.RecordSource);
        Assert.AreEqual(audit.CreatedBy, saved.CreatedBy);
        _mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task Run_MessageWithRequestSnapshot_WritesBlobAndSetsUrl()
    {
        // Arrange
        var audit = CreateAuditMessage();
        audit.RequestSnapshot = new { Name = "Test", Value = 42 };
        var messageText = JsonSerializer.Serialize(audit, JsonOptions);

        _mockBlobClient
            .Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, default))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        // Act
        await _sut.Run(messageText, _mockFunctionContext.Object);

        // Assert
        _mockContainerClient.Verify(x => x.CreateIfNotExistsAsync(default, default, default, default), Times.Once);
        _mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<BinaryData>(), true, default), Times.Once);

        Assert.AreEqual(1, _addedEntities.Count);
        Assert.IsNotNull(_addedEntities[0].RawDataRef);
    }

    [TestMethod]
    public async Task Run_MessageWithoutRequestSnapshot_SkipsBlobWrite()
    {
        // Arrange
        var audit = CreateAuditMessage();
        audit.RequestSnapshot = null;
        var messageText = JsonSerializer.Serialize(audit, JsonOptions);

        // Act
        await _sut.Run(messageText, _mockFunctionContext.Object);

        // Assert
        _mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<BinaryData>(), It.IsAny<bool>(), default), Times.Never);

        Assert.AreEqual(1, _addedEntities.Count);
        Assert.IsNull(_addedEntities[0].RawDataRef);
    }

    [TestMethod]
    public async Task Run_InvalidJson_LogsErrorAndDoesNotWrite()
    {
        // Arrange
        var messageText = "not-valid-json!!!";

        // Act & Assert - should not throw
        await _sut.Run(messageText, _mockFunctionContext.Object);

        Assert.AreEqual(0, _addedEntities.Count);
        _mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [TestMethod]
    public async Task Run_ValidMessage_MapsAllFieldsCorrectly()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var audit = new ParticipantAuditMessage
        {
            CorrelationId = Guid.NewGuid(),
            NhsNumber = "9876543210",
            Source = AuditSource.ParquetFile,
            BatchId = batchId,
            RecordSourceDesc = "Parquet file import",
            CreatedDatetime = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc),
            CreatedBy = "TestFunction",
            ScreeningId = 1,
            RequestSnapshot = null
        };
        var messageText = JsonSerializer.Serialize(audit, JsonOptions);

        // Act
        await _sut.Run(messageText, _mockFunctionContext.Object);

        // Assert
        Assert.AreEqual(1, _addedEntities.Count);
        var saved = _addedEntities[0];
        Assert.AreEqual(audit.CorrelationId, saved.CorrelationId);
        Assert.AreEqual("9876543210", saved.NhsNumber);
        Assert.AreEqual((int)AuditSource.ParquetFile, saved.RecordSource);
        Assert.AreEqual("Parquet file import", saved.RecordSourceDesc);
        Assert.AreEqual("TestFunction", saved.CreatedBy);
        Assert.AreEqual(1, saved.ScreeningId);
        Assert.AreEqual(batchId, saved.BatchId);
        Assert.IsNull(saved.RawDataRef);
    }

    [TestMethod]
    public async Task Run_ValidMessage_StoresBlobInCorrectPath()
    {
        // Arrange
        var audit = CreateAuditMessage();
        audit.Source = AuditSource.ManualAdd;
        audit.CreatedDatetime = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        audit.RequestSnapshot = new { Data = "test" };
        var messageText = JsonSerializer.Serialize(audit, JsonOptions);

        _mockBlobClient
            .Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, default))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        // Act
        await _sut.Run(messageText, _mockFunctionContext.Object);

        // Assert
        var expectedBlobPath = $"ManualAdd/2025-03-15/{audit.CorrelationId}.json";
        _mockContainerClient.Verify(x => x.GetBlobClient(expectedBlobPath), Times.Once);
    }

    private static ParticipantAuditMessage CreateAuditMessage()
    {
        return new ParticipantAuditMessage
        {
            CorrelationId = Guid.NewGuid(),
            NhsNumber = "1234567890",
            Source = AuditSource.ManualAdd,
            RecordSourceDesc = "Test audit",
            CreatedDatetime = DateTime.UtcNow,
            CreatedBy = "UnitTest"
        };
    }
}
