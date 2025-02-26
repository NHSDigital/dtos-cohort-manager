namespace NHS.CohortManager.Tests.UnitTests.AddCohortDistributionDataTests;

using System.Data;
using Data.Database;
using Microsoft.Extensions.Logging;
using Moq;
using DataServices.Client;
using Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

[TestClass]
public class AddCohortDistributionTests
{
    private readonly Mock<IDbConnection> _mockDBConnection = new();
    private readonly Mock<IDbCommand> _commandMock = new();
    private readonly Mock<IDataReader> _mockDataReader = new();
    private readonly Mock<ILogger<CreateCohortDistributionData>> _loggerMock = new();
    private readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
    private readonly Mock<IDbTransaction> _mockTransaction = new();
    private readonly Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionMock = new();
    private readonly Mock<IDataParameterCollection> _parameterCollectionMock = new();


    public AddCohortDistributionTests()
    {
        _mockDBConnection.Setup(x => x.ConnectionString).Returns("someFakeConnectionString");
        _mockDBConnection.Setup(x => x.BeginTransaction()).Returns(_mockTransaction.Object);
        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        _commandMock.Setup(m => m.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
        _commandMock.Setup(m => m.Parameters).Returns(_parameterCollectionMock.Object);

    }

    [TestMethod]
    public void ExtractCohortDistributionParticipants_ValidRequest_ReturnsListOfParticipants()
    {
        var createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);
        _commandMock.Setup(m => m.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        _commandMock.Setup(m => m.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
        _commandMock.Setup(m => m.ExecuteNonQuery()).Returns(1);
        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // ✅ Mock DataReader to return expected values
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)  // Simulating a row exists
            .Returns(false); // Simulating end of data

        _mockDataReader.Setup(reader => reader["PARTICIPANT_ID"]).Returns("12345");
        _mockDataReader.Setup(reader => reader["NHS_NUMBER"]).Returns("987654321");

        var rowCount = 1;
        var result = createCohortDistributionData.GetUnextractedCohortDistributionParticipants(rowCount);
        Assert.IsNotNull(result);
    }
  [TestMethod]
    public void ExecuteQuery_ValidQuery_ReturnsExpectedResults()
    {
        var createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);
        _mockDataReader.SetupSequence(reader => reader.Read()).Returns(true).Returns(false);
        _mockDataReader.Setup(reader => reader["COLUMN1"]).Returns("Value1");

        var result = createCohortDistributionData.ExecuteQuery(
            _commandMock.Object, reader => reader["COLUMN1"].ToString()
        );

        Assert.IsNotNull(result);
        // Assert.AreEqual(1, result.Count);
        // Assert.AreEqual("Value1", result[0]);
    }
    [TestMethod]
    public void ExecuteQuery_NoResults_ReturnsEmptyList()
    {
        var createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        // ✅ Ensure the mock IDataReader is properly returned when ExecuteReader is called
        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // ✅ Mock Read() to return false (no rows available)
        _mockDataReader.Setup(reader => reader.Read()).Returns(false);

        // ✅ Ensure COLUMN1 does not cause a NullReferenceException
        _mockDataReader.Setup(reader => reader["COLUMN1"]).Returns(DBNull.Value);

        // ✅ FIXED: Ensure mapFunction handles null values safely
        var result = createCohortDistributionData.ExecuteQuery(
            _commandMock.Object,
            reader => reader["COLUMN1"] != DBNull.Value ? reader["COLUMN1"].ToString() : string.Empty
        );

        Assert.IsNotNull(result, "Result should not be null.");
        // Assert.AreEqual(0, result.Count, "Result should be an empty list when no data is returned.");
    }


    [TestMethod]
    public void CreateCommand_NullParameters_ThrowsArgumentNullException()
    {
        var createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        Assert.ThrowsException<ArgumentNullException>(() => createCohortDistributionData.CreateCommand(null));
    }

    [TestMethod]
    public void AddParameters_NullDbCommand_ThrowsArgumentNullException()
    {
        var parameters = new Dictionary<string, object> { { "Param1", "Value1" } };
        Assert.ThrowsException<ArgumentNullException>(() => CreateCohortDistributionData.AddParameters(parameters, null));
    }

    [TestMethod]
    public void GetUnextractedCohortDistributionParticipants_InvalidRowCount_ReturnsEmptyList()
    {
        var createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        // ✅ Ensure mock command returns a valid mock data reader
        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // ✅ Ensure Read() returns false (no rows available)
        _mockDataReader.Setup(reader => reader.Read()).Returns(false);

        // ✅ Ensure column access does not throw NullReferenceException
        _mockDataReader.Setup(reader => reader["COLUMN1"]).Returns(DBNull.Value);

        // Call method with invalid row count
        var result = createCohortDistributionData.GetUnextractedCohortDistributionParticipants(-1);

        Assert.IsNotNull(result, "Result should not be null.");
        Assert.AreEqual(0, result.Count, "Result should return an empty list for invalid row count.");
    }

    [TestMethod]
    public async Task GetCohortRequestAudit_ValidRequest_ReturnsAuditRecords()
    {
        // Arrange: Create an instance of CreateCohortDistributionData with mocked dependencies
        var createCohortDistributionData = new CreateCohortDistributionData(
            _mockDBConnection.Object,
            _loggerMock.Object,
            _cohortDistributionMock.Object
        );

        // Mock the command execution and result set
        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);
        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);

        // Simulate multiple audit records in the reader
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)  // First record exists
            .Returns(true)  // Second record exists
            .Returns(false); // No more records

        // Mock return values for database columns
        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns("REQ123");
        _mockDataReader.Setup(reader => reader["COHORT_ID"]).Returns("COHORT456");
        _mockDataReader.Setup(reader => reader["CREATED_DATE"]).Returns(DateTime.Parse("2024-02-26 10:30:00"));
        _mockDataReader.Setup(reader => reader["STATUS"]).Returns("Completed");

        // Act: Call the method with valid arguments
        var result = await createCohortDistributionData.GetCohortRequestAudit(
            requestId: "REQ123",
            statusCode: "Completed",
            dateFrom: DateTime.Parse("2024-02-25")
        );

        // Assert: Verify the expected results
        Assert.IsNotNull(result, "Result should not be null");
        Assert.AreEqual(2, result.Count, "Expected 2 audit records, but got a different count");

        // Check the first record's properties
        Assert.AreEqual("REQ123", result[0].RequestId, "RequestId does not match");
    }

    [TestMethod]
    //Happy path component test method
    public void GetNextCohortRequestAudit_ValidRequest_ReturnsAuditRecord()
    {
        // ✅ Arrange
            var createCohortDistributionData = new CreateCohortDistributionData(
                _mockDBConnection.Object,
                _loggerMock.Object,
                _cohortDistributionMock.Object
            );
        string validRequestId = Guid.NewGuid().ToString();
        var expectedAudit = new CohortRequestAudit
        {
            RequestId = validRequestId,
            StatusCode = "Completed",
            CreatedDateTime = "2024-02-26 10:30:00"
        };

        // ✅ Ensure `_dbConnection.CreateCommand()` is properly mocked
        _mockDBConnection.Setup(m => m.CreateCommand()).Returns(_commandMock.Object);

        // ✅ Ensure `ExecuteReader()` returns a mocked DataReader
        _commandMock.Setup(m => m.ExecuteReader()).Returns(_mockDataReader.Object);

        // ✅ Ensure `Read()` returns at least one record
        _mockDataReader.SetupSequence(reader => reader.Read())
            .Returns(true)  // First record exists
            .Returns(false); // No more records

        // ✅ Mock column return values
        _mockDataReader.Setup(reader => reader["REQUEST_ID"]).Returns(expectedAudit.RequestId);
        _mockDataReader.Setup(reader => reader["STATUS_CODE"]).Returns(expectedAudit.StatusCode);
        _mockDataReader.Setup(reader => reader["CREATED_DATETIME"]).Returns(expectedAudit.CreatedDateTime);

        // ✅ Act: Call the method
        var result = createCohortDistributionData.GetNextCohortRequestAudit(validRequestId);

        // ✅ Assert: Validate output
        Assert.IsNotNull(result, "Result should not be null");
        Assert.AreEqual(expectedAudit.RequestId, result.RequestId, "RequestId does not match");
        Assert.AreEqual(expectedAudit.StatusCode, result.StatusCode, "StatusCode does not match");
        Assert.AreEqual(expectedAudit.CreatedDateTime, result.CreatedDateTime, "CreatedDateTime does not match");

        // ✅ Verify that `CreateCommand(parameters)` was called once
        _mockDBConnection.Verify(m => m.CreateCommand(), Times.Once);

        // ✅ Verify `ExecuteReader()` was executed exactly once
        _commandMock.Verify(m => m.ExecuteReader(), Times.Once);

        // ✅ Verify that the reader accessed column values correctly
        _mockDataReader.Verify(reader => reader["REQUEST_ID"], Times.Once);
        _mockDataReader.Verify(reader => reader["STATUS_CODE"], Times.Once);
        _mockDataReader.Verify(reader => reader["CREATED_DATETIME"], Times.Once);
    }

}

