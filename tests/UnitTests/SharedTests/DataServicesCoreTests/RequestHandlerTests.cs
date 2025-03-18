namespace NHS.CohortManager.Tests.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataServices.Core;
using DataServices.Database;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Net;
using System.Linq.Dynamic.Core;

[TestClass]
public class RequestHandlerTests
{
    private TestContext _context;
    private DataServiceAccessor<TestEntity> _accessor;
    private RequestHandler<TestEntity> _sut;
    private SetupRequest setupRequest = new();

    [TestInitialize]
    public void setup()
    {
        var databaseName = Guid.NewGuid().ToString();
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite(connectionString)  // GUID for the name so a new one is created for each test
            .Options;

        _context = new TestContext(options);

        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _accessor = new DataServiceAccessor<TestEntity>(_context, new NullLogger<DataServiceAccessor<TestEntity>>());

        _context.TestEntities.AddRange(
            new TestEntity { Id = 1, NHSNumber = 12345, RecordType = "ADD", ReasonForRemoval = "DEA", DateOfBirth = new DateTime(1980, 1, 1) },
            new TestEntity { Id = 2, NHSNumber = 67890, RecordType = "ADD", ReasonForRemoval = "DEA", DateOfBirth = new DateTime(1979, 1, 1) },
            new TestEntity { Id = 3, NHSNumber = 00000, RecordType = "AMENDED" }
        );
        _context.SaveChanges();

        AccessRule alwaysTrueRule = i => true;
        AuthenticationConfiguration authConfig = new(alwaysTrueRule, alwaysTrueRule, alwaysTrueRule, alwaysTrueRule, alwaysTrueRule);

        _sut = new RequestHandler<TestEntity>(_accessor, new NullLogger<RequestHandler<TestEntity>>(), authConfig);
    }

    #region Get Tests

    [TestMethod]
    public async Task HandleRequest_GetWithKey_ReturnCorrectEntity()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");

        // Act
        var response = await _sut.HandleRequest(request.Object, "1");
        var result = JsonSerializer.Deserialize<TestEntity>(response.Body);

        // Assert
        Assert.AreEqual(12345, result.NHSNumber);
    }

    [TestMethod]
    public async Task HandleRequest_GetWithKeyNotInDb_ReturnNotFound()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");

        // Act
        var response = await _sut.HandleRequest(request.Object, "4");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_GetWithoutKey_ReturnAllEntities()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");
        request.Setup(x => x.Query).Returns(new NameValueCollection());

        // Act
        var response = await _sut.HandleRequest(request.Object);
        var result = JsonSerializer.Deserialize<List<TestEntity>>(response.Body);

        // Assert
        Assert.AreEqual(_context.TestEntities.Count(), result.Count);
    }

    [TestMethod]
    public async Task HandleRequest_GetWithoutKeyNoData_ReturnNoContent()
    {
        // Arrange
        _context.TestEntities.RemoveRange(_context.TestEntities);
        _context.SaveChanges();

        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");
        request.Setup(x => x.Query).Returns(new NameValueCollection());

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    [DataRow("x => x.ReasonForRemoval == \"DEA\"", 2)]
    [DataRow("x => x.RecordType != \"ADD\"", 1)]
    [DataRow("x => x.Id < 2", 1)]
    public async Task HandleRequest_GetWithPredicate_ReturnAllMatchingEntities(string predicate, int expectedNumEntities)
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");

        request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection{{"single", "false"}, {"query",  predicate} });

        // Act
        var response = await _sut.HandleRequest(request.Object);
        var result = JsonSerializer.Deserialize<List<TestEntity>>(response.Body);

        // Assert
        Assert.AreEqual(expectedNumEntities, result.Count);
    }

    [TestMethod]
    public async Task HandleRequest_GetSingleWithPredicate_ReturnCorrectEntity()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");

        string predicate = "x => x.NHSNumber == 12345";
        request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection { { "single", "true" }, { "query", predicate } });

        // Act
        var response = await _sut.HandleRequest(request.Object);
        var result = JsonSerializer.Deserialize<TestEntity>(response.Body);

        // Assert
        Assert.AreEqual(1, result.Id);
    }

    [TestMethod]
    [DataRow("x => x.ExceptionFlag == 1")]
    [DataRow("blorg")]
    public async Task HandleRequest_GetSingleWithInvalidPredicate_ReturnBadRequest(string predicate)
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");
        request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection { { "single", "true" }, { "query", predicate } });

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [TestMethod]
    public async Task HandleRequest_GetSingleWithPredicateMatchingMultipleEntities_ReturnBadRequest()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");

        string predicate = "x => x.ReasonForRemoval == \"DEA\"";
        request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection { { "single", "true" }, { "query", predicate } });

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task HandleRequest_DeleteWithKey_DeleteAndReturnOk()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("DELETE");

        // Act
        var response = await _sut.HandleRequest(request.Object, "3");

        // Assert
        // var deletedEntity = await _context.TestEntities
        //                                 .FirstOrDefaultAsync(e => e.Id == 4);

        // Assert.IsNull(deletedEntity);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
    // method not allowed
    // no data in DB
    // multiple records returned on get


}