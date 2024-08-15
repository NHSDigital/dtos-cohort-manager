namespace NHS.Screening.RetriveMeshFile;

using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Common;
using Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Clients;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using NHS.MESH.Client.Helpers;

public class RetrieveMeshFile
{
    private readonly ILogger _logger;

    private readonly IMeshInboxService _meshInboxService;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly string _mailboxId;
    private readonly string _blobConnectionString;

    public RetrieveMeshFile(ILogger<RetrieveMeshFile> logger, IMeshInboxService meshInboxService, IExceptionHandler exceptionHandler, IBlobStorageHelper blobStorageHelper)
    {
        _logger = logger;
        _meshInboxService = meshInboxService;
        _exceptionHandler = exceptionHandler;
        _blobStorageHelper = blobStorageHelper;

        _mailboxId = Environment.GetEnvironmentVariable("BSSMailBox");
        _blobConnectionString =  Environment.GetEnvironmentVariable("BlobStorage_ConnectionString");
    }

    [Function("RetrieveMeshFile")]
    public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");



        var checkForMessages = await _meshInboxService.GetMessagesAsync(_mailboxId);
        if(!checkForMessages.IsSuccessful)
        {
            _logger.LogCritical("Error while connecting getting Messages from MESH. ErrorCode: {ErrorCode}, ErrorDescription: {ErrorDescription}",checkForMessages.Error?.ErrorCode,checkForMessages.Error?.ErrorDescription);
            // Log Exception
            return;
        }

        var messageCount = checkForMessages.Response.Messages.Count();

        _logger.LogDebug("{messageCount} Messages were found within mailbox {mailboxId}",messageCount,_mailboxId);




        foreach(var message in checkForMessages.Response.Messages)
        {
            var messageHead = await _meshInboxService.GetHeadMessageByIdAsync(_mailboxId,message);
            if(!messageHead.IsSuccessful){
                _logger.LogCritical("Error while getting Message Head from MESH. ErrorCode: {ErrorCode}, ErrorDescription: {ErrorDescription}",checkForMessages.Error?.ErrorCode,checkForMessages.Error?.ErrorDescription);
            }
            bool wasMessageDownloaded = await TransferMessageToBlobStorage(messageHead.Response.MessageMetaData);
            var acknowledgeResponse = await _meshInboxService.AcknowledgeMessageByIdAsync(_mailboxId,messageHead.Response.MessageMetaData.MessageId);
        }




        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
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

        await _blobStorageHelper.UploadFileToBlobStorage(_blobConnectionString,"inbound" ,blobFile);

        return true;
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
