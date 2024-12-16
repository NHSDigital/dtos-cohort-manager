namespace NHS.CohortManager.Tests.UnitTests.ScreeningDataServicesTests;

using NHS.CohortManager.Tests.TestUtils;
using Model;
using Data.Database;
using FluentAssertions;

[TestClass]
public class ValidationExceptionDataTests : DatabaseTestBaseSetup<ValidationExceptionData>
{
    private List<ValidationException> _exceptionList;
    private readonly Dictionary<string, string> columnToClassPropertyMapping;

    public ValidationExceptionDataTests() : base((conn, logger, transaction, command, response) => new ValidationExceptionData(conn, logger))
    {
        columnToClassPropertyMapping = new Dictionary<string, string>
        {
            { "EXCEPTION_ID", "ExceptionId"},
            { "COHORT_NAME", "CohortName" },
            { "DATE_CREATED", "DateCreated" }
        };
        _exceptionList = new List<ValidationException>
        {
            new ValidationException { ExceptionId = 1, CohortName = "Cohort1", DateCreated = DateTime.Today.AddDays(-2) },
            new ValidationException { ExceptionId = 2, CohortName = "Cohort2", DateCreated = DateTime.Today.AddDays(-1) },
            new ValidationException { ExceptionId = 3, CohortName = "Cohort3", DateCreated = DateTime.Today }
        };
        SetupDataReader(_exceptionList, columnToClassPropertyMapping);
    }

    [TestMethod]
    public void GetAllExceptions_NoExceptionId_ReturnsAllExceptions()
    {
        // Act
        var result = _service.GetAllExceptions(false);

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
    public void GetAllExceptions_TodaysExceptionsOnly_ReturnsAllExceptions()
    {
        // Arrange
        var filteredList = _exceptionList.Where(w => w.DateCreated == DateTime.Today).ToList();
        SetupDataReader(filteredList, columnToClassPropertyMapping);

        // Act
        var result = _service.GetAllExceptions(true);

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
    public void GetExceptionById_ValidExceptionId_ReturnsExpectedException(int exceptionId)
    {
        // Arrange
        SetupDataReader(_exceptionList, columnToClassPropertyMapping, exceptionId);

        // Act
        var result = _service.GetExceptionById(exceptionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationException>();
        result.ExceptionId.Should().Be(exceptionId);

    }

    [DataRow(999)]
    [DataRow(4)]
    [DataRow(37)]
    [TestMethod]
    public void GetExceptionById_InvalidId_ReturnsNull(int exceptionId)
    {
        // Arrange
        _exceptionList = new List<ValidationException>();
        SetupDataReader(_exceptionList, columnToClassPropertyMapping);

        // Act
        var result = _service.GetExceptionById(exceptionId);

        // Assert
        result.Should().BeNull();
    }
}

