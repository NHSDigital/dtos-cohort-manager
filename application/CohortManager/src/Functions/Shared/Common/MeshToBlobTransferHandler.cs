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

            _logger.LogDebug("{messageCount} Messages were found within mailbox {mailboxId}",messageCount,mailboxId);

            await MoveAllMessagesToBlobStorage(checkForMessages.Response.Messages,predicate);

        }
        while(messageCount == 500);

        return true;

    }

    private async Task<bool> MoveAllMessagesToBlobStorage(IEnumerable<string> messages,Func<MessageMetaData,bool> predicate)
    {

        foreach(var message in messages)
        {
            var messageHead = await _meshInboxService.GetHeadMessageByIdAsync(_mailboxId,message);

            if(!messageHead.IsSuccessful)
            {
                _logger.LogCritical("Error while getting Message Head from MESH. ErrorCode: {ErrorCode}, ErrorDescription: {ErrorDescription}",messageHead.Error?.ErrorCode,messageHead.Error?.ErrorDescription);
                continue;
            }
            if(!predicate(messageHead.Response.MessageMetaData)){
                continue;
            }
            bool wasMessageDownloaded = await TransferMessageToBlobStorage(messageHead.Response.MessageMetaData);
            if(wasMessageDownloaded)
            {
                var acknowledgeResponse = await _meshInboxService.AcknowledgeMessageByIdAsync(_mailboxId,messageHead.Response.MessageMetaData.MessageId);
            }
        }
        return true;

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
            _logger.LogError("Failed to download chunked message from MESH MessageId: {}");
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
            _logger.LogError("Failed to download chunked message from MESH MessageId: {}");
            return null;
        }

        return new BlobFile(result.Response.FileAttachment.Content, result.Response.FileAttachment.FileName);
    }
}

