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

    #region UpdateExceptionServiceNowId
    [DataRow(1, "SNW123456", true, true, DisplayName = "Valid exception ID with ServiceNow ID")]
    [DataRow(1, null, true, true, DisplayName = "Valid exception ID with null ServiceNow ID")]
    [DataRow(999, "SNW123456", false, false, DisplayName = "Invalid exception ID")]
    [TestMethod]
    public async Task UpdateExceptionServiceNowId_VariousScenarios_ReturnsExpectedResult(int exceptionId, string serviceNowId, bool exceptionExists, bool expectedResult)
    {
        // Arrange
        var existingException = exceptionExists ? new ExceptionManagement
        {
            ExceptionId = exceptionId,
            ServiceNowId = "OldServiceNow",
            RecordUpdatedDate = DateTime.UtcNow.AddDays(-1)
        } : null;

        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(exceptionId.ToString())).ReturnsAsync(existingException);
        _validationExceptionDataServiceClient.Setup(x => x.Update(It.IsAny<ExceptionManagement>())).ReturnsAsync(true);

        // Act
        var result = await validationExceptionData.UpdateExceptionServiceNowId(exceptionId, serviceNowId);

        // Assert
        result.Should().Be(expectedResult);

        if (exceptionExists)
        {
            existingException.ServiceNowId.Should().Be(serviceNowId);
            existingException.RecordUpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            _validationExceptionDataServiceClient.Verify(x => x.Update(existingException), Times.Once);
        }
        else
        {
            _validationExceptionDataServiceClient.Verify(x => x.Update(It.IsAny<ExceptionManagement>()), Times.Never);
            _logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Exception with ID {exceptionId} not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
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
        result.Should().BeFalse();
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
        result.Should().BeFalse();
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Error updating ServiceNowID for exception {exceptionId}")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    #endregion
}
