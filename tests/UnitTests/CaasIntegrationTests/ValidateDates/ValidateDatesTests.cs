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
    public void ValidateDates_ShouldReturnTrue_WhenDatesAreNull()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid date formats
            CurrentPostingEffectiveFromDate = null,
            EmailAddressEffectiveFromDate = null,
            MobileNumberEffectiveFromDate = null,
            UsualAddressEffectiveFromDate = null,
            TelephoneNumberEffectiveFromDate = null,
            PrimaryCareProviderEffectiveFromDate = null
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsTrue(res);
        _loggerMock.Verify(x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Error),
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MobileNumberEffectiveFromDate")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()),
       Times.Never);
    }


    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_TelephoneNumberEffectiveFromDateIsInValid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid date formats
            CurrentPostingEffectiveFromDate = null,
            EmailAddressEffectiveFromDate = null,
            MobileNumberEffectiveFromDate = null,
            UsualAddressEffectiveFromDate = null,
            TelephoneNumberEffectiveFromDate = "12345678975657",
            PrimaryCareProviderEffectiveFromDate = null
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_MobileNumberEffectiveFromDateIsInValid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid date formats
            CurrentPostingEffectiveFromDate = null,
            EmailAddressEffectiveFromDate = null,
            MobileNumberEffectiveFromDate = "12345678975657",
            UsualAddressEffectiveFromDate = null,
            TelephoneNumberEffectiveFromDate = null,
            PrimaryCareProviderEffectiveFromDate = null
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_PrimaryCareProviderEffectiveFromDateIsInvalid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid date formats
            CurrentPostingEffectiveFromDate = null,
            EmailAddressEffectiveFromDate = null,
            MobileNumberEffectiveFromDate = null,
            UsualAddressEffectiveFromDate = null,
            TelephoneNumberEffectiveFromDate = null,
            PrimaryCareProviderEffectiveFromDate = "12345678975657"
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsFalse(res);
    }

    [TestMethod]
    public void ValidateDates_ShouldReturnFalse_CurrentPostingEffectiveFromDateIsInValid()
    {
        // Arrange
        var participant = new Participant
        {
            //all valid date formats
            CurrentPostingEffectiveFromDate = "12345678975657",
            EmailAddressEffectiveFromDate = null,
            MobileNumberEffectiveFromDate = null,
            UsualAddressEffectiveFromDate = null,
            TelephoneNumberEffectiveFromDate = null,
            PrimaryCareProviderEffectiveFromDate = null
        };

        var res = _validateDates.ValidateAllDates(participant);

        // Assert
        Assert.IsFalse(res);
    }
}
