namespace NHS.CohortManager.Tests.UnitTests.DataTests;

using System.Net;
using Model;
using Moq;
using Data.Database;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DataServices.Client;

[TestClass]
public class UpdateExceptionServiceNowIdTests
{
    private readonly Mock<ILogger<ValidationExceptionData>> _loggerMock = new();
    private readonly Mock<IDataServiceClient<ExceptionManagement>> _validationExceptionDataServiceClientMock = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _demographicDataServiceClientMock = new();
    private readonly ValidationExceptionData _service;
    private readonly ExceptionManagement _validException;

    public UpdateExceptionServiceNowIdTests()
    {
        _validException = new ExceptionManagement
        {
            ExceptionId = 1,
            ServiceNowId = "EXISTING123",
            NhsNumber = "1234567890",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _service = new ValidationExceptionData(
            _loggerMock.Object,
            _validationExceptionDataServiceClientMock.Object,
            _demographicDataServiceClientMock.Object
        );
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123456789";
        var exception = new ExceptionManagement
        {
            ExceptionId = exceptionId,
            ServiceNowId = "DIFFERENT123",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _validationExceptionDataServiceClientMock
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync(exception);

        _validationExceptionDataServiceClientMock
            .Setup(x => x.Update(It.IsAny<ExceptionManagement>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsNull(result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);
        _validationExceptionDataServiceClientMock.Verify(x => x.Update(It.Is<ExceptionManagement>(e =>
            e.ServiceNowId == serviceNowId &&
            e.RecordUpdatedDate > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ExceptionNotFound_ReturnsNotFound()
    {
        // Arrange
        var exceptionId = 999;
        var serviceNowId = "SNOW123456789";

        _validationExceptionDataServiceClientMock
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync((ExceptionManagement?)null);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        Assert.AreEqual($"Exception with ID {exceptionId} not found", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);
        _validationExceptionDataServiceClientMock.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_SameServiceNowId_ReturnsBadRequest()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "EXISTING123";

        _validationExceptionDataServiceClientMock
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync(_validException);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.AreEqual($"ServiceNowId {serviceNowId} is the same as the existing value", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);
        _validationExceptionDataServiceClientMock.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_UpdateFails_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123456789";
        var exception = new ExceptionManagement
        {
            ExceptionId = exceptionId,
            ServiceNowId = "DIFFERENT123",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _validationExceptionDataServiceClientMock
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync(exception);

        _validationExceptionDataServiceClientMock
            .Setup(x => x.Update(It.IsAny<ExceptionManagement>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.AreEqual($"Failed to update exception {exceptionId} in data service", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_DatabaseException_ReturnsInternalServerError()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123456789";
        var expectedException = new Exception("Database connection failed");

        _validationExceptionDataServiceClientMock
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.AreEqual($"Error updating ServiceNowID for exception {exceptionId}", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public async Task UpdateExceptionServiceNowId_EmptyServiceNowId_ReturnsBadRequest(string? serviceNowId)
    {
        // Arrange
        var exceptionId = 1;

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId ?? "");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.AreEqual("ServiceNowID is required.", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ServiceNowIdWithSpaces_ReturnsBadRequest()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW 123456789";

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.AreEqual("ServiceNowID cannot contain spaces.", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ShortServiceNowId_ReturnsBadRequest()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123"; // Less than 9 characters

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.AreEqual("ServiceNowID must be at least 9 characters long.", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_NonAlphanumericServiceNowId_ReturnsBadRequest()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123!@#"; // Contains special characters

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.AreEqual("ServiceNowID must contain only alphanumeric characters.", result.ErrorMessage);

        _validationExceptionDataServiceClientMock.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ServiceNowIdWithLeadingTrailingSpaces_TrimsAndSucceeds()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "  SNOW123456789  "; // Spaces around valid ID
        var trimmedServiceNowId = "SNOW123456789";
        var exception = new ExceptionManagement
        {
            ExceptionId = exceptionId,
            ServiceNowId = "DIFFERENT123",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _validationExceptionDataServiceClientMock
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync(exception);

        _validationExceptionDataServiceClientMock
            .Setup(x => x.Update(It.IsAny<ExceptionManagement>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);

        _validationExceptionDataServiceClientMock.Verify(x => x.Update(It.Is<ExceptionManagement>(e =>
            e.ServiceNowId == trimmedServiceNowId)), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_SuccessfulUpdate_NoErrorLogging()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123456789";
        var exception = new ExceptionManagement
        {
            ExceptionId = exceptionId,
            ServiceNowId = "DIFFERENT123",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _validationExceptionDataServiceClientMock
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync(exception);

        _validationExceptionDataServiceClientMock
            .Setup(x => x.Update(It.IsAny<ExceptionManagement>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        Assert.IsTrue(result.Success);
    }
}
