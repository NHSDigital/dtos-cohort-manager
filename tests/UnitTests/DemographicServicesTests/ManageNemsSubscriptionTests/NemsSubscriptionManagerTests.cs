namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.CohortManager.DemographicServices;
using Model;
using Common;
using DataServices.Core;
using System.Security.Cryptography.X509Certificates;
using System.Linq.Expressions;
using System.Net;

[TestClass]
public class NemsSubscriptionManagerTests
{
    private readonly Mock<IHttpClientFunction> _httpClientFunction = new();
    private readonly Mock<ILogger<NemsSubscriptionManager>> _logger = new();
    private readonly Mock<IDataServiceAccessor<NemsSubscription>> _nemsSubscriptionAccessor = new();
    private readonly Mock<IOptions<ManageNemsSubscriptionConfig>> _config = new();
    private readonly X509Certificate2 _dummyCert = new();
    private readonly NemsSubscriptionManager _sut;

    public NemsSubscriptionManagerTests()
    {
        var config = new ManageNemsSubscriptionConfig
        {
            NemsFhirEndpoint = "https://test.nems.endpoint/STU3",
            FromAsid = "TestFromAsid",
            ToAsid = "TestToAsid",
            OdsCode = "TestOds",
            MeshMailboxId = "TestMesh123",
            BypassServerCertificateValidation = false
        };

        _config.Setup(x => x.Value).Returns(config);

        _sut = new NemsSubscriptionManager(
            _httpClientFunction.Object,
            _config.Object,
            _logger.Object,
            _nemsSubscriptionAccessor.Object,
            _dummyCert
        );
    }

    [TestMethod]
    public async Task LookupSubscriptionIdAsync_ValidNhsNumber_ReturnsSubscriptionId()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var expectedSubscription = new NemsSubscription
        {
            NhsNumber = long.Parse(nhsNumber),
            SubscriptionId = "test-subscription-id"
        };

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(expectedSubscription);

        // Act
        var result = await _sut.LookupSubscriptionIdAsync(nhsNumber);

        // Assert
        Assert.AreEqual(expectedSubscription.SubscriptionId, result);
    }

    [TestMethod]
    public async Task LookupSubscriptionIdAsync_NoSubscriptionFound_ReturnsNull()
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        // Act
        var result = await _sut.LookupSubscriptionIdAsync(nhsNumber);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task LookupSubscriptionIdAsync_ExceptionThrown_ReturnsNull()
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.LookupSubscriptionIdAsync(nhsNumber);

        // Assert
        Assert.IsNull(result);
        
        // Verify error was logged
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to lookup subscription ID")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task DeleteSubscriptionFromDatabaseAsync_ValidNhsNumber_ReturnsTrue()
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteSubscriptionFromDatabaseAsync(nhsNumber);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task DeleteSubscriptionFromDatabaseAsync_DatabaseError_ReturnsFalse()
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.DeleteSubscriptionFromDatabaseAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result);
        
        // Verify error was logged
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception occurred while deleting the subscription")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task SaveSubscriptionInDatabase_ValidData_ReturnsTrue()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "test-subscription-id";

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SaveSubscriptionInDatabase(nhsNumber, subscriptionId);

        // Assert
        Assert.IsTrue(result);
        
        // Verify the subscription was created with correct data
        _nemsSubscriptionAccessor.Verify(x => x.InsertSingle(
            It.Is<NemsSubscription>(s => 
                s.NhsNumber == long.Parse(nhsNumber) &&
                s.SubscriptionId == subscriptionId &&
                s.RecordInsertDateTime.HasValue)),
            Times.Once);
    }

    [TestMethod]
    public async Task SaveSubscriptionInDatabase_DatabaseInsertFails_ReturnsFalse()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "test-subscription-id";

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SaveSubscriptionInDatabase(nhsNumber, subscriptionId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task SaveSubscriptionInDatabase_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "test-subscription-id";

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.SaveSubscriptionInDatabase(nhsNumber, subscriptionId);

        // Assert
        Assert.IsFalse(result);
        
        // Verify error was logged
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Exception occurred while saving the subscription in the database")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task DeleteSubscriptionFromNemsAsync_SuccessfulDelete_ReturnsTrue()
    {
        // Arrange
        var subscriptionId = "test-subscription-id";
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsDelete(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(successResponse);

        // Act
        var result = await _sut.DeleteSubscriptionFromNemsAsync(subscriptionId);

        // Assert
        Assert.IsTrue(result);
        
        // Verify correct URL was called
        _httpClientFunction.Verify(x => x.SendNemsDelete(
            It.Is<string>(url => url.Contains(subscriptionId)),
            "test-jwt-token",
            "TestFromAsid",
            "TestToAsid",
            _dummyCert,
            false),
            Times.Once);
    }

    [TestMethod]
    public async Task DeleteSubscriptionFromNemsAsync_FailedDelete_ReturnsFalse()
    {
        // Arrange
        var subscriptionId = "test-subscription-id";
        var failResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsDelete(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(failResponse);

        // Act
        var result = await _sut.DeleteSubscriptionFromNemsAsync(subscriptionId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task DeleteSubscriptionFromNemsAsync_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        var subscriptionId = "test-subscription-id";

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsDelete(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _sut.DeleteSubscriptionFromNemsAsync(subscriptionId);

        // Assert
        Assert.IsFalse(result);
        
        // Verify error was logged
        _logger.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to delete subscription ID")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [TestMethod]
    public async Task SendSubscriptionToNemsAsync_SuccessfulPost_ReturnsSubscriptionId()
    {
        // Arrange
        var subscriptionJson = "{\"resourceType\":\"Subscription\"}";
        var subscriptionId = "test-subscription-123";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri($"https://nems.endpoint/Subscription/{subscriptionId}") }
        };

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsPost(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(successResponse);

        // Act
        var result = await _sut.SendSubscriptionToNemsAsync(subscriptionJson);

        // Assert
        Assert.AreEqual(subscriptionId, result);
    }

    [TestMethod]
    public async Task SendSubscriptionToNemsAsync_NoLocationHeader_ReturnsNull()
    {
        // Arrange
        var subscriptionJson = "{\"resourceType\":\"Subscription\"}";
        var successResponse = new HttpResponseMessage(HttpStatusCode.Created);

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsPost(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(successResponse);

        // Act
        var result = await _sut.SendSubscriptionToNemsAsync(subscriptionJson);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SendSubscriptionToNemsAsync_DuplicateError_ReturnsExistingId()
    {
        // Arrange
        var subscriptionJson = "{\"resourceType\":\"Subscription\"}";
        var existingId = "abcd1234";
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent($"DUPLICATE_REJECTED subscription already exists : {existingId}")
        };

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsPost(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await _sut.SendSubscriptionToNemsAsync(subscriptionJson);

        // Assert
        Assert.AreEqual(existingId, result);
    }

    [TestMethod]
    public async Task SendSubscriptionToNemsAsync_ExceptionThrown_ReturnsNull()
    {
        // Arrange
        var subscriptionJson = "{\"resourceType\":\"Subscription\"}";

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsPost(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _sut.SendSubscriptionToNemsAsync(subscriptionJson);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void CreateSubscription_ValidInputs_ReturnsSubscription()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var eventType = "pds-record-change-1";

        // Act
        var result = _sut.CreateSubscription(nhsNumber, eventType);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("pds-record-change-1", result.Reason.Split(' ')[2]);
        Assert.IsTrue(result.Criteria.Contains(nhsNumber));
        Assert.IsNotNull(result.Channel);
    }

    [TestMethod]
    public void SerialiseSubscription_ValidSubscription_ReturnsJsonString()
    {
        // Arrange
        var subscription = _sut.CreateSubscription("1234567890");

        // Act
        var result = NemsSubscriptionManager.SerialiseSubscription(subscription);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\"resourceType\":\"Subscription\""));
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_SubscriptionAlreadyExists_ReturnsTrue()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var existingSubscription = new NemsSubscription
        {
            NhsNumber = long.Parse(nhsNumber),
            SubscriptionId = "existing-subscription-id"
        };

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(existingSubscription);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_SuccessfulCreation_ReturnsTrue()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "new-subscription-id";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var successResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri($"https://nems.endpoint/Subscription/{subscriptionId}") }
        };

        _httpClientFunction
            .Setup(x => x.SendNemsPost(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(successResponse);

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_DatabaseSaveFails_ReturnsFalse()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "new-subscription-id";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var successResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri($"https://nems.endpoint/Subscription/{subscriptionId}") }
        };

        _httpClientFunction
            .Setup(x => x.SendNemsPost(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(successResponse);

        _httpClientFunction
            .Setup(x => x.SendNemsDelete(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task RemoveSubscriptionAsync_NoSubscriptionFound_ReturnsFalse()
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        // Act
        var result = await _sut.RemoveSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task RemoveSubscriptionAsync_SuccessfulRemoval_ReturnsTrue()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "subscription-to-remove";
        var existingSubscription = new NemsSubscription
        {
            NhsNumber = long.Parse(nhsNumber),
            SubscriptionId = subscriptionId
        };

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(existingSubscription);

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsDelete(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RemoveSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task RemoveSubscriptionAsync_PartialFailure_ReturnsFalse()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "subscription-to-remove";
        var existingSubscription = new NemsSubscription
        {
            NhsNumber = long.Parse(nhsNumber),
            SubscriptionId = subscriptionId
        };

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(existingSubscription);

        _httpClientFunction
            .Setup(x => x.GenerateNemsJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendNemsDelete(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<X509Certificate2>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RemoveSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result);
    }
}