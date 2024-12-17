namespace NHS.CohortManager.Tests.UnitTests.TransformDataServiceTests;

using Moq;
using Common;
using Model;
using NHS.CohortManager.CohortDistribution;
using Data.Database;
using System.Data;

[TestClass]
public class TransformReasonForRemovalTests
{
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly Mock<IBsTransformationLookups> _transformationLookups = new();
    private readonly Mock<ITransformDataLookupFacade> _dataLookup = new();
    private readonly TransformReasonForRemoval _function;
    private readonly CohortDistributionParticipant _participant;
    public TransformReasonForRemovalTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new TransformReasonForRemoval(_exceptionHandler.Object, _transformationLookups.Object, _dataLookup.Object);
        _participant = new CohortDistributionParticipant();
    }

    [TestMethod]
    public async Task Run_ValidParticipant_ReturnsExistingParticipant()
    {
        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant);

        // Assert
        Assert.AreEqual(_participant, result);
        _exceptionHandler.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("ParticipantNotRegisteredToGPWithReasonForRemoval")),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Never());
    }

    [TestMethod]
    [DataRow("RDR")]
    [DataRow("RDI")]
    [DataRow("RPR")]
    public async Task Run_InvalidParticipantForRule1_ReturnsTransformedParticipant(string reasonForRemoval)
    {
        // Arrange
        var validPostcode = "AL1 1BB";
        _participant.ReasonForRemoval = reasonForRemoval;
        _participant.ReasonForRemovalEffectiveFromDate = "2/10/2024";
        _participant.Postcode = validPostcode;

        _dataLookup.Setup(x => x.ValidateOutcode(It.IsAny<string>())).Returns(true);
        _dataLookup.Setup(x => x.GetBsoCode(It.IsAny<string>())).Returns("ABC");

        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant);

        // Assert
        Assert.AreEqual("ZZZABC", result.PrimaryCareProvider);
        Assert.AreEqual("2/10/2024", result.PrimaryCareProviderEffectiveFromDate);
        Assert.AreEqual(null, result.ReasonForRemoval);
        Assert.AreEqual(null, result.ReasonForRemovalEffectiveFromDate);
        _exceptionHandler.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("ParticipantNotRegisteredToGPWithReasonForRemoval")),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("InvalidPostcode")]
    public async Task Run_InvalidParticipantForRule2_ReturnsTransformedParticipant(string postcode)
    {
        // Arrange
        _participant.ReasonForRemoval = "RDR";
        _participant.ReasonForRemovalEffectiveFromDate = "2/10/2024";
        _participant.Postcode = postcode;

        _dataLookup.Setup(x => x.ValidateOutcode(It.IsAny<string>())).Returns(false);
        _transformationLookups.Setup(x => x.GetPrimaryCareProvider(It.IsAny<string>())).Returns("ABCDEF");
        _transformationLookups.Setup(x => x.GetBsoCodeUsingPCP(It.IsAny<string>())).Returns("ABC");

        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant);

        // Assert
        Assert.AreEqual($"ZZZABC", result.PrimaryCareProvider);
        Assert.AreEqual("2/10/2024", result.PrimaryCareProviderEffectiveFromDate);
        Assert.AreEqual(null, result.ReasonForRemoval);
        Assert.AreEqual(null, result.ReasonForRemovalEffectiveFromDate);
        _exceptionHandler.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("ParticipantNotRegisteredToGPWithReasonForRemoval")),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Never());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("InvalidPostcode")]
    public async Task Run_InvalidParticipantForRule3_ReturnsParticipantAndRaisesException(string postcode)
    {
        // Arrange
        _participant.ReasonForRemoval = "RDR";
        _participant.Postcode = postcode;

        _dataLookup.Setup(x => x.ValidateOutcode(It.IsAny<string>())).Returns(postcode == "ValidPostcode");
        _transformationLookups.Setup(x => x.GetPrimaryCareProvider(It.IsAny<string>())).Returns("ZZZABC");

        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant);

        // Assert
        Assert.AreEqual(_participant, result);
        _exceptionHandler.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("3.ParticipantNotRegisteredToGPWithReasonForRemoval")),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Once());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("InvalidPostcode")]
    public async Task Run_InvalidParticipantForRule4_ReturnsParticipantAndRaisesException(string postcode)
    {
        // Arrange
        _participant.ReasonForRemoval = "RDR";
        _participant.Postcode = postcode;

        _dataLookup.Setup(x => x.ValidateOutcode(It.IsAny<string>())).Returns(postcode == "ValidPostcode");
        _transformationLookups.Setup(x => x.GetPrimaryCareProvider(It.IsAny<string>())).Returns(string.Empty);

        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant);

        // Assert
        Assert.AreEqual(_participant, result);
        _exceptionHandler.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("4.ParticipantNotRegisteredToGPWithReasonForRemoval")),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Once());
    }
}
