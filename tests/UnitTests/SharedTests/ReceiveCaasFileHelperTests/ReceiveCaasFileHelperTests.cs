using Microsoft.Extensions.Logging;
using Moq;
using Model;
using Model.Enums;

namespace Common.Tests
{
    [TestClass]
    public class ReceiveCaasFileHelperTests
    {
        private Mock<ILogger<ReceiveCaasFileHelper>> _mockLogger;
        private ReceiveCaasFileHelper _helper;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ReceiveCaasFileHelper>>();
            _helper = new ReceiveCaasFileHelper(_mockLogger.Object);
        }

        [TestMethod]
        public async Task MapParticipant_ReturnsValidParticipant()
        {
            // Arrange
            var rec = new ParticipantsParquetMap
            {
                Gender = (int)Gender.Female,
                EligibilityFlag = true
            };

            // Act
            var result = _helper.MapParticipant(rec, "scr123", "SCRName", "file.txt");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("scr123", result.ScreeningId);
            Assert.AreEqual("1", result.EligibilityFlag);
            Assert.AreEqual(Gender.Female, result.Gender);
        }


        [TestMethod]
        public void GetUrlFromEnvironment_ReturnsUrl()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TestKey", "http://url");

            // Act
            var result = _helper.GetUrlFromEnvironment("TestKey");

            // Assert
            Assert.AreEqual("http://url", result);
        }

        [TestMethod]
        public void GetUrlFromEnvironment_ThrowsIfMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("MissingKey", null);

            // Act + Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                _helper.GetUrlFromEnvironment("MissingKey"));

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("Environment variable is not set.")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

    }
}
