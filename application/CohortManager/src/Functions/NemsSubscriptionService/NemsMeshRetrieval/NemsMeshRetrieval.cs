namespace NHS.Screening.NemsMeshRetrieval;

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using NHS.MESH.Client.Models;


public class NemsMeshRetrieval
{
    private readonly ILogger<NemsMeshRetrieval> _logger;

    private readonly IMeshToBlobTransferHandler _meshToBlobTransferHandler;
    private readonly string _mailboxId;
    private readonly string _blobConnectionString;
    private readonly IBlobStorageHelper _blobStorageHelper;
    private readonly NemsMeshRetrievalConfig _config;
    private const string NextHandShakeTimeConfigKey = "NextHandShakeTime";
    private const string ConfigFileName = "MeshState.json";

    public NemsMeshRetrieval(ILogger<NemsMeshRetrieval> logger, IMeshToBlobTransferHandler meshToBlobTransferHandler, IBlobStorageHelper blobStorageHelper, IOptions<NemsMeshRetrievalConfig> options)
    {
        _logger = logger;
        _meshToBlobTransferHandler = meshToBlobTransferHandler;
        _blobStorageHelper = blobStorageHelper;
        _mailboxId = options.Value.NemsMeshMailBox;
        _config = options.Value;
        _blobConnectionString = _config.nemsmeshfolder_STORAGE;
    }
    /// <summary>
    /// This function polls the MESH Mailbox every 5 minutes, if there is a file posted to the mailbox.
    /// If there is a file in there will move the file to the Cohort Manager Blob Storage where it will be picked up by the ReceiveCaasFile Function.
    /// </summary>
    [Function("RetrieveNemsMeshFile")]
    public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: ,{Datetime}", DateTime.UtcNow);

        static bool messageFilter(MessageMetaData i) => true; // No current filter defined there might be business rules here

        static string fileNameFunction(MessageMetaData i) => string.Concat(i.MessageId, "_-_", i.WorkflowID, ".xml");

        try
        {
            var shouldExecuteHandShake = await ShouldExecuteHandShake();
            var result = await _meshToBlobTransferHandler.MoveFilesFromMeshToBlob(messageFilter, fileNameFunction, _mailboxId, _blobConnectionString, _config.NemsMeshInboundContainer, shouldExecuteHandShake);

            if (!result)
            {
                _logger.LogError("An error was encountered while moving files from Mesh to Blob");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error encountered while moving files from Mesh to Blob");
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {ScheduleStatus}", myTimer.ScheduleStatus.Next);
        }
    }

    private async Task<bool> ShouldExecuteHandShake()
    {

        Dictionary<string, string> configValues;
        TimeSpan handShakeInterval = new TimeSpan(0, 23, 54, 0);
        var meshState = await _blobStorageHelper.GetFileFromBlobStorage(_blobConnectionString, _config.NemsMeshConfigContainer, ConfigFileName);
        if (meshState == null)
        {

            _logger.LogInformation("MeshState File did not exist, Creating new MeshState File in blob Storage");
            configValues = new Dictionary<string, string>
            {
                { NextHandShakeTimeConfigKey, DateTime.UtcNow.Add(handShakeInterval).ToString() }
            };
            await SetConfigState(configValues);

            return true;

        }
        using (StreamReader reader = new StreamReader(meshState.Data))
        {
            meshState.Data.Seek(0, SeekOrigin.Begin);
            string jsonData = await reader.ReadToEndAsync();
            configValues = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
        }

        string nextHandShakeDateString;
        //config value doenst exist
        if (!configValues.TryGetValue(NextHandShakeTimeConfigKey, out nextHandShakeDateString))
        {
            _logger.LogInformation("NextHandShakeTime config item does not exist, creating new config item");
            configValues.Add(NextHandShakeTimeConfigKey, DateTime.UtcNow.Add(handShakeInterval).ToString());
            await SetConfigState(configValues);
            return true;


        }
        DateTime nextHandShakeDateTime;
        //date cannot be parsed
        if (!DateTime.TryParse(nextHandShakeDateString, CultureInfo.InvariantCulture, out nextHandShakeDateTime))
        {
            _logger.LogInformation("Unable to Parse NextHandShakeTime, Updating config value");
            configValues[NextHandShakeTimeConfigKey] = DateTime.UtcNow.Add(handShakeInterval).ToString();
            SetConfigState(configValues);
            return true;
        }

        if (DateTime.Compare(nextHandShakeDateTime, DateTime.UtcNow) <= 0)
        {
            _logger.LogInformation("Next HandShakeTime was in the past, will execute handshake");
            var NextHandShakeTimeConfig = DateTime.UtcNow.Add(handShakeInterval).ToString();

            configValues[NextHandShakeTimeConfigKey] = NextHandShakeTimeConfig;
            _logger.LogInformation("Next Handshake scheduled for {NextHandShakeTimeConfig}", NextHandShakeTimeConfig);

            return true;

        }
        _logger.LogInformation("Next handshake scheduled for {NextHandShakeDateTime}", nextHandShakeDateTime);
        return false;
    }


    private async Task<bool> SetConfigState(Dictionary<string, string> state)
    {
        try
        {
            string jsonString = JsonSerializer.Serialize(state);
            using (var stream = GenerateStreamFromString(jsonString))
            {
                var blobFile = new BlobFile(stream, ConfigFileName);
                var result = await _blobStorageHelper.UploadFileToBlobStorage(_blobConnectionString, _config.NemsMeshConfigContainer, blobFile, true);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable To set Config State");
            return false;
        }
    }

    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
