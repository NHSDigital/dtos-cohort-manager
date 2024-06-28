namespace NHS.CohortManager.Tests.RemoveFromAggregateTests;



using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Common;
using Data.Database;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Model;
using Model.Enums;
using Moq;

[TestClass]
public class RemoveFromAggregateTests
{
    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly Mock<ILogger<UpdateAggregateData>> _logger = new();
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();
    private readonly Mock<IDbDataParameter> _mockParameter = new();

    public RemoveFromAggregateTests()
    {
        Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
        _mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeCOnnectionString");
        _mockDBConnection.Setup(x => x.BeginTransaction()).Returns(_mockTransaction.Object);

        _commandMock.Setup(c => c.Dispose());
        _commandMock.SetupSequence(m => m.Parameters.Add(It.IsAny<IDbDataParameter>()));
        _commandMock.Setup(m => m.Parameters.Clear()).Verifiable();
        _commandMock.SetupProperty<System.Data.CommandType>(c => c.CommandType);
        _commandMock.SetupProperty<string>(c => c.CommandText);
        _commandMock.Setup(x => x.CreateParameter()).Returns(_mockParameter.Object);

        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
        _mockDBConnection.Setup(conn => conn.Open());



    }
    [TestMethod]
    public void UpdateAggregateParticipantAsInactive_Success ()
    {
        //Arrange
        var updateAggregateData = new UpdateAggregateData(
            _mockDBConnection.Object,
            _logger.Object

        );
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(1);
        var NHSID = "123456";

        //Act
        var result = updateAggregateData.UpdateAggregateParticipantAsInactive(NHSID);


        //Assert
        Assert.IsTrue(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

    [TestMethod]
    public void UpdateAggregateParticipantAsInactive_ParticipantNotExists_Failure ()
    {
        //Arrange
        var updateAggregateData = new UpdateAggregateData(
            _mockDBConnection.Object,
            _logger.Object

        );
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);
        var NHSID = "654321";
        //Act
        var result = updateAggregateData.UpdateAggregateParticipantAsInactive(NHSID);


        //Assert
        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
    }

        [TestMethod]
    public void UpdateAggregateParticipantAsInactive_NoNHSIDProvided_Failure ()
    {
        //Arrange
        var updateAggregateData = new UpdateAggregateData(
            _mockDBConnection.Object,
            _logger.Object

        );
        _commandMock.Setup(x => x.ExecuteNonQuery()).Returns(0);
        var NHSID = "";
        //Act
        var result = updateAggregateData.UpdateAggregateParticipantAsInactive(NHSID);


        //Assert
        Assert.IsFalse(result);
        _commandMock.Verify(m => m.ExecuteNonQuery(), Times.Never);
    }

}
