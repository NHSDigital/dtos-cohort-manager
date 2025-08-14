namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using Model;
using Data.Database;
using FluentAssertions;
using DataServices.Client;
using Moq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Model.Enums;
using System.Net;
using NHS.CohortManager.Tests.TestUtils;

[TestClass]
public class ValidationExceptionDataTests
{
    private readonly Mock<ILogger<ValidationExceptionData>> _logger = new();
    private readonly List<ExceptionManagement> _exceptionList;
    private readonly Mock<IDataServiceClient<ExceptionManagement>> _validationExceptionDataServiceClient = new();
    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _demographicDataServiceClient = new();
    private readonly ValidationExceptionData validationExceptionData;
    private readonly ExceptionCategory _exceptionCategory;

    public ValidationExceptionDataTests()
    {
        validationExceptionData = new ValidationExceptionData(_logger.Object, _validationExceptionDataServiceClient.Object, _demographicDataServiceClient.Object);
        _exceptionList = new List<ExceptionManagement>
        {
            new() { ExceptionId = 1, CohortName = "Cohort1", DateCreated = DateTime.UtcNow.Date, NhsNumber = "1111111111", RuleDescription = "RuleA", Category = 3, ServiceNowId = "ServiceNow1", ServiceNowCreatedDate = DateTime.UtcNow.Date },
            new() { ExceptionId = 2, CohortName = "Cohort2", DateCreated = DateTime.UtcNow.Date.AddDays(-1), NhsNumber = "2222222222", RuleDescription = "RuleB", Category = 3, ServiceNowId = "ServiceNow2", ServiceNowCreatedDate = DateTime.UtcNow.Date.AddDays(-1) },
            new() { ExceptionId = 3, CohortName = "Cohort3", DateCreated = DateTime.UtcNow.Date.AddDays(-2), NhsNumber = "3333333333", RuleDescription = "RuleC", Category = 3, ServiceNowId = null },
            new() { ExceptionId = 4, CohortName = "Cohort4", DateCreated = DateTime.Today.AddDays(-3), NhsNumber = "4444444444", RuleDescription = "RuleD", Category = 3, ServiceNowId = null }
        };
        _exceptionCategory = ExceptionCategory.NBO;
    }

    #region GetAllFilteredExceptions

    [TestMethod]
    public async Task GetAllFilteredExceptions_AllStatusWithAscendingSortOrder_ReturnsAllExceptionsAscendingSortOrder()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Ascending, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(4);
        result.Should().BeInAscendingOrder(o => o.DateCreated);
        result!.First().ExceptionId.Should().Be(4);
        result!.Last().ExceptionId.Should().Be(1);
    }

    [TestMethod]
    public async Task GetAllFilteredExceptions_AllStatusWithDescendingSortOrder_ReturnsAllExceptionsDescendingOrder()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllFilteredExceptions(ExceptionStatus.All, SortOrder.Descending, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(4);
        result.Should().BeInDescendingOrder(o => o.DateCreated);
        result!.First().ExceptionId.Should().Be(1);
        result!.Last().ExceptionId.Should().Be(4);
    }

    [TestMethod]
    public async Task GetAllFilteredExceptions_RaisedStatusWithAscendingSortOrder_ReturnsOnlyRaisedExceptionsAscendingOrder()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllFilteredExceptions(ExceptionStatus.Raised, SortOrder.Ascending, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(o => o.ServiceNowCreatedDate);
        result.Should().OnlyContain(e => !string.IsNullOrEmpty(e.ServiceNowId));
        result!.First().ExceptionId.Should().Be(2);
        result!.First().ServiceNowId.Should().Be("ServiceNow2");
        result!.Last().ExceptionId.Should().Be(1);
        result!.Last().ServiceNowId.Should().Be("ServiceNow1");
    }

    [TestMethod]
    public async Task GetAllFilteredExceptions_RaisedStatusWithDescendingSortOrder_ReturnsOnlyRaisedExceptionsDescendingOrder()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllFilteredExceptions(ExceptionStatus.Raised, SortOrder.Descending, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(o => o.ServiceNowCreatedDate);
        result.Should().OnlyContain(e => !string.IsNullOrEmpty(e.ServiceNowId));
        result!.First().ExceptionId.Should().Be(1);
        result!.First().ServiceNowId.Should().Be("ServiceNow1");
        result!.Last().ExceptionId.Should().Be(2);
        result!.Last().ServiceNowId.Should().Be("ServiceNow2");
    }

    [TestMethod]
    public async Task GetAllFilteredExceptions_NotRaisedStatusWithAscendingSortOrder_ReturnsOnlyNotRaisedExceptionsAscendingOrder()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllFilteredExceptions(ExceptionStatus.NotRaised, SortOrder.Ascending, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(o => o.DateCreated);
        result.Should().OnlyContain(e => string.IsNullOrEmpty(e.ServiceNowId));
        result!.First().ExceptionId.Should().Be(4);
        result!.Last().ExceptionId.Should().Be(3);
    }

    [TestMethod]
    public async Task GetAllFilteredExceptions_NotRaisedStatusWithDescendingSortOrder_ReturnsOnlyNotRaisedExceptionsDescendingOrder()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllFilteredExceptions(ExceptionStatus.NotRaised, SortOrder.Descending, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(o => o.DateCreated);
        result.Should().OnlyContain(e => string.IsNullOrEmpty(e.ServiceNowId));
        result!.First().ExceptionId.Should().Be(3);
        result!.Last().ExceptionId.Should().Be(4);
    }

    [TestMethod]
    public async Task GetAllFilteredExceptions_AllStatusWithNoSortOrderProvided_ReturnsAllRecordsInDefaultDescendingOrder()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllFilteredExceptions(ExceptionStatus.All, null, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(4);
        result.Should().BeInDescendingOrder(o => o.DateCreated);
        result!.First().ExceptionId.Should().Be(1);
        result!.Last().ExceptionId.Should().Be(4);
    }
    #endregion

    #region GetExceptionById
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestMethod]
    public async Task GetExceptionById_ValidExceptionId_ReturnsExpectedException(int exceptionId)
    {
        // Arrange
        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new ExceptionManagement() { ExceptionId = exceptionId, NhsNumber = "123456789" });
        _demographicDataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(new ParticipantDemographic());

        // Act
        var result = await validationExceptionData.GetExceptionById(exceptionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationException>();
        result!.ExceptionId.Should().Be(exceptionId);
    }

    [DataRow(999)]
    [DataRow(4)]
    [DataRow(37)]
    [TestMethod]
    public async Task GetExceptionById_InvalidId_ReturnsNull(int exceptionId)
    {
        // Arrange
        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).Returns(Task.FromResult<ExceptionManagement>(null));

        // Act
        var result = await validationExceptionData.GetExceptionById(exceptionId);

        // Assert
        result.Should().BeNull();
    }
    #endregion

    #region UpdateExceptionServiceNowId - ServiceResponseModel Tests

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ValidInput_ReturnsSuccessResponse()
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

        _validationExceptionDataServiceClient
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync(exception);

        _validationExceptionDataServiceClient
            .Setup(x => x.Update(It.IsAny<ExceptionManagement>()))
            .ReturnsAsync(true);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.ErrorMessage.Should().BeNull();

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);
        _validationExceptionDataServiceClient.Verify(x => x.Update(It.Is<ExceptionManagement>(e =>
            e.ServiceNowId == serviceNowId &&
            e.RecordUpdatedDate > DateTime.UtcNow.AddMinutes(-1))), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ExceptionNotFound_ReturnsNotFoundResponse()
    {
        // Arrange
        var exceptionId = 999;
        var serviceNowId = "SNOW123456789";

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ReturnsAsync((ExceptionManagement?)null);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.ErrorMessage.Should().Be($"Exception with ID {exceptionId} not found");

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);
        _validationExceptionDataServiceClient.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Never);

        _logger.VerifyLogger(LogLevel.Warning, $"Service error occurred: Exception with ID {exceptionId} not found");
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_SameServiceNowId_ReturnsBadRequestResponse()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "EXISTING123";
        var validException = new ExceptionManagement
        {
            ExceptionId = 1,
            ServiceNowId = "EXISTING123",
            NhsNumber = "1234567890",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ReturnsAsync(validException);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.ErrorMessage.Should().Be($"ServiceNowId {serviceNowId} is the same as the existing value");

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);
        _validationExceptionDataServiceClient.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Never);

        _logger.VerifyLogger(LogLevel.Warning, $"Service error occurred: ServiceNowId {serviceNowId} is the same as the existing value");
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_UpdateFails_ReturnsInternalServerErrorResponse()
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

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ReturnsAsync(exception);
        _validationExceptionDataServiceClient.Setup(x => x.Update(It.IsAny<ExceptionManagement>())).ReturnsAsync(false);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.ErrorMessage.Should().Be($"Failed to update exception {exceptionId} in data service");
        _validationExceptionDataServiceClient.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Once);
        _logger.VerifyLogger(LogLevel.Warning, $"Service error occurred: Failed to update exception {exceptionId} in data service");
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_DatabaseException_ReturnsInternalServerErrorResponse()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123456789";
        var expectedException = new Exception("Database connection failed");

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ThrowsAsync(expectedException);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.ErrorMessage.Should().Be($"Error updating ServiceNowID for exception {exceptionId}");

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(exceptionId.ToString()), Times.Once);

        _logger.VerifyLogger(LogLevel.Error, $"Error updating ServiceNowID for exception {exceptionId}", e => e == expectedException);
        _logger.VerifyLogger(LogLevel.Warning, $"Service error occurred: Error updating ServiceNowID for exception {exceptionId}");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public async Task UpdateExceptionServiceNowId_EmptyServiceNowId_ReturnsBadRequestResponse(string? serviceNowId)
    {
        // Arrange
        var exceptionId = 1;

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId ?? "");

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.ErrorMessage.Should().Be("ServiceNowID is required.");

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);

        _logger.VerifyLogger(LogLevel.Warning, "Service error occurred: ServiceNowID is required.");
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ServiceNowIdWithSpaces_ReturnsBadRequestResponse()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW 123456789";

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.ErrorMessage.Should().Be("ServiceNowID cannot contain spaces.");

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);

        _logger.VerifyLogger(LogLevel.Warning, "Service error occurred: ServiceNowID cannot contain spaces.");
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ShortServiceNowId_ReturnsBadRequestResponse()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123";

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.ErrorMessage.Should().Be("ServiceNowID must be at least 9 characters long.");

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);

        _logger.VerifyLogger(LogLevel.Warning, "Service error occurred: ServiceNowID must be at least 9 characters long.");
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_NonAlphanumericServiceNowId_ReturnsBadRequestResponse()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNOW123!@#";

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.ErrorMessage.Should().Be("ServiceNowID must contain only alphanumeric characters.");

        _validationExceptionDataServiceClient.Verify(x => x.GetSingle(It.IsAny<string>()), Times.Never);

        _logger.VerifyLogger(LogLevel.Warning, "Service error occurred: ServiceNowID must contain only alphanumeric characters.");
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ServiceNowIdWithLeadingTrailingSpaces_TrimsAndSucceeds()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "  SNOW123456789  ";
        var trimmedServiceNowId = "SNOW123456789";
        var exception = new ExceptionManagement
        {
            ExceptionId = exceptionId,
            ServiceNowId = "DIFFERENT123",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _validationExceptionDataServiceClient
            .Setup(x => x.GetSingle(exceptionId.ToString()))
            .ReturnsAsync(exception);

        _validationExceptionDataServiceClient
            .Setup(x => x.Update(It.IsAny<ExceptionManagement>()))
            .ReturnsAsync(true);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        _validationExceptionDataServiceClient.Verify(x => x.Update(It.Is<ExceptionManagement>(e =>
            e.ServiceNowId == trimmedServiceNowId)), Times.Once);
    }

    #endregion

    #region UpdateExceptionServiceNowId - Legacy Bool Tests

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ValidExceptionWithServiceNowId_ReturnsSuccess()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNW123456";
        var existingException = new ExceptionManagement
        {
            ExceptionId = exceptionId,
            ServiceNowId = "OldServiceNow",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ReturnsAsync(existingException);
        _validationExceptionDataServiceClient.Setup(x => x.Update(It.IsAny<ExceptionManagement>())).ReturnsAsync(true);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeTrue();
        _validationExceptionDataServiceClient.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_InvalidExceptionId_ReturnsFalse()
    {
        // Arrange
        var exceptionId = 999;
        var serviceNowId = "SNW123456";

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ReturnsAsync((ExceptionManagement)null);
        _validationExceptionDataServiceClient.Setup(x => x.Update(It.IsAny<ExceptionManagement>())).ReturnsAsync(true);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        _validationExceptionDataServiceClient.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_UpdateFails_ReturnsFalse()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNW123456";
        var existingException = new ExceptionManagement { ExceptionId = exceptionId };

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ReturnsAsync(existingException);
        _validationExceptionDataServiceClient.Setup(x => x.Update(It.IsAny<ExceptionManagement>())).ReturnsAsync(false);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [TestMethod]
    public async Task UpdateExceptionServiceNowId_ExceptionThrown_ReturnsFalseAndLogsError()
    {
        // Arrange
        var exceptionId = 1;
        var serviceNowId = "SNW123456";
        var expectedException = new Exception("Database connection failed");

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ThrowsAsync(expectedException);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Success.Should().BeFalse();
        _logger.VerifyLogger(LogLevel.Error, $"Error updating ServiceNowID for exception {exceptionId}", e => e == expectedException);
    }
    #endregion
}
