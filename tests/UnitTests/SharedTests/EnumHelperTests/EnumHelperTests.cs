namespace NHS.CohortManager.Tests.UnitTests.EnumHelperTests;

using Common;
using Microsoft.Identity.Client;
using Model.Enums;

[TestClass]
public class EnumHelperTests 
{

    [TestMethod]
    public void GetDisplayName_ValidInput_ReturnDisplayName()
    {
        //No Arrange 
        //Act
        string result = EnumHelper.GetDisplayName(ServiceProvider.BSS);

        //Assert
        string expectedOutput = "BS SELECT";
        Assert.AreEqual(expectedOutput, result);

    }

    [TestMethod]
    public void GetDisplayName_NullInput_ReturnEmptyString() 
    {
        //No Arrange
        //Act
        string result = EnumHelper.GetDisplayName(null);

        //Assert
        string expectedOutput = "";
        Assert.AreEqual(expectedOutput, result);
    }

}
