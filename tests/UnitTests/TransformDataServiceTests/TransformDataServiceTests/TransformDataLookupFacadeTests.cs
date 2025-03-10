using DataServices.Client;
using Model;
using Moq;
using NHS.CohortManager.CohortDistribution;

namespace NHS.CohortManager.Tests.TransformDataServiceTests;

[TestClass]
public class TransformDataLookupFacadeTests
{
    private readonly TransformDataLookupFacade _sut;
    private Mock<IDataServiceClient<BsSelectOutCode>> _outcodeClientMock = new();
    private Mock<IDataServiceClient<BsSelectGpPractice>> _gpPracticeClientMock = new();

    public TransformDataLookupFacadeTests()
    {
        _outcodeClientMock
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(new BsSelectOutCode());

        _sut = new(_outcodeClientMock.Object, _gpPracticeClientMock.Object);
    }

    [TestMethod]
    [DataRow("ec1a1bb")]
    [DataRow("EC1A1BB")]
    [DataRow("ec1a 1bb")]
    [DataRow("EC1A 1BB")]
    [DataRow("W1A 0AX")]
    [DataRow("M1 1AE")]
    [DataRow("B33 8TH")]
    [DataRow("CR2 6XH")]
    [DataRow("LS10 1LT")]
    public void ValidateOutcode_ValidPostcode_ReturnTrue(string postcode)
    {
        // Act
        bool result = _sut.ValidateOutcode(postcode);

        // Assert
        Assert.IsTrue(result);
    }

    public void ValidateOutcode_OutcodeNotFound_ReturnFalse()
    {
        // Arrange
        _outcodeClientMock
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((BsSelectOutCode) null);

        string postcode = "LS10 1LT";

        // Act
        bool result = _sut.ValidateOutcode(postcode);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("ABC123")]
    [DataRow("ABC 123")]
    public void ValidateOutcode_InvalidPostcode_ThrowTransformationException(string postcode)
    {
        // Act & Assert
        Assert.ThrowsException<TransformationException>(() => _sut.ValidateOutcode(postcode));
    }

}
