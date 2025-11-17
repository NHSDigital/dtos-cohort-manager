using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private CreateCohortDistributionData _service;

    [TestInitialize]
    public void Setup()
    {
        _cohortDistributionDataServiceClient = new Mock<IDataServiceClient<CohortDistribution>>();
        _bsSelectRequestAuditDataServiceClient = new Mock<IDataServiceClient<BsSelectRequestAudit>>();
        _service = new CreateCohortDistributionData(
            _cohortDistributionDataServiceClient.Object,
            _bsSelectRequestAuditDataServiceClient.Object
        );
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_CallsGetByFilterWithCorrectFilter()
    {
        // Arrange
        var capturedFilters = new List<Expression<Func<CohortDistribution, bool>>>();

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .Callback<Expression<Func<CohortDistribution, bool>>>(expr => capturedFilters.Add(expr))
            .ReturnsAsync(new List<CohortDistribution>());

        // Act
        await _service.GetUnextractedCohortDistributionParticipants(10);

        // Assert
        Assert.IsNotNull(capturedFilters.Count == 2, "Filter expression was not captured.");

        // Test the first filter for non-superseded by numbers
        var testParticipant = new CohortDistribution
        {
            ParticipantId = 123,
            NHSNumber = 9997890990,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = null,
            RecordInsertDateTime = DateTime.UtcNow
        };
        // Compile and invoke the filter
        bool isMatch = capturedFilters[0].Compile().Invoke(testParticipant);

        Assert.IsTrue(isMatch, "Filter should match unextracted participant without superseded number.");

        // Test the second filter for superseded by numbers
        var testParticipant2 = new CohortDistribution
        {
            ParticipantId = 123,
            NHSNumber = 9997890990,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = 9993338881,
            RecordInsertDateTime = DateTime.UtcNow
        };
        // Compile and invoke the filter
        bool isMatchSuperseded = capturedFilters[1].Compile().Invoke(testParticipant2);

        Assert.IsTrue(isMatchSuperseded, "Filter should match unextracted participant with superseded number.");
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsExpectedDto()
    {
        // Arrange
        var cohortList = new List<CohortDistribution>
            {
                new CohortDistribution
                {
                    ParticipantId = 1,
                    CohortDistributionId = 1,
                    NHSNumber = 99900000000,
                    IsExtracted = 0,
                    RequestId = Guid.Empty,
                    SupersededNHSNumber = 99900000001,
                    RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
                }
            };

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<System.Linq.Expressions.Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(cohortList);

        _cohortDistributionDataServiceClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync(true);

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync(cohortList[0]);

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("99900000000", result[0].NhsNumber);
        Assert.AreEqual("99900000001", result[0].SupersededByNhsNumber);
        Assert.AreEqual("1", result[0].ParticipantId);
        Assert.AreEqual("0", result[0].IsExtracted); // This has been updated in the DB but DTO reflects original value
        Assert.IsTrue(Guid.TryParse(result[0].RequestId, out var guid) && guid != Guid.Empty, "RequestId should be a non-empty GUID.");
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsUpToRowCount()
    {
        // Arrange
        var participants = new List<CohortDistribution>();
        for (int i = 1; i <= 5; i++)
        {
            participants.Add(new CohortDistribution
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

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<System.Linq.Expressions.Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(participants);
        _cohortDistributionDataServiceClient.Setup(x => x.Update(It.IsAny<CohortDistribution>())).ReturnsAsync(true);
        _cohortDistributionDataServiceClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((string id) => participants.FirstOrDefault(p => p.CohortDistributionId.ToString() == id));
        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(3);

        // Assert
        Assert.AreEqual(3, result.Count());
    }

    [TestMethod]
    public async Task GetCohortDistributionParticipantsByRequestId_ReturnsEmptyForUnknownRequestId()
    {
        // Arrange
        var knownRequestId = Guid.NewGuid();
        var unknownRequestId = Guid.NewGuid();
        var cohortList = new List<CohortDistribution>
    {
        new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000000,
            IsExtracted = 1,
            RequestId = knownRequestId,
            SupersededNHSNumber = 99900000001,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        }
    };

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync((Expression<Func<CohortDistribution, bool>> filter) =>
                cohortList.Where(filter.Compile()).ToList());
        // Act
        var result = await _service.GetCohortDistributionParticipantsByRequestId(unknownRequestId);

        // Assert
        Assert.AreEqual(0, result.Count, "Should return no participants for an unknown requestId.");
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_OnlyReturnsSupersededWithMatchingNhsNumber()
    {
        // Arrange
        var supersededNhsNumberWithMatch = 99988888888;
        var supersededNhsNumberWithoutMatch = 99977777777;

        var supersededParticipants = new List<CohortDistribution>
    {
        new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000001,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = supersededNhsNumberWithMatch,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        },
        new CohortDistribution
        {
            ParticipantId = 2,
            CohortDistributionId = 2,
            NHSNumber = 99900000002,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = supersededNhsNumberWithoutMatch,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-2)
        }
    };

        var matchingParticipant = new CohortDistribution
        {
            ParticipantId = 3,
            CohortDistributionId = 3,
            NHSNumber = supersededNhsNumberWithMatch,
            IsExtracted = 1,
            RequestId = Guid.NewGuid(),
            SupersededNHSNumber = null,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-3)
        };

        // Setup: Always return a non-null list based on filter logic
        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync((Expression<Func<CohortDistribution, bool>> filter) =>
            {
                // First call: filter for unextractedParticipants (none)
                if (filter.Body.ToString().Contains("SupersededNHSNumber == null"))
                    return new List<CohortDistribution>();

                // Second call: filter for supersededParticipants
                if (filter.Body.ToString().Contains("SupersededNHSNumber != null"))
                    return supersededParticipants;

                // Subsequent calls: filter for matching NHSNumber
                var compiled = filter.Compile();
                var allParticipants = new List<CohortDistribution> { matchingParticipant };
                return allParticipants.Where(compiled).ToList();
            });

        _cohortDistributionDataServiceClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync(true);

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((string id) =>
                supersededParticipants.Concat(new[] { matchingParticipant })
                    .FirstOrDefault(p => p.CohortDistributionId.ToString() == id));

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10);

        // Assert
        Assert.AreEqual(1, result.Count, "Only one superseded participant should be extracted (the one with a matching NHSNumber).");
        Assert.AreEqual("99900000001", result[0].NhsNumber);
        Assert.AreEqual(supersededNhsNumberWithMatch.ToString(), result[0].SupersededByNhsNumber);
    }

    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsEmptyWhenNoSupersededMatch()
    {
        // Arrange
        var supersededParticipants = new List<CohortDistribution>
    {
        new CohortDistribution
        {
            ParticipantId = 1,
            CohortDistributionId = 1,
            NHSNumber = 99900000001,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = 99945678901,
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-1)
        }
    };

        // No matching NHSNumber in the database
        var matchingParticipants = new List<CohortDistribution>();

        _cohortDistributionDataServiceClient
            .SetupSequence(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync(new List<CohortDistribution>()) // unextractedParticipants
            .ReturnsAsync(supersededParticipants)         // supersededParticipants
            .ReturnsAsync(matchingParticipants);          // Participant where superseded by number has a record

        _cohortDistributionDataServiceClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync(true);

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((string id) =>
                supersededParticipants.FirstOrDefault(p => p.CohortDistributionId.ToString() == id));

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10);

        // Assert
        Assert.AreEqual(0, result.Count, "Should return no participants when there is no matching NHSNumber.");
    }
    [TestMethod]
    public async Task GetUnextractedCohortDistributionParticipants_ReturnsAllWhenAllSupersededMatch()
    {
        // Arrange
        var supersededNhsNumbers = new[] { 99911111111, 99922222222, 99933333333 };
        var supersededParticipants = new List<CohortDistribution>
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
        },
        new CohortDistribution
        {
            ParticipantId = 3,
            CohortDistributionId = 3,
            NHSNumber = 9990000000,
            IsExtracted = 0,
            RequestId = Guid.Empty,
            SupersededNHSNumber = supersededNhsNumbers[2],
            RecordInsertDateTime = DateTime.UtcNow.AddDays(-2)
        }
    };

        var matchingParticipants = new List<CohortDistribution>
    {
        new CohortDistribution { NHSNumber = supersededNhsNumbers[0], IsExtracted = 1 },
        new CohortDistribution { NHSNumber = supersededNhsNumbers[1], IsExtracted = 1 },
        new CohortDistribution { NHSNumber = supersededNhsNumbers[2], IsExtracted = 0 }
    };

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync((Expression<Func<CohortDistribution, bool>> filter) =>
            {
                // First call: filter for unextractedParticipants (none)
                if (filter.Body.ToString().Contains("SupersededNHSNumber == null"))
                    return new List<CohortDistribution>();

                // Second call: filter for supersededParticipants
                if (filter.Body.ToString().Contains("SupersededNHSNumber != null"))
                    return supersededParticipants;

                // Subsequent calls: filter for matching NHSNumber
                var compiled = filter.Compile();
                return matchingParticipants.Where(compiled).ToList();
            });

        _cohortDistributionDataServiceClient
            .Setup(x => x.Update(It.IsAny<CohortDistribution>()))
            .ReturnsAsync(true);

        _cohortDistributionDataServiceClient
            .Setup(x => x.GetSingle(It.IsAny<string>()))
            .ReturnsAsync((string id) =>
                supersededParticipants.FirstOrDefault(p => p.CohortDistributionId.ToString() == id));

        // Act
        var result = await _service.GetUnextractedCohortDistributionParticipants(10);

        // Assert
        Assert.AreEqual(2, result.Count, "Should return all superseded participants when all have a matching NHSNumber.");
    }
    [TestMethod]
    public async Task GetCohortDistributionParticipantsByRequestId_ReturnsExpectedDto()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var cohortList = new List<CohortDistribution>
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
            RequestId = Guid.NewGuid(),
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
    };
        _cohortDistributionDataServiceClient
            .Setup(x => x.GetByFilter(It.IsAny<Expression<Func<CohortDistribution, bool>>>()))
            .ReturnsAsync((Expression<Func<CohortDistribution, bool>> filter) =>
            cohortList.Where(filter.Compile()).ToList());

        // Act
        var result = await _service.GetCohortDistributionParticipantsByRequestId(requestId);

        // Assert correct participants are returned
        Assert.IsTrue(result.All(r => r.RequestId == requestId.ToString()), "All returned DTOs should have the correct RequestId.");

        // Assert returned DTOs match expected data
        var expected = cohortList.Where(c => c.RequestId == requestId).ToList();
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
