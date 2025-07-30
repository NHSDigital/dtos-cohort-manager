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
        //act
        var result = ValidationHelper.ValidateNHSNumber(nhsNumber);

        //assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("20000101")]               //  yyyymmdd
    [DataRow("200001")]                 //  yyyymm
    [DataRow("2000-01-01")]             //  yyyy-mm-dd
    [DataRow("01/01/2000 12:00:00")]    //  dd/mm/yyyy hh:mm:ss
    [DataRow("2000")]                   //  yyyy
    public void ValidatePastDate_ValidPastDate_ReturnsTrue(string pastDate)
    {
        var result = ValidationHelper.ValidatePastDate(pastDate);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("9999-10-10")]
    [DataRow("3034-04-06")]
    [DataRow("6060-12-23")]
    [DataRow("7070-01-01")]
    [DataRow("dfbgdfdggfggg")]
    [DataRow("-2000")]
    [DataRow(null)]
    public void ValidatePastDate_InvalidPastDate_ReturnsFalse(string pastDate)
    {
        var result = ValidationHelper.ValidatePastDate(pastDate);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidatePastDate_InvalidPastDateAlwaysFuture_ReturnsFalse()
    {
        var result = ValidationHelper.ValidatePastDate(DateTime.UtcNow.AddDays(1).ToString("dd/MM/yyyy HH:mm:ss"));

        Assert.IsFalse(result);
    }
}
