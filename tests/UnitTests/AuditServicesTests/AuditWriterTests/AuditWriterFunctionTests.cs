namespace NHS.CohortManager.Tests.UnitTests.AuditServicesTests;

using System.Text.Json;
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
    private Mock<ILogger<AuditWriterFunction>> _mockLogger;
    private Mock<FunctionContext> _mockFunctionContext;
    private AuditWriterFunction _sut;
    private List<ParticipantAuditLog> _addedEntities;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
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

        _mockLogger = new Mock<ILogger<AuditWriterFunction>>();
        _mockFunctionContext = new Mock<FunctionContext>();

        _sut = new AuditWriterFunction(_mockDbContext.Object, _mockLogger.Object);
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
    public async Task Run_MessageWithRawDataRef_PersistsRefToDatabase()
    {
        // Arrange
        var audit = CreateAuditMessage();
        audit.RawDataRef = "https://storage.blob.core.windows.net/audit-request-snapshots/ManualAdd/2025-03-15/test.json";
        var messageText = JsonSerializer.Serialize(audit, JsonOptions);

        // Act
        await _sut.Run(messageText, _mockFunctionContext.Object);

        // Assert
        Assert.AreEqual(1, _addedEntities.Count);
        Assert.AreEqual(audit.RawDataRef, _addedEntities[0].RawDataRef);
    }

    [TestMethod]
    public async Task Run_MessageWithoutRawDataRef_SavesNullRef()
    {
        // Arrange
        var audit = CreateAuditMessage();
        audit.RawDataRef = null;
        var messageText = JsonSerializer.Serialize(audit, JsonOptions);

        // Act
        await _sut.Run(messageText, _mockFunctionContext.Object);

        // Assert
        Assert.AreEqual(1, _addedEntities.Count);
        Assert.IsNull(_addedEntities[0].RawDataRef);
    }

    [TestMethod]
    public async Task Run_InvalidJson_LogsErrorAndThrows()
    {
        // Arrange
        var messageText = "not-valid-json!!!";

        // Act & Assert
        await Assert.ThrowsExceptionAsync<JsonException>(() =>
            _sut.Run(messageText, _mockFunctionContext.Object));

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
            RawDataRef = null
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
