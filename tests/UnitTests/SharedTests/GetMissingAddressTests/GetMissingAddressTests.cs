namespace NHS.CohortManager.Tests.UnitTests.GetMissingAddressTests;

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Data.Database;
using Microsoft.Extensions.Logging;
using Moq;
using DataServices.Client;
using Model;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
public class MockDbConnection : IDbConnection
{
    public ConnectionState State { get; private set; } = ConnectionState.Closed;
    public string ConnectionString { get; set; }
    public int ConnectionTimeout => 0;
    public string Database => "MockDatabase";

    public IDbTransaction BeginTransaction() => null;
    public IDbTransaction BeginTransaction(IsolationLevel il) => null;
    public void ChangeDatabase(string databaseName) { }

    public void Close() => State = ConnectionState.Closed;
    public IDbCommand CreateCommand() => new MockDbCommand();
    public void Open() => State = ConnectionState.Open;
    public void Dispose() => Close();
}


public class MockDbParameter : IDbDataParameter
{
    public string ParameterName { get; set; }
    public object Value { get; set; }
    public DbType DbType { get; set; }
    public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public bool IsNullable => false;
    public string SourceColumn { get; set; }
    public DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;
    public int Size { get; set; }

    public byte Precision { get; set; }
    public byte Scale { get; set; }
}

public class MockParameterCollection : IDataParameterCollection
{
    private readonly List<IDataParameter> _parameters = new List<IDataParameter>();

    public object this[int index]
    {
        get => _parameters[index];
        set => _parameters[index] = (IDataParameter)value;
    }

    public object this[string parameterName]
    {
        get => _parameters.Find(p => p.ParameterName == parameterName);
        set
        {
            int index = _parameters.FindIndex(p => p.ParameterName == parameterName);
            if (index >= 0) _parameters[index] = (IDataParameter)value;
        }
    }

    public int Count => _parameters.Count;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public bool IsSynchronized => false;
    public object SyncRoot => this;

    public int Add(object value)
    {
        _parameters.Add((IDataParameter)value);
        return _parameters.Count - 1;
    }

    public void Clear() => _parameters.Clear();
    public bool Contains(object value) => _parameters.Contains((IDataParameter)value);
    public bool Contains(string parameterName) => _parameters.Exists(p => p.ParameterName == parameterName);
    public void CopyTo(Array array, int index) => _parameters.CopyTo((IDataParameter[])array, index);


    public IEnumerator GetEnumerator() => _parameters.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _parameters.GetEnumerator();

    public int IndexOf(object value) => _parameters.IndexOf((IDataParameter)value);
    public int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
    public void Insert(int index, object value) => _parameters.Insert(index, (IDataParameter)value);
    public void Remove(object value) => _parameters.Remove((IDataParameter)value);
    public void RemoveAt(int index) => _parameters.RemoveAt(index);
    public void RemoveAt(string parameterName) => _parameters.RemoveAt(IndexOf(parameterName));
}


public class MockDbCommand : IDbCommand
{
    public string CommandText { get; set; }
    public int CommandTimeout { get; set; }


    private System.Data.CommandType _commandType = System.Data.CommandType.Text;
    public System.Data.CommandType CommandType
    {
        get => _commandType;
        set => _commandType = value;
    }

    public IDbConnection Connection { get; set; }
    public IDataParameterCollection Parameters { get; } = new MockParameterCollection();
    public IDbTransaction Transaction { get; set; }
    public UpdateRowSource UpdatedRowSource { get; set; }

    public void Cancel() { }
    public IDbDataParameter CreateParameter() => new MockDbParameter();
    public int ExecuteNonQuery() => 1;
    public IDataReader ExecuteReader() => new MockDataReader();
    public IDataReader ExecuteReader(CommandBehavior behavior) => new MockDataReader();
    public object ExecuteScalar() => null;
    public void Prepare() { }
    public void Dispose() { }
}


public class MockDataReader : IDataReader
{
    private bool _hasRead = false;
    private bool _isClosed = false;

    public bool Read()
    {
        if (_hasRead) return false;
        _hasRead = true;
        return true;
    }

    public object this[int i] => "Test Data";
    public object this[string name] => name switch
    {
        "POST_CODE" => "AB12 3CD",
        "ADDRESS_LINE_1" => "123 Street",
        _ => DBNull.Value
    };

    public int Depth => 0;
    public bool IsClosed => _isClosed;
    public int RecordsAffected => 1;
    public int FieldCount => 2;

    public bool GetBoolean(int i) => true;
    public byte GetByte(int i) => 0;
    public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => 0;
    public char GetChar(int i) => 'A';
    public long GetChars(int i, long fieldOffset, char[] buffer, int bufferoffset, int length) => 0;
    public IDataReader GetData(int i) => this;
    public string GetDataTypeName(int i) => "string";
    public DateTime GetDateTime(int i) => DateTime.Now;
    public decimal GetDecimal(int i) => 0;
    public double GetDouble(int i) => 0;
    public Type GetFieldType(int i) => typeof(string);
    public float GetFloat(int i) => 0;
    public Guid GetGuid(int i) => Guid.NewGuid();
    public short GetInt16(int i) => 0;
    public int GetInt32(int i) => 0;
    public long GetInt64(int i) => 0;
    public string GetName(int i) => "COLUMN";
    public int GetOrdinal(string name) => 0;
    public string GetString(int i) => "Test Data";
    public object GetValue(int i) => "Test Data";
    public int GetValues(object[] values) => 1;
    public bool IsDBNull(int i) => false;

    public void Close() => _isClosed = true;
    public DataTable GetSchemaTable() => new DataTable();
    public bool NextResult() => false;

    public void Dispose()
    {
        Close();
    }
}

[TestClass]
public class GetMissingAddressTests
{
    private Mock<IDbConnection> _mockConnection;
    private Mock<IDbCommand> _mockCommand;
    private Mock<IDataReader> _mockReader;
    private Mock<IDbTransaction> _mockTransaction;
    private Mock<IDbDataParameter> _mockParameter;

    private CohortDistributionParticipant _participant;
    private GetMissingAddress _getMissingAddress;


[TestMethod]
public void GetAddress_ShouldExecuteSuccessfully()
{
    // Arrange
    var participant = new CohortDistributionParticipant
    {
        ParticipantId = "12345",
        Postcode = "AB12 3CD"
    };

    using (var connection = new MockDbConnection())
    {
        var getMissingAddress = new GetMissingAddress(participant, connection);

        // Act
        var result = getMissingAddress.GetAddress();

        // Assert
        Assert.IsNotNull(result);
    }
}

[TestMethod]
public void GetAddress_ShouldCloseConnection()
{
    // Arrange
    var participant = new CohortDistributionParticipant
    {
        ParticipantId = "12345",
        Postcode = "AB12 3CD"
    };

    var connection = new MockDbConnection();

    var getMissingAddress = new GetMissingAddress(participant, connection);

    // Act
    getMissingAddress.GetAddress();

    // Assert
    Assert.AreEqual(ConnectionState.Closed, connection.State, "The connection should be closed after execution.");
}

[TestMethod]
public void GetAddress_ShouldHandleExceptions()
{
    // Arrange
    var participant = new CohortDistributionParticipant
    {
        ParticipantId = "12345",
        Postcode = "AB12 3CD"
    };

    var connection = new MockDbConnection();

    var getMissingAddress = new GetMissingAddress(participant, connection);

    // Act & Assert
    try
    {
        getMissingAddress.GetAddress();
    }
    catch (Exception ex)
    {
        Assert.Fail($"Exception occurred: {ex.Message}");
    }
}


}

