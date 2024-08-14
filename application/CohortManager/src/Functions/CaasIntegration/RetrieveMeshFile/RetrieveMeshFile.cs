namespace NHS.Screening.RetriveMeshFile;

using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Clients;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;

public class RetrieveMeshFile
{
    private readonly ILogger _logger;

    private readonly IMeshInboxService _meshInboxService;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly string _mailboxId;

    public RetrieveMeshFile(ILogger<RetrieveMeshFile> logger, IMeshInboxService meshInboxService, IExceptionHandler exceptionHandler)
    {
        _logger = logger;
        _meshInboxService = meshInboxService;
        _exceptionHandler = exceptionHandler;

        _mailboxId = Environment.GetEnvironmentVariable("BSSMailBox");
    }

    [Function("RetriveMeshFile")]
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
            bool wasMessageDownloaded = await DownloadMessageToBlob(messageHead.Response.MessageMetaData);
            var acknowledgeResponse = await _meshInboxService.AcknowledgeMessageByIdAsync(_mailboxId,messageHead.Response.MessageMetaData.MessageId);
        }




        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }

    private async Task<bool> DownloadMessageToBlob(MessageMetaData messageHead)
    {

        if(messageHead.MessageType != "DATA") {return false;}
        //TODO implement.
        await Task.CompletedTask;
        return true;
    }

}
