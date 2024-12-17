namespace NHS.CohortManager.Tests.UnitTests.TransformDataServiceTests;

using Moq;
using Common;
using Model;
using NHS.CohortManager.CohortDistribution;
using Data.Database;

[TestClass]
public class TransformReasonForRemovalTests
{
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly Mock<IBsTransformationLookups> _transformationLookups = new();
    private readonly Mock<ITransformDataLookupFacade> _dataLookup = new();
    private readonly TransformReasonForRemoval _function;
    public TransformReasonForRemovalTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new TransformReasonForRemoval(_exceptionHandler.Object, _transformationLookups.Object, _dataLookup.Object);
    }

    [TestMethod]
    [DataRow("0000000000")]
    [DataRow("0")]
    public async Task Run_Something(string nhsNumber)
    {
        // Arrange
        var participant = new CohortDistributionParticipant() { NhsNumber = nhsNumber };

        // Act
        var result = await _function.ReasonForRemovalTransformations(participant);

        // Assert
        Assert.AreEqual(nhsNumber, result.NhsNumber);
    }
}
