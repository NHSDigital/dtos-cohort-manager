namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using NHS.CohortManager.DemographicServices;
using Common;
using Microsoft.Extensions.Logging;
using Moq;
using DataServices.Core;
using Model;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

[TestClass]
public class NhsNumberValidationTests
{
    public NhsNumberValidationTests()
    {
    }

    [DataTestMethod]
    [DataRow("1234567890", true)]  // Valid 10-digit number
    [DataRow("0123456789", true)]  // Valid with leading zero
    [DataRow("9876543210", true)]  // Valid 10-digit number
    [DataRow("123 456 7890", true)] // Valid with spaces
    [DataRow("12345678901", false)] // Too long (11 digits)
    [DataRow("123456789", false)]   // Too short (9 digits)
    [DataRow("12345678a0", false)]  // Contains letter
    [DataRow("123.456.789", false)] // Contains dots
    [DataRow("", false)]            // Empty string
    [DataRow(null, false)]          // Null
    [DataRow("   ", false)]         // Only spaces
    [DataRow("123-456-7890", false)] // Contains hyphens
    public void IsValidNhsNumber_VariousInputs_ReturnsExpectedResult(string nhsNumber, bool expectedResult)
    {
        // Arrange & Act
        var result = InvokeIsValidNhsNumber(nhsNumber);

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [TestMethod]
    public void IsValidNhsNumber_WithMultipleSpaces_RemovesAllSpaces()
    {
        // Arrange
        var nhsNumber = " 1 2 3 4 5 6 7 8 9 0 ";

        // Act
        var result = InvokeIsValidNhsNumber(nhsNumber);

        // Assert
        Assert.IsTrue(result, "Should remove all spaces and validate as 10 digits");
    }

    [TestMethod]
    public void IsValidNhsNumber_WithInternalSpaces_HandlesCorrectly()
    {
        // Arrange
        var nhsNumber = "12 34 56 78 90";

        // Act
        var result = InvokeIsValidNhsNumber(nhsNumber);

        // Assert
        Assert.IsTrue(result, "Should handle internal spaces correctly");
    }

    private bool InvokeIsValidNhsNumber(string nhsNumber)
    {
        // Use reflection to call the private static IsValidNhsNumber method
        var method = typeof(ManageNemsSubscription).GetMethod("IsValidNhsNumber", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        Assert.IsNotNull(method, "IsValidNhsNumber method should exist");
        
        return (bool)method.Invoke(null, new object[] { nhsNumber });
    }
}