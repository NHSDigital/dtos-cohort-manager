namespace NHS.CohortManager.Tests.UnitTests.ServiceNowCohortLookupTests;

 using Microsoft.Extensions.Logging;
 using Microsoft.Extensions.Options;
 using Model;
 using Moq;
 using DataServices.Client;
 using NHS.CohortManager.ServiceNowIntegrationService;
 using System.Linq.Expressions;
 using Microsoft.Azure.Functions.Worker;

 [TestClass]
 public class ServiceNowCohortLookupTests
 {
     private Mock<ILogger<ServiceNowCohortLookup>> _loggerMock;
     private Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionClientMock;
     private Mock<IDataServiceClient<ServicenowCases>> _serviceNowCasesClientMock;
     private ServiceNowCohortLookup _service;
     private TimerInfo _timerInfo;
     private const string ValidNhsNumber = "3112728165";
     private const string ServiceNowId = "SN12345";

     [TestInitialize]
     public void Initialize()
     {
         _loggerMock = new Mock<ILogger<ServiceNowCohortLookup>>();
         _cohortDistributionClientMock = new Mock<IDataServiceClient<CohortDistribution>>();
         _serviceNowCasesClientMock = new Mock<IDataServiceClient<ServicenowCases>>();

         _service = new ServiceNowCohortLookup(
             _loggerMock.Object,
             _cohortDistributionClientMock.Object,
             _serviceNowCasesClientMock.Object);

         _timerInfo = new TimerInfo
         {
             ScheduleStatus = new ScheduleStatus
             {
                 Last = DateTime.UtcNow.AddDays(-1),
                 Next = DateTime.UtcNow.AddDays(1),
                 LastUpdated = DateTime.UtcNow
             },
             IsPastDue = false
         };
     }

     [TestMethod]
     public async Task Run_WithInvalidNhsNumber_LogsWarningAndSkips()
     {
         // Arrange
         var invalidCase = new ServicenowCases
         {
             ServicenowId = ServiceNowId,
             NhsNumber = 0, // Invalid NHS number
             Status = ServiceNowStatus.New,
             RecordUpdateDatetime = DateTime.UtcNow
         };

         _serviceNowCasesClientMock.Setup(x =>
                 x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
             .ReturnsAsync(new List<ServicenowCases> { invalidCase });

         // Act
         await _service.Run(_timerInfo);

         // Assert
         _serviceNowCasesClientMock.Verify(x =>
             x.Update(It.IsAny<ServicenowCases>()),
             Times.Never);

         _loggerMock.Verify(x => x.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processed 0/1 cases")),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
             Times.Once);
     }

     [TestMethod]
     public async Task Run_WithNoYesterdayParticipants_LogsInformation()
     {
         // Arrange
         var validNhsNumberLong = long.Parse(ValidNhsNumber);
         var newCase = new ServicenowCases
         {
             ServicenowId = ServiceNowId,
             NhsNumber = validNhsNumberLong,
             Status = ServiceNowStatus.New,
             RecordUpdateDatetime = DateTime.UtcNow
         };

         _serviceNowCasesClientMock.Setup(x =>
                 x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
             .ReturnsAsync(new List<ServicenowCases> { newCase });

         _cohortDistributionClientMock.Setup(x =>
                 x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
             .ReturnsAsync(new List<CohortDistribution>());

         // Act
         await _service.Run(_timerInfo);

         // Assert
         _loggerMock.Verify(x => x.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("0 participants")),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
             Times.Once);
     }

     [TestMethod]
     public async Task Run_WithCaseAlreadyProcessed_DoesNotProcessAgain()
     {
         // Arrange
         var validNhsNumberLong = long.Parse(ValidNhsNumber);
         var processedCase = new ServicenowCases
         {
             ServicenowId = ServiceNowId,
             NhsNumber = validNhsNumberLong,
             Status = ServiceNowStatus.Complete, // Already processed
             RecordUpdateDatetime = DateTime.UtcNow
         };

         _serviceNowCasesClientMock.Setup(x =>
                 x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
             .ReturnsAsync(new List<ServicenowCases> { processedCase });

         // Act
         await _service.Run(_timerInfo);

         // Assert
         _serviceNowCasesClientMock.Verify(x =>
             x.Update(It.IsAny<ServicenowCases>()),
             Times.Never);
     }

     [TestMethod]
     public async Task Run_WithMixedCases_ProcessesCorrectly()
     {
         // Arrange
         var validCase1 = new ServicenowCases
         {
             ServicenowId = "SN1",
             NhsNumber = 123,
             Status = ServiceNowStatus.New
         };

         var validCase2 = new ServicenowCases
         {
             ServicenowId = "SN2",
             NhsNumber = 456,
             Status = ServiceNowStatus.New
         };

         var invalidCase = new ServicenowCases
         {
             ServicenowId = "SN3",
             NhsNumber = 0, // Invalid
             Status = ServiceNowStatus.New
         };

         // Mock case retrieval
         _serviceNowCasesClientMock.Setup(x =>
                 x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
             .ReturnsAsync(new List<ServicenowCases> { validCase1, validCase2, invalidCase });

         // Mock participant lookup - only match validCase1
         var participants = new List<CohortDistribution>
         {
             new CohortDistribution { NHSNumber = 123 } // Only matches validCase1
         };

         _cohortDistributionClientMock.Setup(x =>
                 x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
             .ReturnsAsync(participants);

         var updatedCases = new List<ServicenowCases>();
         _serviceNowCasesClientMock.Setup(x =>
                 x.Update(It.IsAny<ServicenowCases>()))
             .ReturnsAsync(true)
             .Callback<ServicenowCases>(c => updatedCases.Add(c));

         // Act
         await _service.Run(_timerInfo);

         // Assert
         // Verify exactly one case was updated (validCase1)
         Assert.AreEqual(1, updatedCases.Count, "Expected exactly one case to be updated");

         // Verify validCase1 was updated correctly
         var updatedValidCase = updatedCases.FirstOrDefault(c => c.ServicenowId == "SN1");
         Assert.IsNotNull(updatedValidCase, "Valid case 1 should have been updated");
         Assert.AreEqual(ServiceNowStatus.Complete, updatedValidCase.Status, "Case should be marked Complete");

         // Verify other cases weren't updated
         Assert.IsFalse(updatedCases.Any(c => c.ServicenowId == "SN2"), "Valid case without match shouldn't update");
         Assert.IsFalse(updatedCases.Any(c => c.ServicenowId == "SN3"), "Invalid case shouldn't update");

         // Verify logging
         _loggerMock.Verify(x => x.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) =>
                 v.ToString().Contains("No participant found for NHS number in ServiceNowId SN2", StringComparison.OrdinalIgnoreCase) ||
                 v.ToString().Contains("No participant found for NHS number in ServiceNowId SN3", StringComparison.OrdinalIgnoreCase) ||
                 v.ToString().Contains("Processed 1/3 cases successfully", StringComparison.OrdinalIgnoreCase)),
             It.IsAny<Exception>(),
             It.IsAny<Func<It.IsAnyType, Exception, string>>()),
             Times.AtLeastOnce);
     }

     [TestMethod]
     public async Task Run_WithDuplicateNhsNumbers_ProcessesAll()
     {
         // Arrange
         const long testNhsNumber = 123;

         var case1 = new ServicenowCases
         {
             ServicenowId = "SN1",
             NhsNumber = testNhsNumber,
             Status = ServiceNowStatus.New
         };

         var case2 = new ServicenowCases
         {
             ServicenowId = "SN2",
             NhsNumber = testNhsNumber, // Same NHS number
             Status = ServiceNowStatus.New
         };

         _serviceNowCasesClientMock.Setup(x =>
             x.GetByFilter(It.IsAny<Expression<Func<ServicenowCases, bool>>>()))
             .ReturnsAsync(new List<ServicenowCases> { case1, case2 });

         var participant = new CohortDistribution { NHSNumber = testNhsNumber };
         var participants = new List<CohortDistribution> { participant };

         _cohortDistributionClientMock.Setup(x =>
             x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
             .ReturnsAsync(participants);

         var updatedCases = new List<ServicenowCases>();
         _serviceNowCasesClientMock.Setup(x =>
             x.Update(It.IsAny<ServicenowCases>()))
             .ReturnsAsync(true)
             .Callback<ServicenowCases>(c => updatedCases.Add(c));

         // Act
         await _service.Run(_timerInfo);

         // Assert
         Assert.AreEqual(2, updatedCases.Count, "Expected both cases to be updated");
         Assert.IsTrue(updatedCases.Any(c => c.ServicenowId == "SN1"), "Case SN1 should be updated");
         Assert.IsTrue(updatedCases.Any(c => c.ServicenowId == "SN2"), "Case SN2 should be updated");
         Assert.IsTrue(updatedCases.All(c => c.Status == ServiceNowStatus.Complete), "All cases should be marked Complete");
     }
 }
