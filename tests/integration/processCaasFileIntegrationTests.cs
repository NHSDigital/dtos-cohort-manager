using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Net;
using System.Threading.Tasks;

namespace IntegrationTests.processCaasFile
{
    [TestClass]
    public class ProcessCaasFileHttpEndpointTests
    {
        private readonly string apiEndpoint = Environment.GetEnvironmentVariable("PROCESS_CAAS_API_FILE_ENDOINT") ?? "http://localhost:7061";

        [TestMethod]
        public async Task ProcessCaasFile_Endpoint_ReturnsExpectedResult()
        {
            // Check if endpoint is available
            if (!IsEndpointAvailable(apiEndpoint))
            {
                Assert.Inconclusive($"The API endpoint '{apiEndpoint}' is not available.");
                return;
            }

            // Arrange
            var client = new RestClient(apiEndpoint);
            var request = new RestRequest("api/processCaasFile", Method.Post);
            request.AddJsonBody(new { blobName = "test.csv" }); // Adds JSON body to HTTP request to match endpoints expectations.

            // Ac
            var response = await client.ExecuteAsync(request);

            // Log details for debugging if the request fails
            if (response.StatusCode != HttpStatusCode.OK)
            {
                System.Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                System.Console.WriteLine($"Response content: {response.Content}");
            }

            // Asser
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        private bool IsEndpointAvailable(string url)
        {
            try
            {
                var client = new RestClient(url);
                var request = new RestRequest("/", Method.Get);
                var response = client.Execute(request);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }
    }
}
