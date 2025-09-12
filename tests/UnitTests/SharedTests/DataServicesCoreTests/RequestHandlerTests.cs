namespace NHS.CohortManager.Tests.Shared;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataServices.Core;
using NHS.CohortManager.Tests.TestUtils;
using System.Text.Json;
using System.Collections.Specialized;
using System.Net;
using System.Linq.Dynamic.Core;
using DataServices.Database;
using Model;

// If these tests are failing after a DB change, make sure the create_test_table.sql file is up to date.
[TestClass]
public class RequestHandlerTests
{
    private DataServicesContext _context;
    private DataServiceAccessor<ParticipantManagement> _accessor;
    private RequestHandler<ParticipantManagement> _sut;
    private SetupRequest setupRequest = new();

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DataServicesContext>()
            .UseSqlite("DataSource=:memory:")
            .EnableSensitiveDataLogging()
            .Options;

        _context = new DataServicesContext(options);

        _context.Database.OpenConnection();
        string sql = File.ReadAllText("create_test_table.sql");

        _context.Database.ExecuteSqlRaw(sql);

        _accessor = new DataServiceAccessor<ParticipantManagement>(_context, new NullLogger<DataServiceAccessor<ParticipantManagement>>());

        using var transaction = _context.Database.BeginTransaction();
        _context.ParticipantManagements.AddRange(
            new ParticipantManagement { ParticipantId = 1, NHSNumber = 12345, RecordType = "ADD", ReasonForRemoval = "DEA", RecordInsertDateTime = new DateTime(1980, 1, 1) },
            new ParticipantManagement { ParticipantId = 2, NHSNumber = 67890, RecordType = "ADD", ReasonForRemoval = "DEA", RecordInsertDateTime = new DateTime(1979, 1, 1) },
            new ParticipantManagement { ParticipantId = 3, NHSNumber = 00000, RecordType = "AMENDED" }
        );
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        transaction.Commit();

        AccessRule alwaysTrueRule = i => true;
        AuthenticationConfiguration authConfig = new(alwaysTrueRule, alwaysTrueRule, alwaysTrueRule, alwaysTrueRule, alwaysTrueRule);

        _sut = new RequestHandler<ParticipantManagement>(_accessor, new NullLogger<RequestHandler<ParticipantManagement>>(), authConfig);
    }

    #region Get Tests

    [TestMethod]
    public async Task HandleRequest_GetWithKey_ReturnCorrectEntity()
    {
        // Arrange
        var request = setupRequest.Setup("");
        request.Setup(x => x.Method).Returns("GET");

        // Act
        var response = await _sut.HandleRequest(request.Object, "1");
        var result = JsonSerializer.Deserialize<ParticipantManagement>(response.Body);

        // Assert
        Assert.AreEqual(12345, result.NHSNumber);
    }

    [TestMethod]
    public async Task HandleRequest_GetWithKeyNotInDb_ReturnNotFound()
    {
        // Arrange
        var request = setupRequest.Setup("");
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
        var request = setupRequest.Setup("");
        request.Setup(x => x.Method).Returns("GET");
        request.Setup(x => x.Query).Returns(new NameValueCollection());

        // Act
        var response = await _sut.HandleRequest(request.Object);
        var result = JsonSerializer.Deserialize<List<ParticipantManagement>>(response.Body);

        // Assert
        Assert.AreEqual(_context.ParticipantManagements.Count(), result.Count);
    }

    [TestMethod]
    public async Task HandleRequest_GetWithoutKeyNoData_ReturnNoContent()
    {
        // Arrange
        _context.ParticipantManagements.RemoveRange(_context.ParticipantManagements);
        _context.SaveChanges();

        var request = setupRequest.Setup("");
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
    [DataRow("x => x.ParticipantId < 2", 1)]
    public async Task HandleRequest_GetWithPredicate_ReturnAllMatchingEntities(string predicate, int expectedNumEntities)
    {
        // Arrange
        var request = setupRequest.Setup("");
        request.Setup(x => x.Method).Returns("GET");

        request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection{{"single", "false"}, {"query",  predicate} });

        // Act
        var response = await _sut.HandleRequest(request.Object);
        var result = JsonSerializer.Deserialize<List<ParticipantManagement>>(response.Body);

        // Assert
        Assert.AreEqual(expectedNumEntities, result.Count);
    }

    [TestMethod]
    public async Task HandleRequest_GetSingleWithPredicate_ReturnCorrectEntity()
    {
        // Arrange
        var request = setupRequest.Setup("");
        request.Setup(x => x.Method).Returns("GET");

        string predicate = "x => x.NHSNumber == 12345";
        request
            .Setup(x => x.Query)
            .Returns(new NameValueCollection { { "single", "true" }, { "query", predicate } });

        // Act
        var response = await _sut.HandleRequest(request.Object);
        var result = JsonSerializer.Deserialize<ParticipantManagement>(response.Body);

        // Assert
        Assert.AreEqual(1, result.ParticipantId);
    }

    [TestMethod]
    [DataRow("x => x.DateOfBirth == 1")]
    [DataRow("blorg")]
    public async Task HandleRequest_GetSingleWithInvalidPredicate_ReturnBadRequest(string predicate)
    {
        // Arrange
        var request = setupRequest.Setup("");
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
        var request = setupRequest.Setup("");
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
        _context.ParticipantManagements.RemoveRange(_context.ParticipantManagements);
        var entity = new ParticipantManagement { ParticipantId = 4, NHSNumber = 00001, RecordType = "AMENDED" };
        _context.ParticipantManagements.Add(entity);
        _context.SaveChanges();

        _context.Entry(entity).State = EntityState.Detached;

        var request = setupRequest.Setup("");
        request.Setup(x => x.Method).Returns("DELETE");

        // Act
        var response = await _sut.HandleRequest(request.Object, "4");

        // Assert
        var deletedEntity = await _context.ParticipantManagements
                                        .FirstOrDefaultAsync(e => e.ParticipantId == 4);

        Assert.IsNull(deletedEntity);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_DeleteWithKeyNotInDb_ReturnNotFound()
    {
        // Arrange
        var request = setupRequest.Setup("");
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
        var request = setupRequest.Setup("");
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
        var entity = new ParticipantManagement { ParticipantId = 4, NHSNumber = 00000, RecordType = "ADD"};

        var request = setupRequest.Setup(JsonSerializer.Serialize(entity));
        request.Setup(x => x.Method).Returns("POST");

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        var insertedEntity = await _context.ParticipantManagements
                                .FirstOrDefaultAsync(e => e.ParticipantId == 4);

        Assert.AreEqual(entity.ParticipantId, insertedEntity.ParticipantId);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_PostMany_InsertAndReturnOk()
    {
        // Arrange
        var entityList = new List<ParticipantManagement> {
            new() { ParticipantId = 4, NHSNumber = 00000, RecordType = "ADD"},
            new() { ParticipantId = 5, NHSNumber = 11111, RecordType = "ADD"}
        };

        var request = setupRequest.Setup(JsonSerializer.Serialize(entityList));
        request.Setup(x => x.Method).Returns("POST");

        // Act
        var response = await _sut.HandleRequest(request.Object);

        // Assert
        var insertedEntities = _context.ParticipantManagements.Where(e => e.ParticipantId >= 4);

        Assert.AreEqual(entityList.Count, insertedEntities.Count());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Put Tests

    [TestMethod]
    public async Task HandleRequest_Put_UpdateAndReturnOk()
    {
        // Arrange
        var entity = new ParticipantManagement { ParticipantId = 3, NHSNumber = 11111, RecordType = "AMENDED" };

        var request = setupRequest.Setup(JsonSerializer.Serialize(entity));
        request.Setup(x => x.Method).Returns("PUT");

        // Act
        var response = await _sut.HandleRequest(request.Object, "3");

        // Assert
        var updatedEntity = await _context.ParticipantManagements
                                .FirstOrDefaultAsync(e => e.ParticipantId == 3);

        Assert.AreEqual(entity.NHSNumber, updatedEntity.NHSNumber);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HandleRequest_PutKeyNotInDB_ReturnNotFound()
    {
        // Arrange
        var entity = new ParticipantManagement { ParticipantId = 4, NHSNumber = 11111, RecordType = "AMENDED" };

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
        var entity = new ParticipantManagement { ParticipantId = 4, NHSNumber = 11111, RecordType = "AMENDED" };

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

        _sut = new RequestHandler<ParticipantManagement>(_accessor, new NullLogger<RequestHandler<ParticipantManagement>>(), authConfig);

        var request = setupRequest.Setup("");
        request.Setup(x => x.Method).Returns("GET");

        // Act
        var response = await _sut.HandleRequest(request.Object, "1");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}