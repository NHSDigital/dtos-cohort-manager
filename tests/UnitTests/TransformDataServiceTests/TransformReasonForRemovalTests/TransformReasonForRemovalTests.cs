namespace NHS.CohortManager.Tests.TransformDataServiceTests;

using Moq;
using Common;
using Model;
using NHS.CohortManager.CohortDistributionService;
using Data.Database;
using System.Data;

[TestClass]
public class TransformReasonForRemovalTests
{
    private readonly Mock<IExceptionHandler> _exceptionHandler = new();
    private readonly Mock<ITransformDataLookupFacade> _dataLookup = new();
    private readonly TransformReasonForRemoval _function;
    private readonly CohortDistributionParticipant _participant;
    public TransformReasonForRemovalTests()
    {
        Environment.SetEnvironmentVariable("ExceptionFunctionURL", "ExceptionFunctionURL");
        _function = new TransformReasonForRemoval(_exceptionHandler.Object, _dataLookup.Object);
        _participant = new CohortDistributionParticipant();
    }

    [TestMethod]
    public async Task Run_ValidParticipant_ReturnsExistingParticipant()
    {
        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant, null);

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
        var result = await _function.ReasonForRemovalTransformations(_participant, null);

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
        var existingParticipant = new CohortDistribution
        {
            PrimaryCareProvider = "ABCDEF"
        };

        _dataLookup.Setup(x => x.GetBsoCodeUsingPCP(It.IsAny<string>())).Returns("ABC");

        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant, existingParticipant);

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
    public async Task Run_InvalidParticipantForRule3_ReturnTransformedParticipant(string postcode)
    {
        // Arrange
        _participant.ReasonForRemoval = "RDR";
        _participant.Postcode = postcode;

        _dataLookup.Setup(x => x.ValidateOutcode(It.IsAny<string>())).Returns(postcode == "ValidPostcode");
        var existingParticipant = new CohortDistribution
        {
            PrimaryCareProvider = "ZZZABC",
            PrimaryCareProviderDate = DateTime.UtcNow,
            ReasonForRemoval = "RDI",
            ReasonForRemovalDate = DateTime.UtcNow
        };

        System.Console.WriteLine("RFR date: " + existingParticipant.ReasonForRemovalDate);
        System.Console.WriteLine("PCP date: " + existingParticipant.PrimaryCareProviderDate);

        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant, existingParticipant);

        // Assert
        Assert.AreEqual(result.PrimaryCareProvider, existingParticipant.PrimaryCareProvider);
        Assert.AreEqual(result.PrimaryCareProviderEffectiveFromDate, existingParticipant.PrimaryCareProviderDate?.ToString("yyyy-MM-dd"));
        Assert.AreEqual(result.ReasonForRemoval, existingParticipant.ReasonForRemoval);
        Assert.AreEqual(result.ReasonForRemovalEffectiveFromDate, existingParticipant.ReasonForRemovalDate?.ToString("yyyy-MM-dd"));

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
    public async Task Run_InvalidParticipantForRule4_ReturnsNull(string postcode)
    {
        // Arrange
        _participant.ReasonForRemoval = "RDR";
        _participant.Postcode = postcode;

        _dataLookup.Setup(x => x.ValidateOutcode(It.IsAny<string>())).Returns(postcode == "ValidPostcode");

        var existingParticipant = new CohortDistribution
        {
            PrimaryCareProvider = string.Empty
        };

        // Act
        var result = await _function.ReasonForRemovalTransformations(_participant, existingParticipant);

        // Assert
        Assert.AreEqual(new CohortDistributionParticipant().NhsNumber, result.NhsNumber);
        _exceptionHandler.Verify(handleException => handleException.CreateRecordValidationExceptionLog(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("4.ParticipantNotRegisteredToGPWithReasonForRemoval")),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Once());
    }
}
