namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

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
            { "COHORT_NAME", "CohortName" }
        };
        _exceptionList = new List<ValidationException>
        {
            new ValidationException { ExceptionId = 1, CohortName = "Cohort1" },
            new ValidationException { ExceptionId = 2, CohortName = "Cohort2" },
            new ValidationException { ExceptionId = 3, CohortName = "Cohort3" }
        };
        SetupDataReader(_exceptionList, columnToClassPropertyMapping);
    }

    [TestMethod]
    public void GetAllExceptions_NoExceptionId_ReturnsAllExceptions()
    {
        // Act
        var result = _service.GetAllExceptions();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ValidationException>>();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(_exceptionList, options => options
            .ExcludingMissingMembers()
            .Including(x => x.ExceptionId)
            .Including(x => x.CohortName));
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

