using System.Linq.Expressions;
using Moq;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.TestUtilities;

public static class LoggerAssertions
{
    public static void VerifyLogger<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string expectedMessage)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == expectedMessage),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
    }

    public static void VerifyLogger<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string expectedMessage, Expression<Func<Exception?, bool>> exceptionMatch)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == expectedMessage),
                It.Is(exceptionMatch),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
    }

    public static void VerifyNoLogs<T>(this Mock<ILogger<T>> loggerMock, LogLevel level)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Never);
    }
}
