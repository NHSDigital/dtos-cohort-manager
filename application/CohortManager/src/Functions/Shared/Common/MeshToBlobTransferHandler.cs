namespace Common;

using Microsoft.Extensions.Logging;
using Model;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Helpers;
using NHS.MESH.Client.Models;

public class MeshToBlobTransferHandler : IMeshToBlobTransferHandler
{

    private readonly IMeshInboxService _meshInboxService;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly ILogger<MeshToBlobTransferHandler> _logger;

    private string _blobConnectionString;
    private string _mailboxId;
    private string _destinationContainer;

    public MeshToBlobTransferHandler(ILogger<MeshToBlobTransferHandler> logger, IBlobStorageHelper blobStorageHelper, IMeshInboxService meshInboxService)
    {
        _logger = logger;
        _meshInboxService = meshInboxService;
        _blobStorageHelper = blobStorageHelper;

    }

    public async Task<bool> MoveFilesFromMeshToBlob(Func<MessageMetaData,bool> predicate, string mailboxId, string blobConnectionString, string destinationContainer)
    {
        _blobConnectionString = blobConnectionString;
        _mailboxId = mailboxId;
        _destinationContainer = destinationContainer;

        int messageCount;
        do
        {

            var checkForMessages = await _meshInboxService.GetMessagesAsync(mailboxId);
            if(!checkForMessages.IsSuccessful)
            {
                _logger.LogCritical("Error while connecting getting Messages from MESH. ErrorCode: {ErrorCode}, ErrorDescription: {ErrorDescription}",checkForMessages.Error?.ErrorCode,checkForMessages.Error?.ErrorDescription);
                // Log Exception
                return false;
            }


            messageCount = checkForMessages.Response.Messages.Count();

            _logger.LogInformation("{messageCount} Messages were found within mailbox {mailboxId}",messageCount,mailboxId);

            if(messageCount == 0)
            {
                break;
            }

            var messagesMoved = await MoveAllMessagesToBlobStorage(checkForMessages.Response.Messages,predicate);

            _logger.LogInformation("{messagesMoved} out of {messageCount} Messages were moved mailbox: {mailboxId} to Blob Storage",messagesMoved,messageCount,mailboxId);

            if(messagesMoved == 0 && messageCount == 500)
            {
                _logger.LogCritical("Mailbox is full of messages that do not meet the predicate for transfer to Blob Storage");
                return false;
            }
        }
        while(messageCount == 500);

        return true;

    }

    private async Task<int> MoveAllMessagesToBlobStorage(IEnumerable<string> messages,Func<MessageMetaData,bool> predicate)
    {
        var messagesMovedToBlobStorage = 0;
        foreach(var message in messages)
        {
            var messageHead = await _meshInboxService.GetHeadMessageByIdAsync(_mailboxId,message);

            if(!messageHead.IsSuccessful)
            {
                _logger.LogCritical("Error while getting Message Head from MESH. ErrorCode: {ErrorCode}, ErrorDescription: {ErrorDescription}",messageHead.Error?.ErrorCode,messageHead.Error?.ErrorDescription);
                continue;
            }
            if(!predicate(messageHead.Response.MessageMetaData)){
                _logger.LogInformation("Message: {MessageId} was did not meet the predicate for transferring to BlobStoreage",messageHead.Response.MessageMetaData.MessageId);
                continue;
            }
            bool wasMessageDownloaded = await TransferMessageToBlobStorage(messageHead.Response.MessageMetaData);
            if(!wasMessageDownloaded)
            {
                _logger.LogCritical("Message: {MessageId} was not able to be transferred to BlobStorage",messageHead.Response.MessageMetaData.MessageId);
                continue;
            }
            var acknowledgeResponse = await _meshInboxService.AcknowledgeMessageByIdAsync(_mailboxId,messageHead.Response.MessageMetaData.MessageId);
            if(!acknowledgeResponse.IsSuccessful)
            {
                _logger.LogCritical("Message: {MessageId} was not able to be transferred to be acknowledged, Message will be removed from blob storage",messageHead.Response.MessageMetaData.MessageId);
            }
            messagesMovedToBlobStorage++;
        }
        return messagesMovedToBlobStorage;

    }

    private async Task<bool> TransferMessageToBlobStorage(MessageMetaData messageHead)
    {

        if(messageHead.MessageType != "DATA") {return false;}

        BlobFile? blobFile;

        if((messageHead.TotalChunks ?? 1) > 1)
        {
            blobFile = await DownloadChunkedFile(messageHead.MessageId);
        }
        else
        {
            blobFile = await DownloadFile(messageHead.MessageId);
        }

        if(blobFile == null)
        {
            return false;
        }

        var uploadedToBlob = await _blobStorageHelper.UploadFileToBlobStorage(_blobConnectionString,_destinationContainer,blobFile);

        return uploadedToBlob;
    }

    private async Task<BlobFile?> DownloadChunkedFile(string messageId)
    {
        var result = await _meshInboxService.GetChunkedMessageByIdAsync(_mailboxId,messageId);
        if(!result.IsSuccessful)
        {
            _logger.LogError("Failed to download chunked message from MESH MessageId: {messageId}",messageId);
            return null;
        }
        var meshFile = await FileHelpers.ReassembleChunkedFile(result.Response.FileAttachments);

        return new BlobFile(meshFile.Content,meshFile.FileName);
    }

    private async Task<BlobFile?> DownloadFile(string messageId)
    {
        var result = await _meshInboxService.GetMessageByIdAsync(_mailboxId,messageId);
        if(!result.IsSuccessful)
        {
            _logger.LogError("Failed to download chunked message from MESH MessageId: {messageId}",messageId);
            return null;
        }

        return new BlobFile(result.Response.FileAttachment.Content, result.Response.FileAttachment.FileName);
    }
}

