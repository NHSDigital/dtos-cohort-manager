namespace NHS.CohortManager.Tests.UnitTests.EnumHelperTests;

using Common;
using Microsoft.Identity.Client;
using Model.Enums;

[TestClass]
public class EnumHelperTests 
{

    [TestMethod]
    public void ReturnDisplayNameWhenValidInput ()
    {
        //No Arrange 
        //Act
        string result = EnumHelper.GetDisplayName(ServiceProvider.BSS);

        //Assert
        string expectedOutput = "BS SELECT";
        Assert.AreEqual(expectedOutput, result);

    }

    [TestMethod]
    public void ReturnEmptyStringWhenDisplayNameIsNull() 
    {
        //No Arrange
        //Act
        string result = EnumHelper.GetDisplayName(null);

        //Assert
        string expectedOutput = "";
        Assert.AreEqual(expectedOutput, result);
    }

    // May remove this and associated method after discussion with team
    [TestMethod]
    public void ReturnListOfHTTPStatusCodesCorrectly() 
    {
        //No Arrange
        //Act
        List<string> actual = EnumHelper.GetHttpStatusCodeStringList();

        //Assert
        List<string> expectedList = ["100","101","102","103","200","201","202","203","204","205","206","207","208","226","300","300","301","301","302","302","303","303","304","305","306","307","307","308","400","401","402","403","404","405","406","407","408","409","410","411","412","413","414","415","416","417","421","422","422","423","424","426","428","429","431","451","500","501","502","503","504","505","506","507","508","510","511"];
        CollectionAssert.AreEqual(expectedList, actual);
    }

}