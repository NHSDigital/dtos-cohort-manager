namespace NHS.CohortManager.Tests.SnapShotTests;

using System.Threading.Tasks;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using VerifyMSTest;


[TestClass]
public class AddFileTests : VerifyBase
{
    private DataServicesContext _dbContext;
    [TestInitialize]
    public void Setup()
    {

        var options = new DbContextOptionsBuilder<DataServicesContext>().UseSqlServer("").Options;
        _dbContext = new DataServicesContext(options);
    }
    [TestMethod]
    public async Task ValidateExceptionTable()
    {
        var Exceptions = await _dbContext.exceptionManagements.ToListAsync();
        await Verify(Exceptions);
    }
}
