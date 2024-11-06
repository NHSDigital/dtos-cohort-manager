namespace NHS.CohortManager.Tests.TestUtils
{
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Net;
    using Moq;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Azure.Functions.Worker;
    using System;
    using Common;
    using System.Data;
    using Data.Database;
    using Microsoft.Extensions.Logging;
    using System.Reflection;

    public abstract class DatabaseTestBaseSetup<TService> where TService : class
    {
        protected readonly Mock<IDbConnection> _mockDBConnection = new();
        protected readonly Mock<IDbCommand> _commandMock = new();
        protected Mock<IDataReader> _mockDataReader = new();
        protected readonly Mock<ILogger<TService>> _loggerMock = new();
        protected readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
        protected readonly Mock<IDbDataParameter> _mockParameter = new();
        protected readonly Mock<IDbTransaction> _mockTransaction = new();
        protected TService _service;
        protected Mock<HttpRequestData> _request;
        protected Mock<FunctionContext> _context = new();
        protected Mock<ICreateResponse> _createResponseMock = new();

        protected DatabaseTestBaseSetup(Func<IDbConnection, ILogger<TService>, IDbTransaction, IDbCommand, ICreateResponse, TService> serviceFactory)
        {
            _service = serviceFactory(_mockDBConnection.Object, _loggerMock.Object, _mockTransaction.Object, _commandMock.Object, _createResponseMock.Object);

            Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
            Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

            _mockDBConnection.Setup(s => s.ConnectionString).Returns("someFakeConnectionString");
            _mockDBConnection.Setup(s => s.BeginTransaction()).Returns(_mockTransaction.Object);
            _mockDBConnection.Setup(s => s.CreateCommand()).Returns(_commandMock.Object);
            _mockDBConnection.Setup(s => s.Open());

            _commandMock.Setup(s => s.Dispose());
            _commandMock.Setup(s => s.Parameters.Add(It.IsAny<IDbDataParameter>())).Verifiable();
            _commandMock.SetupProperty<System.Data.CommandType>(s => s.CommandType);
            _commandMock.SetupProperty<string>(c => c.CommandText);
            _commandMock.Setup(s => s.CreateParameter()).Returns(_mockParameter.Object);
            _commandMock.Setup(s => s.ExecuteReader()).Returns(_mockDataReader.Object);

            _mockDataReader.SetupSequence(reader => reader.Read()).Returns(true).Returns(false);
        }

        public Mock<HttpRequestData> SetupRequest(string json)
        {
            var byteArray = Encoding.ASCII.GetBytes(json);
            var bodyStream = new MemoryStream(byteArray);

            _request = new Mock<HttpRequestData>(_context.Object);
            _request.Setup(s => s.Body).Returns(bodyStream);
            _request.Setup(s => s.CreateResponse()).Returns(() =>
            {
                var response = new Mock<HttpResponseData>(_context.Object);
                response.SetupProperty(s => s.Headers, new HttpHeadersCollection());
                response.SetupProperty(s => s.StatusCode);
                response.SetupProperty(s => s.Body, new MemoryStream());
                return response.Object;
            });

            return _request;
        }

        public Mock<ICreateResponse> CreateHttpResponseMock()
        {
            _createResponseMock.Setup(s => s.CreateHttpResponse(It.IsAny<HttpStatusCode>(), It.IsAny<HttpRequestData>(), It.IsAny<string>()))
                .Returns((HttpStatusCode statusCode, HttpRequestData req, string responseBody) =>
                {
                    var response = req.CreateResponse(statusCode);
                    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                    response.WriteString(responseBody);
                    return response;
                });

            return _createResponseMock;
        }

        public void SetupRequestWithQueryParams(Dictionary<string, string> queryParams)
        {
            var queryCollection = new NameValueCollection();
            foreach (var param in queryParams)
            {
                queryCollection.Add(param.Key, param.Value);
            }
            _request.Setup(s => s.Query).Returns(queryCollection);
        }

        public void SetupDataReader<T>(List<T> dataList, Dictionary<string, string> columnToClassPropertyMapping)
        {
            var classProperties = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);
            SetupReadSequence(dataList.Count);
            SetupColumnMappings(dataList, columnToClassPropertyMapping, classProperties);
        }

        private void SetupReadSequence(int count)
        {
            var sequenceSetup = _mockDataReader.SetupSequence(r => r.Read());
            for (int i = 0; i < count; i++)
            {
                sequenceSetup = sequenceSetup.Returns(true);
            }
            sequenceSetup.Returns(false);
        }

        private void SetupColumnMappings<T>(List<T> dataList, Dictionary<string, string> columnToClassPropertyMapping, Dictionary<string, PropertyInfo> classProperties)
        {
            var currentIndex = 0;

            _mockDataReader
                .Setup(r => r[It.IsAny<string>()])
                .Returns((string columnName) =>
                {
                    if (currentIndex < dataList.Count
                        && columnToClassPropertyMapping.TryGetValue(columnName, out string propertyName)
                        && classProperties.TryGetValue(propertyName, out var property))
                    {
                        var value = property.GetValue(dataList[currentIndex]);
                        currentIndex++;
                        return value;
                    }
                    return null;
                });
        }
    }
}

