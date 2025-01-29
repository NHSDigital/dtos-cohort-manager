namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using NHS.CohortManager.Tests.TestUtils;
using Model;
using Data.Database;
using FluentAssertions;
using DataServices.Client;
using Moq;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq.Expressions;

[TestClass]
public class ValidationExceptionDataTests
{
    private readonly Mock<ILogger<ValidationExceptionData>> _logger = new();
    private List<ExceptionManagement> _exceptionList;
    private readonly Dictionary<string, string> columnToClassPropertyMapping;

    private readonly Mock<IDataServiceClient<ExceptionManagement>> _validationExceptionDataServiceClient = new();

    private readonly Mock<IDataServiceClient<ParticipantDemographic>> _demographicDataServiceClient = new();

    private readonly Mock<IDataServiceClient<GPPractice>> _gpPracticeDataServiceClient = new();

    private readonly ValidationExceptionData validationExceptionData;

    public ValidationExceptionDataTests()
    {
        validationExceptionData = new ValidationExceptionData(_logger.Object, _validationExceptionDataServiceClient.Object, _demographicDataServiceClient.Object, _gpPracticeDataServiceClient.Object);
        columnToClassPropertyMapping = new Dictionary<string, string>
        {
            { "EXCEPTION_ID", "ExceptionId"},
            { "COHORT_NAME", "CohortName" },
            { "DATE_CREATED", "DateCreated" }
        };
        _exceptionList = new List<ExceptionManagement>
        {
            new ExceptionManagement { ExceptionId = 1, CohortName = "Cohort1", DateCreated = DateTime.Today.AddDays(-2) },
            new ExceptionManagement { ExceptionId = 2, CohortName = "Cohort2", DateCreated = DateTime.Today.AddDays(-1) },
            new ExceptionManagement { ExceptionId = 3, CohortName = "Cohort3", DateCreated = DateTime.Today }
        };
    }

    [TestMethod]
    public async Task GetAllExceptions_NoExceptionId_ReturnsAllExceptions()
    {
        //arrange 
        _validationExceptionDataServiceClient.Setup(x => x.GetAll()).ReturnsAsync(_exceptionList);

        // Act
        var result = await validationExceptionData.GetAllExceptions(false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(_exceptionList, options => options
            .Including(x => x.ExceptionId)
            .Including(x => x.CohortName)
            .Including(x => x.DateCreated));

    }

    [TestMethod]
    public async Task GetAllExceptions_TodaysExceptionsOnly_ReturnsAllExceptions()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.DateCreated == DateTime.Today).ToList();
        _validationExceptionDataServiceClient.Setup(x => x.GetByFilter(It.IsAny<Expression<Func<ExceptionManagement, bool>>>())).ReturnsAsync(filteredList);

        // Act
        var result = await validationExceptionData.GetAllExceptions(true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo([_exceptionList[2]], options => options
                .Including(x => x.ExceptionId)
                .Including(x => x.CohortName)
                .Including(x => x.DateCreated)
        );
    }

    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestMethod]
    public async Task GetExceptionById_ValidExceptionId_ReturnsExpectedException(int exceptionId)
    {
        //arrange 
        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new ExceptionManagement() { ExceptionId = exceptionId });
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
        _validationExceptionDataServiceClient.Setup(x => x.GetSingle(It.IsAny<string>())).ReturnsAsync(new ExceptionManagement() { ExceptionId = exceptionId });
        _demographicDataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<ParticipantDemographic, bool>>>())).ReturnsAsync((ParticipantDemographic)null);
        _gpPracticeDataServiceClient.Setup(x => x.GetSingleByFilter(It.IsAny<Expression<Func<GPPractice, bool>>>())).ReturnsAsync((GPPractice)null);

        // Act
        var result = await validationExceptionData.GetExceptionById(exceptionId);

        // Assert
        result.Should().BeNull();
    }
}

