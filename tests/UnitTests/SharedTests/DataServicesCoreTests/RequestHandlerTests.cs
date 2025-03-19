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
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseSqlite("DataSource=:memory:")
            .EnableSensitiveDataLogging()
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
        _context.ChangeTracker.Clear();

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
        _context.TestEntities.RemoveRange(_context.TestEntities);
        var entity = new TestEntity { Id = 4, NHSNumber = 00001, RecordType = "AMENDED" };
        _context.TestEntities.Add(entity);
        _context.SaveChanges();

        _context.Entry(entity).State = EntityState.Detached;

        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("DELETE");

        // Act
        var response = await _sut.HandleRequest(request.Object, "4");

        // Assert
        var deletedEntity = await _context.TestEntities
                                        .FirstOrDefaultAsync(e => e.Id == 4);

        Assert.IsNull(deletedEntity);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_DeleteWithKeyNotInDb_ReturnNotFound()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("DELETE");

        // Act
        var response = await _sut.HandleRequest(request.Object, "4");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_DeleteWithoutKey_ReturnBadRequest()
    {
        // Arrange
        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("DELETE");

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Post Tests

    [TestMethod]
    public async Task HandleRequest_PostSingle_InsertAndReturnOk()
    {
        // Arrange
        var entity = new TestEntity { Id = 4, NHSNumber = 00000, RecordType = "ADD"};

        var request = setupRequest.Setup(JsonSerializer.Serialize(entity));
        request.Setup(x => x.Method).Returns("POST");

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        var insertedEntity = await _context.TestEntities
                                .FirstOrDefaultAsync(e => e.Id == 4);

        Assert.AreEqual(entity.Id, insertedEntity.Id);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_PostSingleInvalidEntity_ReturnInternalServerError()
    {
        // Arrange
        var entity = new TestEntity { Id = 4, NHSNumber = 00000}; // Missing required field RecordType

        var request = setupRequest.Setup(JsonSerializer.Serialize(entity));
        request.Setup(x => x.Method).Returns("POST");

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_PostMany_InsertAndReturnOk()
    {
        // Arrange
        var entityList = new List<TestEntity> {
            new() { Id = 4, NHSNumber = 00000, RecordType = "ADD"},
            new() { Id = 5, NHSNumber = 11111, RecordType = "ADD"}
        };

        var request = setupRequest.Setup(JsonSerializer.Serialize(entityList));
        request.Setup(x => x.Method).Returns("POST");

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        var insertedEntities = _context.TestEntities.Where(e => e.Id >= 4);

        Assert.AreEqual(entityList.Count, insertedEntities.Count());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Put Tests

    [TestMethod]
    public async Task HandleRequest_Put_UpdateAndReturnOk()
    {
        // Arrange
        var entity = new TestEntity { Id = 3, NHSNumber = 11111, RecordType = "AMENDED" };

        var request = setupRequest.Setup(JsonSerializer.Serialize(entity));
        request.Setup(x => x.Method).Returns("PUT");

        // Act
        var response = await _sut.HandleRequest(request.Object, "3");

        // Assert
        var updatedEntity = await _context.TestEntities
                                .FirstOrDefaultAsync(e => e.Id == 3);

        Assert.AreEqual(entity.NHSNumber, updatedEntity.NHSNumber);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_PutKeyNotInDB_ReturnNotFound()
    {
        // Arrange
        var entity = new TestEntity { Id = 4, NHSNumber = 11111, RecordType = "AMENDED" };

        var request = setupRequest.Setup(JsonSerializer.Serialize(entity));
        request.Setup(x => x.Method).Returns("PUT");

        // Act
        var response = await _sut.HandleRequest(request.Object, "4");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_PutWithoutKey_ReturnBadRequest()
    {
        // Arrange
        var entity = new TestEntity { Id = 4, NHSNumber = 11111, RecordType = "AMENDED" };

        var request = setupRequest.Setup(JsonSerializer.Serialize(entity));
        request.Setup(x => x.Method).Returns("PUT");

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    [TestMethod]
    public async Task HandleRequest_MethodBlocked_ReturnUnauthorized()
    {
        // Arrange
        AccessRule alwaysTrueRule = i => true;
        AccessRule alwaysFalseRule = i => false;
        AuthenticationConfiguration authConfig = new(alwaysTrueRule, alwaysFalseRule, alwaysTrueRule, alwaysTrueRule, alwaysTrueRule);

        _sut = new RequestHandler<TestEntity>(_accessor, new NullLogger<RequestHandler<TestEntity>>(), authConfig);

        var request = setupRequest.Setup();
        request.Setup(x => x.Method).Returns("GET");

        // Act
        var response = await _sut.HandleRequest(request.Object, "1");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}