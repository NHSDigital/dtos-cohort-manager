namespace NHS.CohortManager.Tests.ScreeningDataServicesTests;

using NHS.CohortManager.Tests.TestUtils;
using Model;
using Data.Database;

[TestClass]
public class ValidationExceptionDataTests : DatabaseTestBaseSetup<ValidationExceptionData>
{
    private List<ValidationException> _exceptionList;
    private readonly Dictionary<string, string> columnToClassPropertyMapping;

    public ValidationExceptionDataTests(): base((conn, logger, transaction, command, response) => new ValidationExceptionData(conn,logger))
    {
        columnToClassPropertyMapping = new Dictionary<string, string>{{ "EXCEPTION_ID", "ExceptionId" }};
        _exceptionList = new List<ValidationException>
        {
            new ValidationException { ExceptionId = 1 },
            new ValidationException { ExceptionId = 2 },
            new ValidationException { ExceptionId = 3 }
        };

        SetupDataReader(_exceptionList, columnToClassPropertyMapping);
    }

    [TestMethod]
    public void GetAllExceptions_NoExceptionId_ReturnsAllExceptions()
    {
        // Act
        var result = _service.GetAllExceptions();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(List<ValidationException>));
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(1, result[0].ExceptionId);
        Assert.AreEqual(2, result[1].ExceptionId);
        Assert.AreEqual(3, result[2].ExceptionId);
    }

    [TestMethod]
    public void GetExceptionById_ValidExceptionId_ReturnsExpectedException()
    {
        // Arrange
        var exceptionId = 1;

        // Act
        var result = _service.GetExceptionById(exceptionId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ValidationException));
        Assert.AreEqual(1, result.ExceptionId);
    }

    [TestMethod]
    public void GetExceptionById_InvalidId_ReturnsNull()
    {
        // Arrange
        var exceptionId = 999;
        _exceptionList = new List<ValidationException>();
        SetupDataReader(_exceptionList, columnToClassPropertyMapping);

        // Act
        var result = _service.GetExceptionById(exceptionId);

        // Assert
        Assert.IsNull(result);
    }
}

