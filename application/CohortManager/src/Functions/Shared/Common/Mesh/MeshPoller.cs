namespace Common;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using NHS.MESH.Client.Contracts.Services;

public class MeshPoller : IMeshPoller
{
    private readonly ILogger<MeshPoller> _logger;
    private readonly IMeshOperationService _meshOperationService;
    private readonly IBlobStorageHelper _blobStorageHelper;

    public MeshPoller(ILogger<MeshPoller> logger, IMeshOperationService meshOperationService)
    {
        _logger = logger;
        _meshOperationService = meshOperationService;
    }

    public async Task<bool> ShouldExecuteHandshake(string mailboxId, string configFileName)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("ShouldExecuteHandshake is not yet implemented");
    }

    public async Task<bool> ExecuteHandshake(string mailboxId)
    {
        var meshValidationResponse = await _meshOperationService.MeshHandshakeAsync(mailboxId);

        if (!meshValidationResponse.IsSuccessful)
        {
            _logger.LogError("Error While handshaking with MESH. ErrorCode: {ErrorCode}, ErrorDescription: {ErrorDescription}", meshValidationResponse.Error?.ErrorCode, meshValidationResponse.Error?.ErrorDescription);
            return false;
        }

        return true;
    }

}



