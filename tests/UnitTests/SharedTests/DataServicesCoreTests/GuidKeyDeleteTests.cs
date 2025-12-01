using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataServices.Core;
using DataServices.Database;
using Model;
using NHS.CohortManager.Tests.TestUtils;
using System.Net;
using System.Net.Http;

namespace NHS.CohortManager.Tests.Shared.DataServicesCore
{
    [TestClass]
    public class GuidKeyDeleteTests
    {
        private DataServicesContext _context = null!;
        private DataServiceAccessor<ServicenowCase> _accessor = null!;
        private RequestHandler<ServicenowCase> _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DataServicesContext>()
                .UseInMemoryDatabase(databaseName: $"DataServices-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new DataServicesContext(options);
            _accessor = new DataServiceAccessor<ServicenowCase>(_context, new NullLogger<DataServiceAccessor<ServicenowCase>>() );

            // allow all operations
            AccessRule allow = _ => true;
            var auth = new AuthenticationConfiguration(allow, allow, allow, allow, allow);
            _sut = new RequestHandler<ServicenowCase>(_accessor, new NullLogger<RequestHandler<ServicenowCase>>(), auth);
        }


        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }


        [TestMethod]
        public async Task Delete_ByGuidKey_RemovesEntity_AndReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new ServicenowCase
            {
                Id = id,
                ServicenowId = "CASE0001",
                NhsNumber = 9990001111
            };
            _context.Add(entity);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var setup = new SetupRequest();
            var request = setup.Setup(method: HttpMethod.Delete);

            // Act
            var response = await _sut.HandleRequest(request.Object, id.ToString());

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var remaining = await _context.Set<ServicenowCase>().FindAsync(id);
            Assert.IsNull(remaining);
        }
    }
}
