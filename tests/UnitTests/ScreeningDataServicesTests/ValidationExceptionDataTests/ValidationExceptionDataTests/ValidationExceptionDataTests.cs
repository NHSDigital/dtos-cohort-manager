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
    private readonly Mock<IDataServiceClient<GPPractice>> _gpPracticeDataServiceClient = new();
    private readonly ValidationExceptionData validationExceptionData;
    private readonly ExceptionCategory _exceptionCategory;

    public ValidationExceptionDataTests()
    {
        validationExceptionData = new ValidationExceptionData(_logger.Object, _validationExceptionDataServiceClient.Object, _demographicDataServiceClient.Object, _gpPracticeDataServiceClient.Object);
        _exceptionList = new List<ExceptionManagement>
        {
            new() { ExceptionId = 1, CohortName = "Cohort1", DateCreated = DateTime.Today.AddDays(-2), NhsNumber = "1111111111", RuleDescription = "RuleA", Category = 1 },
            new() { ExceptionId = 2, CohortName = "Cohort2", DateCreated = DateTime.Today.AddDays(-1), NhsNumber = "2222222222", RuleDescription = "RuleB", Category = 3 },
            new() { ExceptionId = 3, CohortName = "Cohort3", DateCreated = DateTime.Today, NhsNumber = "3333333333", RuleDescription = "RuleC", Category = 3 }
        };
        _exceptionCategory = ExceptionCategory.NBO;
    }

    #region GetAllExceptions
    [TestMethod]
    public async Task GetAllExceptions_TodaysExceptionsOnly_ReturnsTodaysNboExceptions()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.DateCreated == DateTime.Today && w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllExceptions(true, ExceptionSort.DateCreated, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(filteredList, options => options
                .Including(x => x.ExceptionId)
                .Including(x => x.CohortName)
                .Including(x => x.DateCreated)
        );
    }

    [DataRow(ExceptionSort.DateCreated)]
    [DataRow(null)]
    [TestMethod]
    public async Task GetAllExceptions_DateCreatedOrderByProperty_ReturnsAllNboExceptionsInDateDescendingOrder(ExceptionSort orderByProperty)
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllExceptions(false, orderByProperty, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(o => o.DateCreated);
    }

    [DataRow(ExceptionSort.ExceptionId)]
    [DataRow(ExceptionSort.NhsNumber)]
    [DataRow(ExceptionSort.RuleDescription)]
    [TestMethod]
    public async Task GetAllExceptions_OrderByProperty_ReturnsAllNboExceptionsSortedInAscendingOrder(ExceptionSort orderByProperty)
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        var exceptionSorts = new Dictionary<ExceptionSort, Expression<Func<ValidationException, IComparable>>>
        {
            { ExceptionSort.ExceptionId, o => o.ExceptionId },
            { ExceptionSort.NhsNumber, o => o.NhsNumber },
            { ExceptionSort.RuleDescription, o => o.RuleDescription }
        };

        // Act
        var result = await validationExceptionData.GetAllExceptions(false, orderByProperty, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(exceptionSorts[orderByProperty]);
    }

    [TestMethod]
    public async Task GetAllExceptions_DefaultExceptionCategory_ReturnsAllNboExceptions()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)_exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllExceptions(false, ExceptionSort.DateCreated, _exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(filteredList, options => options
            .Including(x => x.ExceptionId)
            .Including(x => x.CohortName)
            .Including(x => x.DateCreated));
    }

    [DataRow(ExceptionCategory.BSSelect)]
    [TestMethod]
    public async Task GetAllExceptions_ValidExceptionCategory_ReturnsExceptionsFilteredByCategory(ExceptionCategory exceptionCategory)
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.Category == (int)exceptionCategory).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllExceptions(false, ExceptionSort.DateCreated, exceptionCategory);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(filteredList, options => options
            .Including(x => x.ExceptionId)
            .Including(x => x.CohortName)
            .Including(x => x.DateCreated));
    }
    #endregion

    #region GetExceptionById
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestMethod]
    public async Task GetExceptionById_ValidExceptionId_ReturnsExpectedException(int exceptionId)
    {
        //arrange
        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new ExceptionManagement() { ExceptionId = exceptionId, NhsNumber = "123456789" });
        _demographicDataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync(new ParticipantDemographic());
        _gpPracticeDataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<GPPractice, bool>>>())).ReturnsAsync(new GPPractice());

        // Act
        var result = await validationExceptionData.GetExceptionById(exceptionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationException>();
        result.ExceptionId.Should().Be(exceptionId);
    }

    [DataRow(999)]
    [DataRow(4)]
    [DataRow(37)]
    [TestMethod]
    public async Task GetExceptionById_InvalidId_ReturnsNull(int exceptionId)
    {
        // Arrange
        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync((ExceptionManagement)null);
        _demographicDataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync((ParticipantDemographic)null);
        _gpPracticeDataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<GPPractice, bool>>>())).ReturnsAsync((GPPractice)null);

        // Act
        var result = await validationExceptionData.GetExceptionById(exceptionId);

        // Assert
        result.Should().BeNull();
    }
    #endregion
}

