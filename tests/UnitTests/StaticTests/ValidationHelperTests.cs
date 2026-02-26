namespace NHS.CohortManager.Tests.UnitTests.StaticTests;

using Common;


[TestClass]
public class ValidationHelperTests
{
    [TestMethod]
    [DataRow("3112728165")]
    [DataRow("6541239878")]
    [DataRow("9876543210")]
    public void ValidateNhsNumber_ValidNHNumbers_ReturnsTrue(string nhsNumber)
    {
        //Act
        var result = ValidationHelper.ValidateNHSNumber(nhsNumber);

        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("-1234567890")]
    [DataRow("1a2b3c4d5e")]
    [DataRow("1234567890")]
    [DataRow("9876543219")]
    [DataRow("123456789")]
    [DataRow("")]
    [DataRow("sdfgsdg")]
    [DataRow("0000000000")]
    public void ValidateNhsNumber_InvalidNHNumbers_ReturnsFalse(string nhsNumber)
    {
        //Act
        var result = ValidationHelper.ValidateNHSNumber(nhsNumber);

        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("20000101", DisplayName = "Format yyyyMMdd")]
    [DataRow("200001", DisplayName = "Format yyyyMM")]
    [DataRow("2000-01-01", DisplayName = "Format yyyy-MM-dd")]
    [DataRow("01/01/2000 12:00:00", DisplayName = "Format dd/MM/yyyy HH:mm:ss")]
    [DataRow("2000", DisplayName = "Format yyyy")]
    [DataRow("2000-01-01T10:30:00+00:00", DisplayName = "ISO 8601 with timezone offset")]
    [DataRow("2000-01-01T10:30:00Z", DisplayName = "ISO 8601 with UTC indicator")]
    [DataRow("2000-01-01T10:30:00", DisplayName = "ISO 8601 without timezone")]
    [DataRow("2000-01-01T10:30:00.123+00:00", DisplayName = "ISO 8601 with milliseconds and timezone")]
    [DataRow("2000-01-01T10:30:00.123", DisplayName = "ISO 8601 with milliseconds")]
    [DataRow("2000-01-01T10:30:00-05:00", DisplayName = "ISO 8601 with negative timezone offset")]
    public void ValidatePastDate_ValidPastDate_ReturnsTrue(string pastDate)
    {
        //Act
        var result = ValidationHelper.ValidatePastDate(pastDate);

        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("9999-10-10", DisplayName = "Future date standard format")]
    [DataRow("3034-04-06", DisplayName = "Future date 3034")]
    [DataRow("6060-12-23", DisplayName = "Future date 6060")]
    [DataRow("7070-01-01", DisplayName = "Future date 7070")]
    [DataRow("dfbgdfdggfggg", DisplayName = "Invalid format non-numeric")]
    [DataRow("-2000", DisplayName = "Negative year")]
    [DataRow(null, DisplayName = "Null value")]
    [DataRow("9999-12-31T23:59:59+00:00", DisplayName = "Future ISO 8601 with timezone")]
    [DataRow("9999-12-31T23:59:59Z", DisplayName = "Future ISO 8601 UTC")]
    [DataRow("9999-12-31T23:59:59.999+00:00", DisplayName = "Future ISO 8601 with milliseconds")]
    public void ValidatePastDate_InvalidPastDate_ReturnsFalse(string pastDate)
    {
        //Act
        var result = ValidationHelper.ValidatePastDate(pastDate);

        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidatePastDate_InvalidPastDateAlwaysFuture_ReturnsFalse()
    {
        //Act
        var result = ValidationHelper.ValidatePastDate(DateTime.UtcNow.AddDays(1).ToString("dd/MM/yyyy HH:mm:ss"));

        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidatePastDate_FutureISO8601Date_ReturnsFalse()
    {
        //Act
        var futureDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssK");
        var result = ValidationHelper.ValidatePastDate(futureDate);

        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("B33 8TH", DisplayName = "Standard postcode")]
    [DataRow("SW1A 1AA", DisplayName = "London postcode SW1A")]
    [DataRow("SM1 1AA", DisplayName = "London postcode SM1")]
    [DataRow("ZZ99 3CZ", DisplayName = "Dummy postcode format")]
    public void ValidatePostcode_ValidPostcode_ReturnsTrue(string postCode)
    {
        //Act
        var result = ValidationHelper.ValidatePostcode(postCode);

        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("A1", DisplayName = "Too short")]
    [DataRow("ABCDE 123", DisplayName = "Invalid format letters first")]
    [DataRow("123 ABC", DisplayName = "Invalid format numbers first")]
    [DataRow("W1A", DisplayName = "Missing incode")]
    [DataRow("SW1A1AAA", DisplayName = "Too long no space")]
    public void ValidatePostcode_InvalidPostcode_ReturnsFalse(string postCode)
    {
        //Act
        var result = ValidationHelper.ValidatePostcode(postCode);

        //Assert
        Assert.IsFalse(result);
    }
}
