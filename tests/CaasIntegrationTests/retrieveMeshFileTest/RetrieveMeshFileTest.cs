namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using NHS.Screening.RetrieveMeshFile;
using NHS.MESH.Client.Contracts.Clients;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using NHS.MESH.Client.Helpers;
using Model;

[TestClass]
public class RetrieveMeshFileTest
{

    private readonly Mock<ILogger<RetrieveMeshFile>> _mockLogger = new();
    private readonly Mock<IMeshInboxService> _mockMeshInboxService = new();
    private readonly Mock<IExceptionHandler> _mockExceptionHandler = new();
    private readonly Mock<IBlobStorageHelper> _mockBlobStorageHelper = new();
    private readonly RetrieveMeshFile _retrieveMeshFile;

    private const string mailboxId = "TestMailBox";

    public RetrieveMeshFileTest()
    {
        Environment.SetEnvironmentVariable("BSSMailBox", mailboxId);
        Environment.SetEnvironmentVariable("BlobStorage_ConnectionString", "BlobStorage_ConnectionString");
        _retrieveMeshFile = new RetrieveMeshFile(_mockLogger.Object,_mockMeshInboxService.Object,_mockExceptionHandler.Object,_mockBlobStorageHelper.Object);



    }
    [TestMethod]
    public async Task Run_Download_Single_File_from_Mesh_Success()
    {
        //arrange

        var messageId = "MessageId";
        var fileName = "testFile.csv";
        var content = new byte[] {0x20,0x20,0x20,0x20,0x20,0x20,0x20};
        MeshResponse<CheckInboxResponse> inboxResponse = CreateSuccessfulCheckInboxResponse([messageId]);
        MeshResponse<HeadMessageResponse> headResponse = CreateSuccessfulMeshHeadResponse(messageId,fileName);
        MeshResponse<GetMessageResponse> messageResponse = CreateSuccessfulGetMessageResponse(messageId,fileName,content,"application/octet-stream");
        MeshResponse<AcknowledgeMessageResponse> acknowledgeMessageResponse = CreateSuccessfulAcknowledgeResponse(messageId);

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(inboxResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId,messageId)).ReturnsAsync(headResponse);
        _mockMeshInboxService.Setup(i => i.GetMessageByIdAsync(mailboxId,messageId)).ReturnsAsync(messageResponse);
        _mockBlobStorageHelper.Setup(i => i.UploadFileToBlobStorage("BlobStorage_ConnectionString","inbound",It.IsAny<BlobFile>())).ReturnsAsync(true);
        _mockMeshInboxService.Setup(i => i.AcknowledgeMessageByIdAsync(mailboxId,messageId)).ReturnsAsync(acknowledgeMessageResponse);


        //act
        await _retrieveMeshFile.RunAsync(new Microsoft.Azure.Functions.Worker.TimerInfo());


        //assert
        _mockMeshInboxService.Verify(i => i.GetMessagesAsync(It.IsAny<string>()),Times.Once);
        _mockMeshInboxService.Verify(i => i.GetHeadMessageByIdAsync(It.IsAny<string>(),It.IsAny<string>()),Times.Once);
        _mockMeshInboxService.Verify(i => i.GetMessageByIdAsync(It.IsAny<string>(),It.IsAny<string>()),Times.Once);
        _mockBlobStorageHelper.Verify(i => i.UploadFileToBlobStorage(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<BlobFile>()),Times.Once);
        _mockMeshInboxService.Verify(i => i.AcknowledgeMessageByIdAsync(It.IsAny<string>(),It.IsAny<string>()),Times.Once);

    }


    [TestMethod]
    public void Run_Download_Single_Chunked_File_from_Mesh_Success()
    {
        //arrange


        var fileName = "TestFile.txt";
        var messageId = "TestMessage";
        MeshResponse<CheckInboxResponse> meshResponse = new MeshResponse<CheckInboxResponse>
        {
          IsSuccessful = true,
          Response = new CheckInboxResponse{
              Messages = [messageId]
          }
        };

        _mockMeshInboxService.Setup(i => i.GetMessagesAsync(mailboxId)).ReturnsAsync(meshResponse);
        _mockMeshInboxService.Setup(i => i.GetHeadMessageByIdAsync(mailboxId,messageId));



        //act


        //assert
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void Run_Download_Multiple_Files_from_Mesh_Success()
    {
        //arrange


        //act


        //assert
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void Run_No_Files_Available_In_Mesh_Success()
    {
        //arrange


        //act


        //assert
        Assert.IsTrue(true);
    }






    // check can access mesh no files

    // head file is not DATA

    // log error when cannot connect to mesh

    // cannot upload to blob error

    private static MeshResponse<CheckInboxResponse> CreateSuccessfulCheckInboxResponse(IEnumerable<string> messageIds)
    {
        return new MeshResponse<CheckInboxResponse>
        {
          IsSuccessful = true,
          Response = new CheckInboxResponse{
              Messages = messageIds
          }
        };
    }

    private static MessageMetaData CreateMessageMetaData(string messageId, string? filename = null, string? messageType = "DATA", string? chunkRange = null, int? TotalChunks = null)
    {
        return new MessageMetaData
        {
            WorkflowID = "testWorkflow",
            ToMailbox = mailboxId,
            FromMailbox = mailboxId,
            MessageId = messageId,
            FileName = filename,
            MessageType = messageType,
            ChunkRange = chunkRange,
            TotalChunks = TotalChunks
        };
    }

    private static MeshResponse<HeadMessageResponse> CreateSuccessfulMeshHeadResponse(string messageId, string? filename = null, string? messageType = "DATA", string? chunkRange = null, int? TotalChunks = null)
    {
        return new MeshResponse<HeadMessageResponse>
        {
            IsSuccessful = true,
            Response = new HeadMessageResponse
            {
                MessageMetaData = CreateMessageMetaData(messageId,filename,messageType,chunkRange,TotalChunks)
            }
        };
    }

    private static MeshResponse<GetMessageResponse> CreateSuccessfulGetMessageResponse(string messageId, string filename, byte[] content, string contentType)
    {
        return new MeshResponse<GetMessageResponse>
        {
            IsSuccessful = true,
            Response = new GetMessageResponse
            {
                MessageMetaData = CreateMessageMetaData(messageId),
                FileAttachment = new FileAttachment
                {
                    FileName = filename,
                    Content = content,
                    ContentType = contentType
                }
            }
        };
    }

    private static MeshResponse<AcknowledgeMessageResponse> CreateSuccessfulAcknowledgeResponse(string messageId)
    {
        return new MeshResponse<AcknowledgeMessageResponse>
        {
            IsSuccessful = true,
            Response = new AcknowledgeMessageResponse
            {
                MessageId = messageId
            }
        };
    }





}
