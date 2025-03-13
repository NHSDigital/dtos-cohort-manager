using Microsoft.VisualBasic;
using NHS.CohortManager.Shared.Utilities;

namespace MappingUtilitiesTests;

[TestClass]
public class MappingUtilitiesTests
{
    private static readonly DateTime expectedResultDT = new DateTime(2000, 01, 01);

    [TestMethod]
    [DataRow("2000/01/01")]
    public void ParseNullableDateTime_ValidInput_ReturnDate(string date)
    {
        //No Arrange
        //Act
        var actual = MappingUtilities.ParseNullableDateTime(date);

        //Assert
        Assert.AreEqual(actual, expectedResultDT);
    }

    [TestMethod]
    [DataRow("2000/0/0")] //Invalid date
    [DataRow(null)] //Null input
    public void ParseNullableDateTime_InvalidInput_ReturnNull(string date)
    {
        //No Arrange
        //Act
        var actual = MappingUtilities.ParseNullableDateTime(date);

        //Assert
        Assert.IsNull(actual);
    }

    [TestMethod]
    [DataRow("0",(short)0)] 
    [DataRow("1",(short)1)] 
    [DataRow("N",(short)0)] 
    [DataRow("Y",(short)1)] 
    public void ParseStringFlag_ValidInput_ReturnDate(string flag, short expectedResult)
    {   
        //No Arrange
        //Act
        short actual = MappingUtilities.ParseStringFlag(flag);
    
        //Assert
        Assert.AreEqual(actual, expectedResult);
    }

    [TestMethod]
    [DataRow("W")]
    [DataRow(null)]
    public void ParseStringFlag_InvalidInput_ReturnArguementError(string flag)
    {
        //No Arrange or Act
        //Assert
        var exception = Assert.ThrowsException<ArgumentException>(() => MappingUtilities.ParseStringFlag(flag));
        Assert.AreEqual("Invalid input", exception.Message);
    }

    [TestMethod]
    [DataRow("01/01/2000")] // dd/mm/yyyy
    [DataRow("20000101")] // yyyymmdd
    [DataRow("1/1/2000")] // d/m/yyyy
    public void ParseDates_ValidInput_ReturnDate(string date)
    {
        //No Arrange
        //Act
        var actual = MappingUtilities.ParseDates(date);

        //Assert
        Assert.AreEqual(actual, expectedResultDT);
    }

    [TestMethod]
    [DataRow("00/00/2000")] // dd/mm/yyyy
    [DataRow("20000000")] // yyyymmdd
    [DataRow("0/0/2000")] // d/m/yyyy
    public void ParseDates_InvalidInput_ReturnNull(string date)
    {
        //No Arrange
        //Act
        var actual = MappingUtilities.ParseDates(date);

        //Assert 
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void FormatDateTime_ValidInput_ReturnDate()
    {
        //Arrange
        string expectedResult = "2000-01-01";

        //Act
        var actual = MappingUtilities.FormatDateTime(expectedResultDT);

        //Assert 
        Assert.AreEqual(actual, expectedResult);
    }

    [TestMethod]
    public void FormatDateTime_InvalidInput_ReturnNull()
    {
        //No Arrange
        //Act
        var actual = MappingUtilities.FormatDateTime(null);

        //Assert 
        Assert.IsNull(actual);
    }
}