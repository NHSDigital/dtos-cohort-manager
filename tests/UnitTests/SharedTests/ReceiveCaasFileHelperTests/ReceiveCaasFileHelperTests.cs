using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Model;
using Model.Enums;
using NHS.Screening.ReceiveCaasFile;

namespace Common.Tests
{
    [TestClass]
    public class ReceiveCaasFileHelperTests
    {
        private readonly Mock<ILogger<ReceiveCaasFileHelper>> _logger = new();
        private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
        private readonly ReceiveCaasFileHelper _sut;

        public ReceiveCaasFileHelperTests()
        {
            _sut = new ReceiveCaasFileHelper(_logger.Object, _httpClientFunction.Object);
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
            var result = await _sut.MapParticipant(rec, "scr123", "SCRName", "file.txt");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("scr123", result.ScreeningId);
            Assert.AreEqual("1", result.EligibilityFlag);
            Assert.AreEqual(Gender.Female, result.Gender);
        }

        [TestMethod]
        public async Task InsertValidationErrorIntoDatabase_LogsFailureProperly()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FileValidationURL", "http://dummy-url");

            _httpClientFunction
                .Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

            // Act
            await _sut.InsertValidationErrorIntoDatabase("fail.txt", "record");

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("fail.txt")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public void GetUrlFromEnvironment_ReturnsUrl()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TestKey", "http://url");

            // Act
            var result = _sut.GetUrlFromEnvironment("TestKey");

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
                _sut.GetUrlFromEnvironment("MissingKey"));

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Environment variable is not set.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task CheckFileName_ReturnsFalse_IfInvalid()
        {
            // Arrange
            var parser = new FileNameParser("testname");
            Environment.SetEnvironmentVariable("FileValidationURL", "http://dummy-url");

            _httpClientFunction
                .Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

            // Act
            var result = await _sut.CheckFileName("badfile", parser, "error happened");

            // Assert
            Assert.IsFalse(result);
            _httpClientFunction.Verify(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
