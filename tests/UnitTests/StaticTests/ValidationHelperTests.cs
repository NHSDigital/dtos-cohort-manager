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
        //act
        var result = ValidationHelper.ValidateNHSNumber(nhsNumber);

        //assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("1234567890")]
    [DataRow("9876543219")]
    [DataRow("123456789")]
    [DataRow("")]
    [DataRow("sdfgsdg")]
    [DataRow("0000000000")]
    public void ValidateNhsNumber_InvalidNHNumbers_ReturnsFalse(string nhsNumber)
    {
        //act
        var result = ValidationHelper.ValidateNHSNumber(nhsNumber);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("1990-10-10")]
    [DataRow("2002-04-06")]
    [DataRow("1997-12-23")]
    [DataRow("2010-01-01")]
    [DataRow("2005-12-10")]
    public void ValidatePastDate_ValidPastDate_ReturnsTrue(string pastDate)
    {
        var result = ValidationHelper.ValidatePastDate(pastDate);

        Assert.IsTrue(result);
    }


    [TestMethod]
    [DataRow("1990-10-10")]
    [DataRow("2002-04-06")]
    [DataRow("1997-12-23")]
    [DataRow("2010-01-01")]
    [DataRow("2005-12-10")]
    public void ValidateDate_ValidDate_ReturnsTrue(string date)
    {
        var result = ValidationHelper.IsValidDate(date);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("9999-10-10")]
    [DataRow("3034-04-06")]
    [DataRow("6060-12-23")]
    [DataRow("7070-01-01")]
    [DataRow("dfbgdfdggfggg")]
    public void ValidatePastDate_InvalidPastDate_ReturnsFalse(string pastDate)
    {
        var result = ValidationHelper.ValidatePastDate(pastDate);

        Assert.IsFalse(result);
    }
}
