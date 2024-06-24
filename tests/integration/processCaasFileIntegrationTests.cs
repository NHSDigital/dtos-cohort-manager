using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
using System.Net;
using System.Threading.Tasks;

namespace IntegrationTests.processCaasFile
{
    [TestClass]
    public class ProcessCaasFileHttpEndpointTests
    {
        [TestMethod]
        public async Task ProcessCaasFile_Endpoint_ReturnsExpectedResult()
        {
            // Arrange
            var client = new RestClient("http://localhost:7061");
            var request = new RestRequest("api/processCaasFile", Method.Post);
            request.AddJsonBody(new { blobName = "test.csv" }); // Adds JSON body to HTTP request to match endpoints expectations.

            // Act
            var response = await client.ExecuteAsync(request);

            // Log details for debugging if the request fails
            if (response.StatusCode != HttpStatusCode.OK)
            {
                System.Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                System.Console.WriteLine($"Response content: {response.Content}");
            }

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}