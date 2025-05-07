namespace NHS.CohortManager.DemographicServices.NEMSUnSubscription;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Model;
using Common;
using System.IO;
using System.Threading.Tasks;
using NHS.Screening.NEMSUnSubscription;
using NHS.CohortManager.Tests.TestUtils;
using Microsoft.Extensions.Options;

[TestClass]
    public class NEMSUnSubscriptionTests : DatabaseTestBaseSetup<NEMSUnSubscription>
    {
        private Mock<INemsSubscriptionService> _nemsSubscriptionServiceMock;
        private Mock<ICreateResponse> _createResponseMock;
        private Mock<ICallFunction> _callFunctionMock;

        private HttpRequestData _request;
        private HttpResponseData _response;
        private static IHttpClientFactory CreateHttpClientFactory()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            return mockFactory.Object;
        }

        private NEMSUnSubscription CreateFunction(INemsSubscriptionService nemsService)
        {
            return new NEMSUnSubscription(
                _loggerMock.Object,
                CreateHttpClientFactory(),
                new Mock<IExceptionHandler>().Object,
                _createResponseMock.Object,
                Options.Create(new NEMSUnSubscriptionConfig
                {
                    NemsDeleteEndpoint = "http://localhost/delete"  //WIP , would change once we get the actual endpoints
                }),
                _callFunctionMock.Object,
                nemsService
            );
}

        public NEMSUnSubscriptionTests() : base(
            (connection,  logger, transaction, command, createResponse) =>
                new NEMSUnSubscription(
                    logger,
                    CreateHttpClientFactory(),
                    new Mock<IExceptionHandler>().Object,
                    createResponse,
                    Options.Create(new NEMSUnSubscriptionConfig
                    {
                        NemsDeleteEndpoint = "http://localhost/delete" //WIP , would change once we get the actual endpoints
                    }),
                    new Mock<ICallFunction>().Object,
                    new Mock<INemsSubscriptionService>().Object
                )
        )
        { }


        [TestInitialize]
        public void Setup()
        {
            _nemsSubscriptionServiceMock = new Mock<INemsSubscriptionService>();
            _createResponseMock = CreateHttpResponseMock();
            _callFunctionMock = new Mock<ICallFunction>();

            var requestBody = JsonSerializer.Serialize(new UnsubscriptionRequest { NhsNumber = "1234567890" });
            _request = SetupRequest(requestBody).Object;
        }

        [TestMethod]
        public async Task Run_ReturnsBadRequest_WhenRequestIsEmpty()
        {
            var request = SetupRequest(string.Empty).Object;

            var func = CreateFunction(_nemsSubscriptionServiceMock.Object);

            var result = await func.Run(request, _context.Object);

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [TestMethod]
        public async Task Run_ReturnsNotFound_WhenSubscriptionIdIsNull()
        {
            _nemsSubscriptionServiceMock.Setup(s => s.LookupSubscriptionIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var func = CreateFunction(_nemsSubscriptionServiceMock.Object);

            var result = await func.Run(_request, _context.Object);

            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }

        [TestMethod]
        public async Task Run_ReturnsOk_WhenUnsubscribedSuccessfully()
        {
            _nemsSubscriptionServiceMock.Setup(s => s.LookupSubscriptionIdAsync(It.IsAny<string>()))
                .ReturnsAsync("abc-123");

            _nemsSubscriptionServiceMock.Setup(s => s.DeleteSubscriptionFromNems("abc-123"))
                .ReturnsAsync(true);

            _nemsSubscriptionServiceMock.Setup(s => s.DeleteSubscriptionFromTableAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var func = CreateFunction(_nemsSubscriptionServiceMock.Object);

            var result = await func.Run(_request, _context.Object);

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }
    }
