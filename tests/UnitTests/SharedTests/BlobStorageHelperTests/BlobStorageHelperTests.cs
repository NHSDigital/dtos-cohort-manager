namespace NHS.CohortManager.Tests.UnitTests.SharedTests;


using System;
using Common;
using System.IO;
using Model;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;


[TestClass]
    public class BlobStorageHelperTests
    {
        private Mock<BlobServiceClient> _mockBlobServiceClient;
        private Mock<BlobContainerClient> _mockBlobContainerClient;
        private Mock<BlobClient> _mockBlobClient;
        private Mock<ILogger<BlobStorageHelper>> _mockLogger;
        private BlobStorageHelper _blobStorageHelper;

        private readonly string _connectionString = "UseDevelopmentStorage=true"; // Azurite Connection String
        private readonly string _containerName = "test-container";
        private readonly string _fileName = "testfile.txt";

        [TestInitialize]


        public void Setup()
        {
            _mockBlobServiceClient = new Mock<BlobServiceClient>();
            _mockBlobContainerClient = new Mock<BlobContainerClient>();
            _mockBlobClient = new Mock<BlobClient>();
            _mockLogger = new Mock<ILogger<BlobStorageHelper>>();

            _mockBlobServiceClient
                .Setup(m => m.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_mockBlobContainerClient.Object);

            _mockBlobContainerClient
                .Setup(m => m.GetBlobClient(It.IsAny<string>()))
                .Returns(_mockBlobClient.Object);

            _blobStorageHelper = new BlobStorageHelper(_mockLogger.Object);
        }

   [TestMethod]
    public async Task GetFileFromBlobStorage_FileExists_ReturnsBlobFile()
    {
        // Arrange: Use older API version for Azurite compatibility
        var options = new BlobClientOptions();
        var blobServiceClient = new BlobServiceClient(_connectionString, options);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        // Create a test blob
        var blobClient = containerClient.GetBlobClient(_fileName);
        var testData = "This is test data";
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testData)))
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        var yourClassInstance = new BlobStorageHelper(_mockLogger.Object);

        // Act
        var result = await yourClassInstance.GetFileFromBlobStorage(_connectionString, _containerName, _fileName);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(_fileName, result.FileName);
    }


    [TestMethod]
    public async Task GetFileFromBlobStorage_FileDoesNotExist_ReturnsNull()
    {
        // Arrange: Use older API version for Azurite compatibility
        var options = new BlobClientOptions();
        var blobServiceClient = new BlobServiceClient(_connectionString, options);
        var yourClassInstance = new BlobStorageHelper(_mockLogger.Object);

        // Act
        var result = await yourClassInstance.GetFileFromBlobStorage(_connectionString, _containerName, "nonexistent.txt");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CopyFileAsync_SuccessfulCopy_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable("fileExceptions", "destination-container");

        // Arrange: Use older API version for Azurite compatibility
        var options = new BlobClientOptions();
        var blobServiceClient = new BlobServiceClient(_connectionString, options);

        var sourceContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        var destinationContainerClient = blobServiceClient.GetBlobContainerClient("destination-container");
        await sourceContainerClient.CreateIfNotExistsAsync();
        await destinationContainerClient.CreateIfNotExistsAsync();

        // Upload a test file
        var sourceBlobClient = sourceContainerClient.GetBlobClient(_fileName);
        var testData = "Sample data";
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testData)))
        {
            await sourceBlobClient.UploadAsync(stream, overwrite: true);
        }

        var blobStorageHelper = new BlobStorageHelper(_mockLogger.Object);

        // Act
        var result = await blobStorageHelper.CopyFileAsync(_connectionString, _fileName, _containerName);

        // Assert
        Assert.IsTrue(result);
    }


        [TestMethod]
        public async Task UploadFileToBlobStorage_UploadFails_ReturnFalse()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            var blobFile = new BlobFile(memoryStream, "test.txt");

            _mockBlobClient
                .Setup(m => m.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException("Upload failed")); // Simulate failure

            // Act
            var result = await _blobStorageHelper.UploadFileToBlobStorage("UseDevelopmentStorage=true", "container", blobFile);

            // Assert
            Assert.IsFalse(result, "UploadFileToBlobStorage should return false when upload fails.");
        }

    }
