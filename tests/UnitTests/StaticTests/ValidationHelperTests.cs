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
}
