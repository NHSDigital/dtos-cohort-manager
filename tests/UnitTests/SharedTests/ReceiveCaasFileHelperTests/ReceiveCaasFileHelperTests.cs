using System;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Common;
using Model;
using Model.Enums;
using NHS.Screening.ReceiveCaasFile;

namespace Common.Tests
{
    [TestClass]
    public class ReceiveCaasFileHelperTests
    {
        private Mock<ILogger<ReceiveCaasFileHelper>> _mockLogger;
        private Mock<ICallFunction> _mockCallFunction;
        private ReceiveCaasFileHelper _helper;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ReceiveCaasFileHelper>>();
            _mockCallFunction = new Mock<ICallFunction>();
            _helper = new ReceiveCaasFileHelper(_mockLogger.Object, _mockCallFunction.Object);
        }

    [TestMethod]
    public async Task MapParticipant_ReturnsValidParticipant()
    {
        var rec = new ParticipantsParquetMap
        {
            Gender = (int)Gender.Female,
            EligibilityFlag = true
        };

        var result = await _helper.MapParticipant(rec, "scr123", "SCRName", "file.txt");

        Assert.IsNotNull(result);
        Assert.AreEqual("scr123", result.ScreeningId);
        Assert.AreEqual("1", result.EligibilityFlag);
        Assert.AreEqual(Gender.Female, result.Gender);
    }



    [TestMethod]
    public void BitStringFromNullableBoolean_ReturnsExpectedValues()
    {
        var method = typeof(ReceiveCaasFileHelper).GetMethod("BitStringFromNullableBoolean", BindingFlags.NonPublic | BindingFlags.Static);

        var resultTrue = method.Invoke(null, new object[] { (bool?)true });
        var resultFalse = method.Invoke(null, new object[] { (bool?)false });
        var resultNull = method.Invoke(null, new object[] { null });

        Assert.AreEqual("1", resultTrue);
        Assert.AreEqual("0", resultFalse);
        Assert.IsNull(resultNull);
    }

    [TestMethod]
    public async Task InsertValidationErrorIntoDatabase_LogsFailureProperly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("FileValidationURL", "http://dummy-url");

        var webResponseMock = new Mock<HttpWebResponse>();
        var mockResponse = webResponseMock.Object;
        _mockCallFunction
            .Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _helper.InsertValidationErrorIntoDatabase("fail.txt", "record");

        // Assert: verify log captured the filename
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("fail.txt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [TestMethod]
    public void GetUrlFromEnvironment_ReturnsUrl()
    {
        Environment.SetEnvironmentVariable("TestKey", "http://url");

        var result = _helper.GetUrlFromEnvironment("TestKey");

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

        // Verify logger
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


    [TestMethod]
    public async Task CheckFileName_ReturnsFalse_IfInvalid()
    {
        var parser = new FileNameParser("testname");

        Environment.SetEnvironmentVariable("FileValidationURL", "http://dummy-url");
        var mockResponse = new Mock<HttpWebResponse>();

        _mockCallFunction
            .Setup(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(mockResponse.Object);

        var result = await _helper.CheckFileName("badfile", parser, "error happened");

        Assert.IsFalse(result);
        _mockCallFunction.Verify(x => x.SendPost(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
    }
}
