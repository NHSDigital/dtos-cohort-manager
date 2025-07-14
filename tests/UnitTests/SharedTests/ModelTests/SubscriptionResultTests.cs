using Microsoft.VisualStudio.TestTools.UnitTesting;
using Model;

namespace UnitTests.SharedTests.ModelTests;

[TestClass]
public class SubscriptionResultTests
{
    [TestMethod]
    public void Success_WithSubscriptionId_ReturnsSuccessResult()
    {
        // Arrange
        var subscriptionId = "test-subscription-id";

        // Act
        var result = SubscriptionResult.CreateSuccess(subscriptionId);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(subscriptionId, result.SubscriptionId);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Failure_WithErrorMessage_ReturnsFailureResult()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = SubscriptionResult.CreateFailure(errorMessage);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.SubscriptionId);
        Assert.AreEqual(errorMessage, result.ErrorMessage);
    }

    [TestMethod]
    public void Success_WithNullSubscriptionId_ReturnsSuccessResult()
    {
        // Act
        var result = SubscriptionResult.CreateSuccess(null);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNull(result.SubscriptionId);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Failure_WithNullErrorMessage_ReturnsFailureResult()
    {
        // Act
        var result = SubscriptionResult.CreateFailure(null);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.SubscriptionId);
        Assert.IsNull(result.ErrorMessage);
    }
}