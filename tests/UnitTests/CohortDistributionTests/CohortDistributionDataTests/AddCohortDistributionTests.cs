namespace NHS.CohortManager.Tests.CohortDistributionServiceTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Interfaces;
using Data.Database;
using DataServices.Client;
using Model;
using Moq;
using System.Linq.Expressions;

[TestClass]
public class AddCohortDistributionTests
{
    private Mock<IDataServiceClient<CohortDistribution>> _cohortDistributionDataServiceClient;
    private Mock<IDataServiceClient<BsSelectRequestAudit>> _bsSelectRequestAuditDataServiceClient;
    private IExtractCohortDistributionRecordsStrategy _extractionStrategy;
    private CreateCohortDistributionData _service;
    private List<CohortDistribution> _participantList;

    [TestInitialize]
    public void Setup()
    {
        _cohortDistributionDataServiceClient = new Mock<IDataServiceClient<CohortDistribution>>();
        _bsSelectRequestAuditDataServiceClient = new Mock<IDataServiceClient<BsSelectRequestAudit>>();
        _participantList = new List<CohortDistribution>();
        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync((Expression<Func<CohortDistribution, bool>> filter) =>
                _participantList.Where(filter.Compile()).ToList());

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((string id) =>
                _participantList.FirstOrDefault(p => p.CohortDistributionId.ToString() == id));

        _cohortDistributionDataServiceClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync((CohortDistribution cohort) =>
            {
                var existing = _participantList.FirstOrDefault(c => c.CohortDistributionId == cohort.CohortDistributionId);
                if (existing != null)
                {
                    existing.IsExtracted = cohort.IsExtracted;
                    existing.RequestId = cohort.RequestId;
                    return true;
                }
                return false;
            });

        _extractionStrategy = new ExtractCohortDistributionRecords(_cohortDistributionDataServiceClient.Object);

        _service = new CreateCohortDistributionData(
            _cohortDistributionDataServiceClient.Object,
            _bsSelectRequestAuditDataServiceClient.Object,
            _extractionStrategy
        );
    }

    // Tests for old logic when retrieveSupersededRecordsLast is false

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsOneRecordWhenRetrieveSupersededRecordsLastIsFalse()
    {
        // Arrange
        _participantList.AddRange(new List<CohortDistribution>
        {
            new CohortDistribution
            {
                ParticipantId = 1,
                CohortDistributionId = 1,
                NHSNumber = 99900000000,
                IsExtracted = 0,
                RequestId = Guid.Empty,
                SupersededNHSNumber = null,
                RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
            },
            new CohortDistribution
            {
                ParticipantId = 2,
                CohortDistributionId = 2,
                NHSNumber = 99900000001,
                IsExtracted = 0,
                RequestId = Guid.Empty,
                SupersededNHSNumber = 99988877777,
                RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
            }
        });

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10, false);

        // Assert
        Assert.IsTrue(result.Count == 2, "Should return two records when retrieveSupersededRecordsLast is false.");
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsUpToRowCount()
    {
        // Arrange - Add 5 participants
        for (int i = 1; i <= 5; i++)
        {
            _participantList.Add(new CohortDistribution
            {
                ParticipantId = i,
                CohortDistributionId = i,
                NHSNumber = 9990000000 + i,
                IsExtracted = 0,
                RequestId = Guid.Empty,
                SupersededNHSNumber = null,
                RecordInsertDateTime = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(3, false);

        // Assert
        Assert.AreEqual(3, result.Count());
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsExpectedDto()
    {
        // Arrange
        _participantList.Add(new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000000,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = 99900000001,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        });

        // Act - Tests real filtering and DTO mapping logic
        var result = await _service.GetUnextractedCohortDistributionParticipants(10, false);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("99900000000", result[0].NhsNumber);
        Assert.AreEqual("99900000001", result[0].SupersededByNhsNumber);
        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual("0", result[0].IsExtracted); // This has been updated in the DB but DTO reflects original value
        Assert.IsTrue(Guid.TryParse(result[0].RequestId, out var guid) && guid != Guid.Empty, "RequestId should be a non-empty GUID.");
    }

    [TestMethod]
    public async Task GetCohortDistributionParticipantsByRequestId_ReturnsEmptyForUnknownRequestId()
    {
        // Arrange
        var knownRequestId = Guid.NewGuid();
        var unknownRequestId = Guid.NewGuid();

        _participantList.Add(new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000000,
            IsExtracted = 1,
            RequestId = knownRequestId,
            SupersededNHSNumber = 99900000001,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        });

        // Act - Tests filtering by RequestId
        var result = await _service.GetCohortDistributionParticipantsByRequestId(unknownRequestId);

        // Assert
        Assert.AreEqual(0, result.Count, "Should return no participants for an unknown requestId.");
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_OnlyReturnsSupersededWithMatchingNhsNumber()
    {
        // Arrange
        var supersededNhsNumberWithMatch = 99988888888;

        _participantList.Add(new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000001,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = supersededNhsNumberWithMatch,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        });

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10, false);

        // Assert
        Assert.AreEqual(1, result.Count, "Only one superseded participant should be extracted (the one with a matching NHSNumber).");
        Assert.AreEqual("99900000001", result[0].NhsNumber);
        Assert.AreEqual(supersededNhsNumberWithMatch.ToString(), result[0].SupersededByNhsNumber);
    }

// Tests for new logic when retrieveSupersededRecordsLast is true
    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsEmptyWhenNoSupersededMatch()
    {
        // Arrange
        _participantList.Add(new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000001,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = 99988888888, // No matching extracted record with this NHS number
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        });

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10, true);

        // Assert
        Assert.AreEqual(0, result.Count, "Should return no participants when there is no matching extracted NHSNumber.");
    }

     [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_UsesExtractionStrategyWhenRetrieveSupersededRecordsLastIsTrue()
    {
        // Arrange
        _participantList.Add(new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000000,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = null,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        });

        // Act - This now tests the real ExtractCohortDistributionRecords strategy
        var result = await _service.GetUnextractedCohortDistributionParticipants(10, true);

        // Assert - Verify the strategy correctly extracted the participant
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("99900000000", result[0].NhsNumber);
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsAllWhenAllSupersededMatch()
    {
        // Arrange
        var supersededNhsNumbers = new[] { 99911111111, 99922222222};

        // Add the extracted records that the superseded records will match against
        _participantList.AddRange(new List<CohortDistribution>
        {
            new CohortDistribution
            {
                ParticipantId = 10,
                CohortDistributionId = 10,
                NHSNumber = supersededNhsNumbers[0],
                IsExtracted = 1,
                RequestId = Guid.NewGuid(),
                SupersededNHSNumber = null,
                RecordInsertDateTime = DateTime.UtcNow.AddDays(-10)
            },
            new CohortDistribution
            {
                ParticipantId = 11,
                CohortDistributionId = 11,
                NHSNumber = supersededNhsNumbers[1],
                IsExtracted = 1,
                RequestId = Guid.NewGuid(),
                SupersededNHSNumber = null,
                RecordInsertDateTime = DateTime.UtcNow.AddDays(-11)
            }
        });

        // Add the unextracted superseded participants
        _participantList.AddRange(new List<CohortDistribution>
        {
            new CohortDistribution
            {
                ParticipantId = 1,
                CohortDistributionId = 1,
                NHSNumber = 99900000001,
                IsExtracted = 0,
                RequestId = Guid.Empty,
                SupersededNHSNumber = supersededNhsNumbers[0],
                RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
            },
            new CohortDistribution
            {
                ParticipantId = 2,
                CohortDistributionId = 2,
                NHSNumber = 99900000002,
                IsExtracted = 0,
                RequestId = Guid.Empty,
                SupersededNHSNumber = supersededNhsNumbers[1],
                RecordInsertDateTime = DateTime.UtcNow.AddDays(-2)
            }
        });

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10, true);

        // Assert
        Assert.AreEqual(2, result.Count, "Should return all superseded participants when all have a matching extracted NHSNumber.");
    }

    [TestMethod]
    public async Task GetCohortDistributionParticipantsByRequestId_ReturnsExpectedDto()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var otherRequestId = Guid.NewGuid();

        _participantList.AddRange(new List<CohortDistribution>
        {
            new CohortDistribution
            {
                ParticipantId = 1,
                CohortDistributionId = 1,
                NHSNumber = 99900000000,
                IsExtracted = 1,
                RequestId = requestId,
                SupersededNHSNumber = 99900000001,
                RecordInsertDateTime = DateTime.UtcNow
            },
            new CohortDistribution
            {
                ParticipantId = 2,
                CohortDistributionId = 2,
                NHSNumber = 99900000002,
                IsExtracted = 1,
                RequestId = otherRequestId,
                SupersededNHSNumber = null,
                RecordInsertDateTime = DateTime.UtcNow
            },
            new CohortDistribution
            {
                ParticipantId = 3,
                CohortDistributionId = 3,
                NHSNumber = 99900000003,
                IsExtracted = 1,
                RequestId = requestId,
                SupersededNHSNumber = null,
                RecordInsertDateTime = DateTime.UtcNow
            }
        });

        // Act
        var result = await _service.GetCohortDistributionParticipantsByRequestId(requestId);

        // Assert correct participants are returned.  This should return all records with the requestId, regardless of superseded by nhs number status
        Assert.AreEqual(2, result.Count, "Should return only participants with matching RequestId");
        Assert.IsTrue(result.All(r => r.RequestId == requestId.ToString()), "All returned DTOs should have the correct RequestId.");

        // Assert returned DTOs match expected data
        var expected = _participantList.Where(c => c.RequestId == requestId).ToList();
        for (int i = 0; i < 2; i++)
        {
            Assert.AreEqual(expected[i].NHSNumber.ToString(), result[i].NhsNumber);
            Assert.AreEqual(expected[i].SupersededNHSNumber?.ToString() ?? "", result[i].SupersededByNhsNumber);
            Assert.AreEqual(expected[i].ParticipantId.ToString(), result[i].ParticipantId);
            Assert.AreEqual(expected[i].IsExtracted.ToString(), result[i].IsExtracted);
            Assert.AreEqual(expected[i].RequestId.ToString(), result[i].RequestId);
        }
    }
}
