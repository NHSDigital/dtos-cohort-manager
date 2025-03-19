namespace PlayWriteTestsEndToEnd;




using Microsoft.Playwright;
using NUnit.Framework;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.IO;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;


[TestClass]
public sealed class Test1
{


    private IPage _page;
    private IBrowser _browser;
    private IBrowserContext _context;
    private BlobServiceClient _blobServiceClient;
    private const string _containerName = "inbound";

    private const string queueName = "add-participant-queue";
    private QueueClient _queue;

    private string _connectionString = "";


    public Test1()
    {
        _blobServiceClient = new BlobServiceClient(_connectionString);
        _queue = new QueueClient(_connectionString, queueName);
    }

    [TestMethod]
    public async Task TestMethod1()
    {

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient("add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet");

        var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        assemblyDirectory = assemblyDirectory.Replace("bin/Debug/net8.0", "");
        var filePath = Path.Combine(assemblyDirectory, "add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet");


        using (FileStream fs = File.OpenRead(filePath))
        {
            await blobClient.UploadAsync(fs, overwrite: true);
        }

        // assert that the cohort distribution made it to the database
        //await Page.GotoAsync("https://playwright.dev");

        // Expect a title "to contain" a substring.
        //await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));
        Assert.That(await CheckDatabase());
    }

    private async Task<bool> CheckDatabase()
    {
        var rowCount = 1; // Example row count
        var url = $"http://localhost:7095/api/RetrieveCohortDistributionData?rowCount={rowCount}";

        using HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(responseData))
            {
                return true;
            }
            return false;
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
        }
        return false;
    }

    static async Task<bool> RetrieveNextMessageAsync(QueueClient theQueue)
    {
        if (await theQueue.ExistsAsync())
        {
            QueueProperties properties = await theQueue.GetPropertiesAsync();

            if (properties.ApproximateMessagesCount > 0)
            {
                QueueMessage[] retrievedMessage = await theQueue.ReceiveMessagesAsync(1);
                var theMessage = retrievedMessage[0].Body.ToString();
                await theQueue.DeleteMessageAsync(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);

                if (string.IsNullOrEmpty(theMessage))
                {
                    return false;
                }
                return true;
            }
        }
        return false;
    }
}
