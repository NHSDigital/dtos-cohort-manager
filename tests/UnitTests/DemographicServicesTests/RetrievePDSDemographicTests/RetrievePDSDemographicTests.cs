namespace NHS.CohortManager.Tests.UnitTests.DemographicServicesTests;

using System.Net;
using Common;
using Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Moq;
using NHS.CohortManager.DemographicServices;
using DataServices.Client;
using NHS.CohortManager.Tests.TestUtils;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;

[TestClass]
public class RetrievePdsDemographicTests : DatabaseTestBaseSetup<RetrievePdsDemographic>
{
    private static readonly Mock<IHttpClientFunction> _mockHttpClientFunction = new();
    private static readonly Mock<IOptions<RetrievePDSDemographicConfig>> _mockConfig = new();
    private static readonly Mock<IFhirPatientDemographicMapper> _mockFhirPatientDemographicMapper = new();
    private static Mock<IBearerTokenService> _bearerTokenService = new();

    private static Mock<IHttpClientFunction> _httpClientFunction = new();

    private static readonly Mock<IPdsProcessor> _mockPdsProcessor = new();


    private const string _validNhsNumber = "3112728165";
    private const long _validNhsNumberLong = 3112728165;

    public RetrievePdsDemographicTests() : base((conn, logger, transaction, command, response) =>
    new RetrievePdsDemographic(
        logger,
        response,
        _httpClientFunction.Object,
        _mockFhirPatientDemographicMapper.Object,
        _mockConfig.Object,
        _bearerTokenService.Object,
        _mockPdsProcessor.Object
        ))
    {
        CreateHttpResponseMock();
    }
}
