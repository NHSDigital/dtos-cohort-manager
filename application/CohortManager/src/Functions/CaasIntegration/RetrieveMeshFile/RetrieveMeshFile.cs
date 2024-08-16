namespace NHS.Screening.RetrieveMeshFile;

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Models;


public class RetrieveMeshFile
{
    private readonly ILogger _logger;

    private readonly IMeshToBlobTransferHandler _meshToBlobTransferHandler;
    private readonly string _mailboxId;
    private readonly string _blobConnectionString;

    public RetrieveMeshFile(ILogger<RetrieveMeshFile> logger, IMeshToBlobTransferHandler meshToBlobTransferHandler)
    {
        _logger = logger;
        _meshToBlobTransferHandler = meshToBlobTransferHandler;

        _mailboxId = Environment.GetEnvironmentVariable("BSSMailBox");
        _blobConnectionString =  Environment.GetEnvironmentVariable("BlobStorage_ConnectionString");
    }

    [Function("RetrieveMeshFile")]
    public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        Func<MessageMetaData,bool> messageFilter = i => true; // No current filter defined there might be business rules here

        try
        {
            var result = await _meshToBlobTransferHandler.MoveFilesFromMeshToBlob(messageFilter,_mailboxId,_blobConnectionString,"inbound");

            if(!result)
            {
                _logger.LogError("An error was encountered while moving files from Mesh to Blob");
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"An error encountered while moving files from Mesh to Blob");
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }
}
