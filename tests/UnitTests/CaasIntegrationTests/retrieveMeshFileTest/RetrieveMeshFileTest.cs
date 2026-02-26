namespace NHS.CohortManager.Tests.CaasIntegrationTests;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using NHS.Screening.RetrieveMeshFile;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using Model;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using System.Globalization;

[TestClass]
public class RetrieveMeshFileTest
{
    private readonly Mock<ILogger<RetrieveMeshFile>> _mockLogger = new();
    private readonly Mock<ILogger<MeshToBlobTransferHandler>> _mockMeshTransferLogger = new();
    private readonly Mock<IMeshInboxService> _mockMeshInboxService = new();
    private readonly Mock<IMeshOperationService> _mockMeshOperationService = new();
    private readonly Mock<IBlobStorageHelper> _mockBlobStorageHelper = new();
    private readonly Mock<IOptions<RetrieveMeshFileConfig>> _config = new();
    private readonly RetrieveMeshFile _retrieveMeshFile;
    private const string mailboxId = "TestMailBox";
    private const string NextHandShakeTimeConfigKey = "NextHandShakeTime";
    private const string ConfigFileName = "MeshState.json";
    private const string BlobStorageConnectionString = "BlobStorage_ConnectionString";
    private const string MessageId = "MessageId";
    private const string FileName = "testFile.csv";
    private const string ContentType = "application/octet-stream";
    private const string ContainerName = "inbound";
    private const string ErrorCode = "Error";

    public RetrieveMeshFileTest()
    {
        var testConfig = new RetrieveMeshFileConfig
        {
            BSSMailBox = mailboxId,
            caasfolder_STORAGE = BlobStorageConnectionString,
            MeshPassword = "MeshPassword",
            MeshSharedKey = "MeshSharedKey",
            MeshKeyPassphrase = "MeshKeyPassphrase",
            MeshKeyName = "MeshKeyName",
            KeyVaultConnectionString = "KeyVaultConnectionString"
        };

        _config.Setup(c => c.Value).Returns(testConfig);

        var _meshToBlobTransferHandler = new MeshToBlobTransferHandler(_mockMeshTransferLogger.Object, _mockBlobStorageHelper.Object, _mockMeshInboxService.Object, _mockMeshOperationService.Object);
        _retrieveMeshFile = new RetrieveMeshFile(_mockLogger.Object, _meshToBlobTransferHandler, _mockBlobStorageHelper.Object, _config.Object);

    }
    [TestMethod]
    public async Task Run_DownloadSingleFileFromMesh_Success()
    {
        //arrange
        var content = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse([MessageId]);
        MeshResponse<HeadMessageResponse> headResponse = MeshResponseTestHelper.CreateSuccessfulMeshHeadResponse(mailboxId, MessageId, FileName);
        MeshResponse<GetMessageResponse> messageResponse = MeshResponseTestHelper.CreateSuccessfulGetMessageResponse(mailboxId, MessageId, FileName, content, ContentType);
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = MeshResponseTestHelper.CreateSuccessfulAcknowledgeResponse(MessageId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(headResponse);
        _mockMeshInboxService.Setup(i => i.GetMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(messageResponse);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(true);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(acknowledgeMessageResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }


    [TestMethod]
    public async Task Run_DownloadSingleChunkedFileFromMesh_Success()
    {
        //arrange
        var content = new List<byte[]> { new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 }, new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 } };

        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse([MessageId]);
        MeshResponse<HeadMessageResponse> headResponse = MeshResponseTestHelper.CreateSuccessfulMeshHeadResponse(mailboxId, MessageId, FileName, "DATA", "1:2", 2);
        MeshResponse<GetChunkedMessageResponse> messageResponse = MeshResponseTestHelper.CreateSuccessfulGetChunkedMessageResponse(mailboxId, MessageId, FileName, ContentType, content);
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = MeshResponseTestHelper.CreateSuccessfulAcknowledgeResponse(MessageId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(headResponse);
        _mockMeshInboxService.Setup(i => i.GetChunkedMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(messageResponse);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(true);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(acknowledgeMessageResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetChunkedMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_DownloadTwoSingleFilesFromMesh_Success()
    {
        //arrange
        List<string> messages = new List<string>{
            "Message1",
            "Message2"
        };
        byte[] byteData = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse(messages);

        var headResponses = new Queue<MeshResponse<HeadMessageResponse>>();
        var messageResponses = new Queue<MeshResponse<GetMessageResponse>>();
        foreach (var message in messages)
        {
            headResponses.Enqueue(MeshResponseTestHelper.CreateSuccessfulMeshHeadResponse(mailboxId, message));
            messageResponses.Enqueue(MeshResponseTestHelper.CreateSuccessfulGetMessageResponse(mailboxId, message, "File.Txt", byteData, ContentType));
        }
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = MeshResponseTestHelper.CreateSuccessfulAcknowledgeResponse("DummyData");

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, It.IsAny<string>())).ReturnsAsync(headResponses.Dequeue());
        _mockMeshInboxService.Setup(i => i.GetMessageByIdAsync(mailboxId, It.IsAny<string>())).ReturnsAsync(messageResponses.Dequeue());
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(true);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, It.IsAny<string>())).ReturnsAsync(acknowledgeMessageResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        _mockMeshInboxService.Verify(i => i.GetMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Exactly(2));
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));

    }

    [TestMethod]
    public async Task Run_NoFilesAvailableInMesh_NoAttemptsToDownload()
    {
        //arrange
        List<string> messages = new List<string>();
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse(messages);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockMeshInboxService.Verify(i => i.GetChunkedMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Never);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_SingleFileAvailableCannotUploadToBlob_DoesntMarkAsAcknowledged()
    {
        //arrange
        var content = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse([MessageId]);
        MeshResponse<HeadMessageResponse> headResponse = MeshResponseTestHelper.CreateSuccessfulMeshHeadResponse(mailboxId, MessageId, FileName);
        MeshResponse<GetMessageResponse> messageResponse = MeshResponseTestHelper.CreateSuccessfulGetMessageResponse(mailboxId, MessageId, FileName, content, ContentType);
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = MeshResponseTestHelper.CreateSuccessfulAcknowledgeResponse(MessageId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(headResponse);
        _mockMeshInboxService.Setup(i => i.GetMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(messageResponse);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(false);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(acknowledgeMessageResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_SingleFileFailsToGetMessageHead_DoesNotAttemptToDownload()
    {
        //arrange
        var content = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse([MessageId]);
        MeshResponse<HeadMessageResponse> headResponse = new MeshResponse<HeadMessageResponse>
        {
            IsSuccessful = false,
            Error = new APIErrorResponse
            {
                ErrorCode = ErrorCode,
                ErrorDescription = "Failed to Head Message"
            }
        };
        MeshResponse<GetMessageResponse> messageResponse = MeshResponseTestHelper.CreateSuccessfulGetMessageResponse(mailboxId, MessageId, FileName, content, ContentType);
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = MeshResponseTestHelper.CreateSuccessfulAcknowledgeResponse(MessageId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(headResponse);
        _mockMeshInboxService.Setup(i => i.GetMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(messageResponse);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(false);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(acknowledgeMessageResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Never);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_DownloadSingleFile_FailToGetMessage()
    {
        //arrange
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse([MessageId]);
        MeshResponse<HeadMessageResponse> headResponse = MeshResponseTestHelper.CreateSuccessfulMeshHeadResponse(mailboxId, MessageId, FileName);
        MeshResponse<GetMessageResponse> messageResponse = new MeshResponse<GetMessageResponse>
        {
            IsSuccessful = false,
            Error = new APIErrorResponse
            {
                ErrorCode = ErrorCode,
                ErrorDescription = "Failed to get Message"
            }
        };
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = MeshResponseTestHelper.CreateSuccessfulAcknowledgeResponse(MessageId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(headResponse);
        _mockMeshInboxService.Setup(i => i.GetMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(messageResponse);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(true);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(acknowledgeMessageResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Never);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_DownloadMessages_FailedToGetMessages()
    {
        //arrange
        MeshResponse<CheckInboxResponse> inboxResponse = new MeshResponse<CheckInboxResponse>
        {
            IsSuccessful = false,
            Error = new APIErrorResponse
            {
                ErrorCode = ErrorCode,
                ErrorDescription = "Failed to CheckInboxMessage"
            }
        };
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, It.IsAny<string>())).ReturnsAsync(It.IsAny<MeshResponse<HeadMessageResponse>>);
        _mockMeshInboxService.Setup(i => i.GetMessageByIdAsync(mailboxId, It.IsAny<string>())).ReturnsAsync(It.IsAny<MeshResponse<GetMessageResponse>>);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(true);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, It.IsAny<string>())).ReturnsAsync(It.IsAny<MeshResponse<AcknowledgeMessageResponse>>);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockMeshInboxService.Verify(i => i.GetMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Never);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_DownloadSingleChunkFile_FailToGetMessage()
    {
        //arrange
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        MeshResponse<CheckInboxResponse> inboxResponse = MeshResponseTestHelper.CreateSuccessfulCheckInboxResponse([MessageId]);
        MeshResponse<HeadMessageResponse> headResponse = MeshResponseTestHelper.CreateSuccessfulMeshHeadResponse(mailboxId, MessageId, FileName, "DATA", "1:2", 2);
        MeshResponse<GetChunkedMessageResponse> messageResponse = new MeshResponse<GetChunkedMessageResponse>
        {
            IsSuccessful = false,
            Error = new APIErrorResponse
            {
                ErrorCode = ErrorCode,
                ErrorDescription = "Failed to get Message"
            }

        };
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = MeshResponseTestHelper.CreateSuccessfulAcknowledgeResponse(MessageId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(headResponse);
        _mockMeshInboxService.Setup(i => i.GetChunkedMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(messageResponse);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage(BlobStorageConnectionString, ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>())).ReturnsAsync(true);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId, MessageId)).ReturnsAsync(acknowledgeMessageResponse);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockMeshInboxService.Verify(i => i.GetChunkedMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(), ContainerName, It.IsAny<BlobFile>(), It.IsAny<bool>()), Times.Never);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Run_Handshake_Success()
    {
        // arrange
        MeshResponse<HandshakeResponse> handshakeResponse = MeshResponseTestHelper.CreateSuccessfulHandshakeResponse(mailboxId);
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        // act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        // assert
        _mockMeshOperationService.Verify(i => i.MeshHandshakeAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_Handshake_Failure()
    {
        // arrange
        MeshResponse<HandshakeResponse> handshakeResponse = new MeshResponse<HandshakeResponse>
        {
            IsSuccessful = false,
            Error = new APIErrorResponse
            {
                ErrorCode = ErrorCode,
                ErrorDescription = "Handshake failed"
            }
        };
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).ReturnsAsync(handshakeResponse);

        // act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        // assert
        _mockMeshOperationService.Verify(i => i.MeshHandshakeAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_Handshake_Exception()
    {
        // arrange
        _mockMeshOperationService.Setup(i => i.MeshHandshakeAsync(mailboxId)).Throws(new Exception("Handshake exception"));

        // act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        // assert
        _mockMeshOperationService.Verify(i => i.MeshHandshakeAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task ShouldExecuteHandshake_NextHandshakeTime_InPast()
    {
        // arrange
        Dictionary<string, string> configValues = new Dictionary<string, string>
        {
            { NextHandShakeTimeConfigKey, DateTime.UtcNow.AddHours(-1).ToString(CultureInfo.InvariantCulture) }
        };
        string json = JsonSerializer.Serialize(configValues);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        _mockBlobStorageHelper.Setup(i => i.GetFileFromBlobStorage(It.IsAny<string>(), "config", ConfigFileName)).ReturnsAsync(new BlobFile(bytes, ConfigFileName));

        // act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        // assert
        _mockMeshOperationService.Verify(i => i.MeshHandshakeAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task ShouldExecuteHandshake_NoMeshStateFile_ReadError()
    {
        // arrange
        _mockBlobStorageHelper.Setup(i => i.GetFileFromBlobStorage(It.IsAny<string>(), "config", ConfigFileName)).ReturnsAsync((BlobFile)null);

        // act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        // assert
        _mockMeshOperationService.Verify(i => i.MeshHandshakeAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task ShouldExecuteHandshake_NextHandshakeTime_ParsingError()
    {
        // arrange
        Dictionary<string, string> configValues = new Dictionary<string, string>
        {
            { NextHandShakeTimeConfigKey, " invalid date " }
        };
        string json = JsonSerializer.Serialize(configValues);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        _mockBlobStorageHelper.Setup(i => i.GetFileFromBlobStorage(It.IsAny<string>(), "config", ConfigFileName)).ReturnsAsync(new BlobFile(bytes, ConfigFileName));

        // act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());

        // assert
        _mockMeshOperationService.Verify(i => i.MeshHandshakeAsync(It.IsAny<string>()), Times.Once);
    }
}
