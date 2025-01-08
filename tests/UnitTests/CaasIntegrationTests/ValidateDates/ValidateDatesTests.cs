namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.Reflection;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.Screening.ReceiveCaasFile;

[TestClass]
public class ValidateDatesTests
{
    private ValidateDates _validateDates;

    private Mock<ILogger<ValidateDates>> _loggerMock = new();

    public ValidateDatesTests()
    {
        _validateDates = new ValidateDates(_loggerMock.Object);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenCurrentPostingEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            //all invalid dates
            CurrentPostingEffectiveFromDate = "123456789",
            EmailAddressEffectiveFromDate = "20230101",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "20230101",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnTrue_WhenAllDatesAreValid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid dates
            CurrentPostingEffectiveFromDate = "20230101",
            EmailAddressEffectiveFromDate = "20230101",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "20230101",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsTrue(res);

        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("found in participant data")),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
         Times.Never);

    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenEmailAddressEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            //Valid date formats
            CurrentPostingEffectiveFromDate = "20230101",
            EmailAddressEffectiveFromDate = "123456789",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "20230101",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsFalse(res);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("EmailAddressEffectiveFromDate")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }


    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenMobileNumberEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            // Valid date format
            CurrentPostingEffectiveFromDate = "20230101",
            EmailAddressEffectiveFromDate = "123456789",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "20230101",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var res = _validateDates.ValidateAllDates(participant);
        // Assert
        Assert.IsFalse(res);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MobileNumberEffectiveFromDate")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_WhenUsualAddressEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid date formats
            CurrentPostingEffectiveFromDate = "20230101",
            EmailAddressEffectiveFromDate = "20230101",
            MobileNumberEffectiveFromDate = "20230101",
            UsualAddressEffectiveFromDate = "123456789",
            TelephoneNumberEffectiveFromDate = "20230101",
            PrimaryCareProviderEffectiveFromDate = "20230101"
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsFalse(res);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MobileNumberEffectiveFromDate")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateIsNull()
    {
        // Arrange
        string? date = null;


        var method = _validateDates.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_validateDates, arguments);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateIsEmpty()
    {
        // Arrange
        string date = string.Empty;

        var method = _validateDates.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act


        var res = (bool)method.Invoke(_validateDates, arguments);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateLengthIs8()
    {
        // Arrange
        string date = "20230101"; // Valid date format with 8 characters

        var method = _validateDates.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_validateDates, arguments);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnFalse_WhenDateLengthIsGreaterThan8()
    {
        // Arrange
        string date = "20230101234"; // Invalid date with more than 8 characters

        var method = _validateDates.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_validateDates, arguments);

        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void IsValidDate_ShouldReturnTrue_WhenDateLengthIsLessThan8()
    {
        // Arrange
        string date = "202301"; // Valid date with less than 8 characters

        var method = _validateDates.GetType().GetMethod("IsValidDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var arguments = new object[] { date };

        // Act
        var res = (bool)method.Invoke(_validateDates, arguments);

        // Assert
        Assert.IsTrue(res);
    }
}
