namespace ValidationHelperTests;

using Common;

[TestClass]
public class ValidationHelperTests
{ 
    [TestMethod]
    [DataRow("20000101")]               //  yyyymmdd
    [DataRow("200001")]                 //  yyyymm
    [DataRow("2000-01-01")]             //  yyyy-mm-dd
    [DataRow("01/01/2000 12:00:00")]    //  dd/mm/yyyy hh:mm:ss
    //[DataRow("1/01/2000 12:00:00 01")]  //  d/mm/yyyy hh:mm:ss tt
    //[DataRow("01/01/2000 12:00:00 01")] //  dd/mm/yyyy hh:mm:ss tt
    [DataRow("2000")]                   //  yyyy
    
    public void ValidatePastDate_ValidInput_ReturnTrue(string date)
    {
        //No Arrange
        //Act
        var actual = ValidationHelper.ValidatePastDate(date);

        //Assert
        bool expected = true;
        Assert.AreEqual(actual, expected);
    }

    [TestMethod]
    [DataRow(null)]                     //  null
    [DataRow("20300101")]               //  future date
    [DataRow("2000-01-32")]             //  date doesn't exist
    [DataRow("1/13/2000 12:00:00 01")]  //  d/mm/yyyy hh:mm:ss tt
    [DataRow("01/01/2000 25:00:00 01")] //  dd/mm/yyyy hh:mm:ss tt
    [DataRow("-2000")]                  //  negative year
    public void ValidatePastDate_InvalidInput_ReturnFalse(string date) 
    {
        //No Arrange
        //Act
        var actual = ValidationHelper.ValidatePastDate(date);

        //Assert
        bool expected = false;
        Assert.AreEqual(actual, expected);
    }

    [TestMethod]
    [DataRow("8919361401")]     // These are random valid NHS numbers created by https://data-gorilla.uk/en/healthcare/nhs-number/
    [DataRow("4539728490")]     // This does not contain PII.
    [DataRow("1056154497")]
    [DataRow("1201484383")]
    [DataRow("8325769629")]
    public void ValidateNHSNumber_ValidInput_ReturnTrue(string nhsNumber)
    {
        //No Arrange
        //Act
        var actual = ValidationHelper.ValidateNHSNumber(nhsNumber);

        //Assert
        bool expected = true;
        Assert.AreEqual(actual, expected);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("1234567890")]  
    [DataRow("-1234567890")]    
    [DataRow("1a2b3c4d5e")]
    [DataRow("123")]
    [DataRow("0000000000")]
    public void ValidateNHSNumber_InvalidInput_ReturnFalse(string nhsNumber) 
    {
        //No Arrange
        //Act
        var actual = ValidationHelper.ValidateNHSNumber(nhsNumber);

        //Assert
        bool expected = false;
        Assert.AreEqual(actual, expected);
    }   
}