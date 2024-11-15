
namespace NHS.CohortManager.Tests.UnitTests
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
    using Common.Interfaces;

    public abstract class DatabaseTestBaseSetup<TService> where TService : class
    {
        protected readonly Mock<IDbConnection> _mockDBConnection = new();
        protected readonly Mock<IDbCommand> _commandMock = new();
        protected readonly Mock<ILogger<TService>> _loggerMock = new();
        protected readonly Mock<IDatabaseHelper> _databaseHelperMock = new();
        protected readonly Mock<IDbDataParameter> _mockParameter = new();
        protected readonly Mock<IDbTransaction> _mockTransaction = new();
        protected readonly Mock<IValidationExceptionData> _validationDataMock = new();
        protected readonly Mock<IHttpParserHelper> _httpParserHelperMock = new();
        protected Mock<HttpRequestData> _request;
        protected Mock<IDataReader> _mockDataReader = new();
        protected Mock<FunctionContext> _context = new();
        protected Mock<ICreateResponse> _createResponseMock = new();
        protected TService _service;
        protected DatabaseTestBaseSetup(Func<IDbConnection, ILogger<TService>, IDbTransaction, IDbCommand, ICreateResponse, TService> serviceFactory)
        {
            _service = serviceFactory(_mockDBConnection.Object, _loggerMock.Object, _mockTransaction.Object, _commandMock.Object, _createResponseMock.Object);

            Environment.SetEnvironmentVariable("DtOsDatabaseConnectionString", "DtOsDatabaseConnectionString");
            Environment.SetEnvironmentVariable("LookupValidationURL", "LookupValidationURL");

            SetupMock();
        }

        private void SetupMock()
        {
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

        /// <summary>
        /// Sets up a mock <see cref="HttpRequestData"/> with a specified JSON body.
        /// </summary>
        /// <param name="json">The JSON string to be used as the request body.</param>
        /// <returns>A mock <see cref="HttpRequestData"/> object with the JSON body set.</returns>
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

        /// <summary>
        /// Creates and sets up a mock implementation of the <see cref="ICreateResponse"/> interface.
        /// </summary>
        /// <returns>A mock <see cref="ICreateResponse"/> object configured to create HTTP responses with JSON content.</returns>
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

        /// <summary>
        /// Sets up a mock <see cref="HttpRequestData"/> with specified query parameters.
        /// Can be used when mocking Run Methods to define multiple query parameters.
        /// </summary>
        /// <param name="queryParams">A dictionary of query parameters to be added to the request.</param>
        public void SetupRequestWithQueryParams(Dictionary<string, string> queryParams)
        {
            var queryCollection = new NameValueCollection();
            foreach (var param in queryParams)
            {
                queryCollection.Add(param.Key, param.Value);
            }
            _request.Setup(s => s.Query).Returns(queryCollection);
        }

        /// <summary>
        /// Configures a mock data reader to return data from a list of objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of object in the data list.</typeparam>
        /// <param name="dataList">The list of data objects to be returned by the mock data reader.</param>
        /// <param name="columnToClassPropertyMapping">A dictionary mapping column names to class property names.
        /// 1st property is the column name in the table, 2nd is class name e.g. "EXCEPTION_ID", "ExceptionId" </param>
        public void SetupDataReader<T>(List<T> dataList, Dictionary<string, string> columnToClassPropertyMapping, int? specificId = null)
        {
            SetupReadSequence(specificId.HasValue ? 1 : dataList.Count);
            var classProperties = typeof(T).GetProperties().ToList();
            SetupColumnMappings(dataList, columnToClassPropertyMapping, classProperties, specificId);
        }


        /// <summary>
        /// Sets up the mock data reader to read the specified number of rows from the data list.
        /// </summary>
        /// <param name="count">The number of rows to be returned by the mock data reader
        /// used to loop the sequenceSetup rather than chaining .Returns.</param>
        private void SetupReadSequence(int count)
        {
            var sequenceSetup = _mockDataReader.SetupSequence(r => r.Read());
            for (int i = 0; i < count; i++)
            {
                sequenceSetup = sequenceSetup.Returns(true);
            }
            sequenceSetup.Returns(false);
        }

        /// <summary>
        /// Configures the mock data reader to map column names to the appropriate class properties.
        /// </summary>
        /// <typeparam name="T">The type of object in the data list.</typeparam>
        /// <param name="dataList">The list of data objects to be returned by the mock data reader.</param>
        /// <param name="columnToClassPropertyMapping">A dictionary mapping column names to class property names.</param>
        /// <param name="classProperties">A dictionary of class property information for mapping.</param>
        ///
        private void SetupColumnMappings<T>(List<T> dataList, Dictionary<string, string> columnToClassPropertyMapping, List<PropertyInfo> classProperties, int? specificId = null)
        {
            int currentIndex = 0;

            foreach (var item in columnToClassPropertyMapping)
            {
                string columnName = item.Key;
                string classPropertyName = item.Value;
                object value;

                PropertyInfo property = classProperties.SingleOrDefault(s => s.Name.Equals(classPropertyName, StringComparison.OrdinalIgnoreCase));

                _mockDataReader.Setup(r => r[columnName]).Returns(() =>
                {
                    if (specificId.HasValue)
                    {
                        var specificItem = dataList.SingleOrDefault(item => property.GetValue(item) is int id && id == specificId.Value);
                        if (specificItem != null)
                        {
                            value = property.GetValue(specificItem);
                            return value;
                        }
                    }

                    var currentItem = dataList[currentIndex];
                    value = property.GetValue(currentItem);

                    // Increment the index only once per item (after processing all columns)
                    if (columnName == columnToClassPropertyMapping.Keys.Last())
                    {
                        currentIndex++;
                    }

                    return value;
                });
            }
        }
    }
}
