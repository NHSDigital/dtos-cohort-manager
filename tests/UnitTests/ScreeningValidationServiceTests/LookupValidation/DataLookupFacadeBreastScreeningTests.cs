namespace NHS.CohortManager.Tests.UnitTests.ScreeningValidationServiceTests;


using DataServices.Client;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NHS.CohortManager.ScreeningValidationService;


[TestClass]
public class DataLookupFacadeBreastScreeningTests
{

    private readonly Mock<ILogger<DataLookupFacadeBreastScreening>> _logger = new();
    private readonly Mock<IDataServiceClient<BsSelectGpPractice>> _gpPracticeServiceClient = new();
    private readonly Mock<IDataServiceClient<BsSelectOutCode>> _outcodeClient = new();
    private readonly Mock<IDataServiceClient<CurrentPosting>> _currentPostingClient = new();
    private readonly Mock<IDataServiceClient<ExcludedSMULookup>> _excludedSMUClient = new();

    private IDataLookupFacadeBreastScreening _dataLookupFacade;

    public DataLookupFacadeBreastScreeningTests()
    {
        _dataLookupFacade = new DataLookupFacadeBreastScreening(_logger.Object,_gpPracticeServiceClient.Object,_outcodeClient.Object,_currentPostingClient.Object,_excludedSMUClient.Object);
    }


    [TestMethod]
    [DataRow("CYM","WALES")]
    [DataRow(null,null)]
    public void RetrievePostingCategory_validRequest_ReturnsPostingCategory(string currentPosting, string expectedPostingCategory)
    {
        //arrange
        _currentPostingClient.Setup(x => x.GetSingle(currentPosting)).ReturnsAsync(new CurrentPosting{PostingCategory = expectedPostingCategory});
        //act
        var result = _dataLookupFacade.RetrievePostingCategory(currentPosting);

        //assert
        Assert.AreEqual(expectedPostingCategory,result);
    }

    [TestMethod]
    [DataRow("ENGLAND")]
    [DataRow("IOM")]
    [DataRow("DMS")]
    public void ValidatePostingCategories_ValidRequest_ReturnsTrue(string postingCategory)
    {
        // Arrange
        _currentPostingClient.Setup(x => x.GetSingle(postingCategory)).ReturnsAsync(new CurrentPosting { PostingCategory = postingCategory });
        // Act
        bool result = _dataLookupFacade.ValidatePostingCategories(postingCategory);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow("WALES")]
    public void ValidatePostingCategories_InvalidRequest_ReturnsFalse(string postingCategory)
    {
        // Arrange
        _currentPostingClient.Setup(x => x.GetSingle(postingCategory)).ReturnsAsync(new CurrentPosting { PostingCategory = postingCategory });
        // Act
        bool result = _dataLookupFacade.ValidatePostingCategories(postingCategory);

        // Assert
        Assert.IsFalse(result);
    }

}
