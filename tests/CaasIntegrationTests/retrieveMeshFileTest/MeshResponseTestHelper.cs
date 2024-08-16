namespace NHS.CohortManager.Tests.CaasIntegrationTests;

using NHS.MESH.Client.Helpers.ContentHelpers;
using NHS.MESH.Client.Models;

public static class MeshResponseTestHelper
{
    public static MeshResponse<CheckInboxResponse> CreateSuccessfulCheckInboxResponse(IEnumerable<string> messageIds)
    {
        return new MeshResponse<CheckInboxResponse>
        {
            IsSuccessful = true,
            Response = new CheckInboxResponse{
                Messages = messageIds
            }
        };
    }

    public static MessageMetaData CreateMessageMetaData(string mailboxId,string messageId, string? filename = null, string? messageType = "DATA", string? chunkRange = null, int? TotalChunks = null)
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

    public static MeshResponse<HeadMessageResponse> CreateSuccessfulMeshHeadResponse(string mailboxId,string messageId, string? filename = null, string? messageType = "DATA", string? chunkRange = null, int? TotalChunks = null)
    {
        return new MeshResponse<HeadMessageResponse>
        {
            IsSuccessful = true,
            Response = new HeadMessageResponse
            {
                MessageMetaData = CreateMessageMetaData(mailboxId,messageId,filename,messageType,chunkRange,TotalChunks)
            }
        };
    }

    public static MeshResponse<GetMessageResponse> CreateSuccessfulGetMessageResponse(string mailboxId, string messageId, string filename, byte[] content, string contentType)
    {
        return new MeshResponse<GetMessageResponse>
        {
            IsSuccessful = true,
            Response = new GetMessageResponse
            {
                MessageMetaData = CreateMessageMetaData(mailboxId,messageId),
                FileAttachment = new FileAttachment
                {
                    FileName = filename,
                    Content = content,
                    ContentType = contentType,
                }
            }
        };
    }

    public static MeshResponse<GetChunkedMessageResponse> CreateSuccessfulGetChunkedMessageResponse(string mailboxId ,string messageId,string fileName, string contentType, List<byte[]> chunks)
    {
        return new MeshResponse<GetChunkedMessageResponse>
        {
            IsSuccessful = true,
            Response = new GetChunkedMessageResponse
            {
                MessageMetaData = CreateMessageMetaData(mailboxId,messageId),
                FileAttachments = chunks.Select((file,index) => new FileAttachment
                    {
                        FileName =fileName,
                        ContentType = contentType,
                        Content = GZIPHelpers.CompressBuffer(file),
                        ChunkNumber = index+1,
                    }).ToList()
            }
        };
    }

    public static MeshResponse<AcknowledgeMessageResponse> CreateSuccessfulAcknowledgeResponse(string messageId)
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
