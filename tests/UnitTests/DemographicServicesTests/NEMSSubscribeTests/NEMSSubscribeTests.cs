namespace NHS.CohortManager.Tests.UnitTests.NEMSSubscribeTests;

using Moq;
using Moq.Protected;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Common;
using Model;
using NHS.CohortManager.DemographicServices;
using NHS.CohortManager.Tests.TestUtils;

using FhirTask = Hl7.Fhir.Model.Task; // 👈 Alias to avoid ambiguity

    [TestClass]
    public class NEMSSubscribeTests
    {
        private Mock<ILogger<NEMSSubscribe>> _loggerMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<IExceptionHandler> _exceptionHandlerMock;
        private Mock<ICreateResponse> _createResponseMock;
        private Mock<ICallFunction> _callFunctionMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;

        private NEMSSubscribe _nemsSubscribe;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<NEMSSubscribe>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _exceptionHandlerMock = new Mock<IExceptionHandler>();
            _createResponseMock = new Mock<ICreateResponse>();
            _callFunctionMock = new Mock<ICallFunction>();

            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _nemsSubscribe = new NEMSSubscribe(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _exceptionHandlerMock.Object,
                _createResponseMock.Object,
                _callFunctionMock.Object
            );
        }

        [TestMethod]
        public async Task Run_ShouldReturnCreated_WhenValidRequest()
        {
            // Arrange
            var nhsNumber = "1234567890";
            var subscriptionId = "sub-001";

            var requestObj = new NemsSubscriptionRequest { NhsNumber = nhsNumber };
            var requestJson = JsonSerializer.SerializeToUtf8Bytes(requestObj);

            var functionContext = new Mock<FunctionContext>();
            var reqMock = new Mock<HttpRequestData>(functionContext.Object);
            var responseDataMock = new Mock<HttpResponseData>(functionContext.Object);

            responseDataMock.SetupAllProperties();
            reqMock.Setup(r => r.Body).Returns(new MemoryStream(requestJson));
            reqMock.Setup(r => r.ReadFromJsonAsync<NemsSubscriptionRequest>(default)).ReturnsAsync(requestObj);
            reqMock.Setup(r => r.CreateResponse(HttpStatusCode.Created)).Returns(responseDataMock.Object);

            // Mock internal methods
            var nemsSubMock = new Mock<NEMSSubscription>(_loggerMock.Object,
                                                         _httpClientFactoryMock.Object,
                                                         _exceptionHandlerMock.Object,
                                                         _createResponseMock.Object,
                                                         _callFunctionMock.Object) { CallBase = true };

            nemsSubMock.Setup(x => x.ValidateAgainstPds(nhsNumber)).ReturnsAsync(true);
            nemsSubMock.Setup(x => x.PostSubscriptionToNems(It.IsAny<string>())).ReturnsAsync(subscriptionId);
            nemsSubMock.Setup(x => x.StoreSubscriptionInDatabase(nhsNumber, subscriptionId)).ReturnsAsync(true);

            // Act
            var result = await nemsSubMock.Object.Run(reqMock.Object);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        }
    }
