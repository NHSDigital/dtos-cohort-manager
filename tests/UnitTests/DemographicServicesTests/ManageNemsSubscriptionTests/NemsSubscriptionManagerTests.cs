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
using System.Runtime.CompilerServices;

[TestClass]
public class NemsSubscriptionManagerTests
{
    private readonly Mock<INemsHttpClientFunction> _httpClientFunction = new();
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
            NemsFromAsid = "TestFromAsid",
            NemsToAsid = "TestToAsid",
            NemsOdsCode = "TestOds",
            NemsMeshMailboxId = "TestMesh123",
            NemsBypassServerCertificateValidation = false
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>(), It.IsAny<int>()))
            .ReturnsAsync(successResponse);

        // Act
        var result = await _sut.DeleteSubscriptionFromNemsAsync(subscriptionId);

        // Assert
        Assert.IsTrue(result);

        // Verify correct URL was called
        _httpClientFunction.Verify(x => x.SendSubscriptionDelete(
            It.Is<NemsSubscriptionRequest>(req => req.Url.Contains(subscriptionId) &&
                req.JwtToken == "test-jwt-token" &&
                req.FromAsid == "TestFromAsid" &&
                req.ToAsid == "TestToAsid" &&
                req.ClientCertificate == _dummyCert &&
                req.BypassCertValidation == false), It.IsAny<int>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DeleteSubscriptionFromNemsAsync_FailedDelete_ReturnsFalse()
    {
        // Arrange
        var subscriptionId = "test-subscription-id";
        var failResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>(), It.IsAny<int>()))
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>(), It.IsAny<int>()))
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
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
    public async Task CreateAndSendSubscriptionAsync_SubscriptionAlreadyExists_ReturnsSuccess()
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
        Assert.IsTrue(result.Success);
        Assert.AreEqual("existing-subscription-id", result.SubscriptionId);
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_SuccessfulCreation_ReturnsSuccess()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "new-subscription-id";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var successResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri($"https://nems.endpoint/Subscription/{subscriptionId}") }
        };

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
            .ReturnsAsync(successResponse);

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(subscriptionId, result.SubscriptionId);
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_DatabaseSaveFails_ReturnsFailure()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var subscriptionId = "new-subscription-id";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var successResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri($"https://nems.endpoint/Subscription/{subscriptionId}") }
        };

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
            .ReturnsAsync(successResponse);

        _httpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>(), It.IsAny<int>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Failed to save subscription to database", result.ErrorMessage);
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>(), It.IsAny<int>()))
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
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        _httpClientFunction
            .Setup(x => x.SendSubscriptionDelete(It.IsAny<NemsSubscriptionRequest>(), It.IsAny<int>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        _nemsSubscriptionAccessor
            .Setup(x => x.Remove(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.RemoveSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_NemsApiFailure_ReturnsFailure()
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var failureResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
            .ReturnsAsync(failureResponse);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Failed to create subscription in NEMS", result.ErrorMessage);
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_ExceptionThrown_ReturnsFailure()
    {
        // Arrange
        var nhsNumber = "1234567890";

        // Setup the lookup to return null (no existing subscription)
        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        // Make the HTTP client throw an exception to trigger the outer catch block
        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("JWT generation failed"));

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Failed to create subscription in NEMS", result.ErrorMessage);
    }

    [TestMethod]
    public async Task CreateAndSendSubscriptionAsync_DuplicateSubscription_ReturnsExistingId()
    {
        // Arrange
        var nhsNumber = "1234567890";
        var existingId = "abc123def456";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var duplicateResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent($"DUPLICATE_REJECTED subscription already exists: {existingId}")
        };

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
            .ReturnsAsync(duplicateResponse);

        // Mock the database save to succeed for the duplicate subscription
        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(existingId, result.SubscriptionId);
    }

    [DataTestMethod]
    [DataRow("DUPLICATE_REJECTED subscription already exists: abc123def456", "abc123def456")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: 550e8400-e29b-41d4-a716-446655440000", "550e8400-e29b-41d4-a716-446655440000")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: my-subscription-id-2024", "my-subscription-id-2024")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: abc123 but something else", "abc123")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: XYZ789 for patient NHS123456", "XYZ789")]
    [DataRow("DUPLICATE_REJECTED subscription already exists : spaced-id-123", "spaced-id-123")]
    [DataRow("DUPLICATE_REJECTED subscription already exists:no-space-id", "no-space-id")]
    [DataRow("Error: DUPLICATE_REJECTED subscription already exists: prefix-test-456 with additional context", "prefix-test-456")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: \"8965851501ce40bdb85841f84b726d93\"", "8965851501ce40bdb85841f84b726d93")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: \"8965851501ce40bdb85841f84b726d93\"/>", "8965851501ce40bdb85841f84b726d93")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: 8965851501ce40bdb85841f84b726d93\"/>", "8965851501ce40bdb85841f84b726d93")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: \"abc123def456\"/> additional content", "abc123def456")]
    [DataRow("DUPLICATE_REJECTED subscription already exists: test-id-123\"/> <other>xml</other>", "test-id-123")]
    public async Task CreateAndSendSubscriptionAsync_VariousDuplicateFormats_ExtractsCorrectId(string errorMessage, string expectedId)
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var duplicateResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorMessage)
        };

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
            .ReturnsAsync(duplicateResponse);

        _nemsSubscriptionAccessor
            .Setup(x => x.InsertSingle(It.IsAny<NemsSubscription>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsTrue(result.Success, $"Expected success for message: {errorMessage}");
        Assert.AreEqual(expectedId, result.SubscriptionId, $"Expected ID '{expectedId}' but got '{result.SubscriptionId}' for message: {errorMessage}");
    }

    [DataTestMethod]
    [DataRow("DUPLICATE_REJECTED subscription already exists:")] // No ID after colon
    [DataRow("DUPLICATE_REJECTED subscription already exists")] // No colon
    [DataRow("DUPLICATE_REJECTED something else happened")] // Different error format
    [DataRow("DUPLICATE_REJECTED subscription already exists: ")] // Empty after colon
    [DataRow("DUPLICATE_REJECTED subscription already exists: <invalid>special@chars</invalid>")] // Invalid characters in ID
    [DataRow("DUPLICATE_REJECTED subscription already exists: @invalid@id")] // Invalid characters in ID
    [DataRow("DUPLICATE_REJECTED subscription already exists: \"\"/")] // Empty quoted ID with XML
    public async Task CreateAndSendSubscriptionAsync_MalformedDuplicateMessages_ReturnsFailure(string errorMessage)
    {
        // Arrange
        var nhsNumber = "1234567890";

        _nemsSubscriptionAccessor
            .Setup(x => x.GetSingle(It.IsAny<Expression<Func<NemsSubscription, bool>>>()))
            .ReturnsAsync((NemsSubscription)null);

        _httpClientFunction
            .Setup(x => x.GenerateJwtToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test-jwt-token");

        var duplicateResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorMessage)
        };

        _httpClientFunction
            .Setup(x => x.SendSubscriptionPost(It.IsAny<NemsSubscriptionPostRequest>(), It.IsAny<int>()))
            .ReturnsAsync(duplicateResponse);

        // Act
        var result = await _sut.CreateAndSendSubscriptionAsync(nhsNumber);

        // Assert
        Assert.IsFalse(result.Success, $"Expected failure for malformed message: {errorMessage}");
        Assert.AreEqual("Failed to create subscription in NEMS", result.ErrorMessage);
    }
}
