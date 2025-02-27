namespace NHS.CohortManager.Tests.UnitTests.CohortDistributionTests.RetrieveCohortRequestAuditTests
{
    using System.Data;
    using Data.Database;
    using Microsoft.Extensions.Logging;
    using Moq;

    public abstract class CohortDistributionDataBase
    {
        protected readonly Mock<IDbConnection> _mockDBConnection = new();
        protected readonly Mock<IDbCommand> _commandMock = new();
        protected readonly Mock<IDataReader> _mockDataReader = new();
        protected readonly Mock<ILogger<CreateCohortDistributionData>> _loggerMock = new();
        protected readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
        protected readonly Mock<IDbDataParameter> _mockParameter = new();
        protected readonly Mock<IDbTransaction> _mockTransaction = new();
        protected CreateCohortDistributionData _createCohortDistributionDataService;

        protected CohortDistributionDataBase()
        {
            Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
            Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

            _mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeConnectionString");
            _mockDBConnection.Setup(x => x.BeginTransaction()).Returns(_mockTransaction.Object);

            _commandMock.Setup(c => c.Dispose());
            _commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
            _commandMock.Setup(m => m.Parameters.Clear()).Verifiable();
            _commandMock.SetupProperty<System.Data.CommandType>(c => c.CommandType);
            _commandMock.SetupProperty<string>(c => c.CommandText);
            _commandMock.Setup(x => x.CreateParameter()).Returns(_mockParameter.Object);

            _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
            _commandMock.Setup(m => m.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
            _commandMock.Setup(m => m.ExecuteReader())
                .Returns(_mockDataReader.Object);
            _mockDBConnection.Setup(conn => conn.Open());

            _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)
            .Returns(false);

            _createCohortDistributionDataService = new CreateCohortDistributionData(
                _mockDBConnection.Object,
                _loggerMock.Object);
        }
    }
}
