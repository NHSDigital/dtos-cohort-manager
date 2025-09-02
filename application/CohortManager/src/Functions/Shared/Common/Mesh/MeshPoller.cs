namespace Common;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Model;
using NHS.MESH.Client.Contracts.Services;

/// <summary>
/// Coordinates periodic MESH mailbox operations such as handshake validation.
/// </summary>
public class MeshPoller : IMeshPoller
{
    private readonly ILogger<MeshPoller> _logger;
    private readonly IMeshOperationService _meshOperationService;

    public MeshPoller(ILogger<MeshPoller> logger, IMeshOperationService meshOperationService)
    {
        _logger = logger;
        _meshOperationService = meshOperationService;
    }

    /// <summary>
    /// Determines if a handshake should be executed based on persisted state.
    /// </summary>
    /// <param name="mailboxId">The mailbox to validate.</param>
    /// <param name="configFileName">A state/configuration file identifier.</param>
    /// <returns>True if a handshake should run; otherwise false.</returns>
    public async Task<bool> ShouldExecuteHandshake(string mailboxId, string configFileName)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("ShouldExecuteHandshake is not yet implemented");
    }

    /// <summary>
    /// Executes a MESH handshake for the provided mailbox.
    /// </summary>
    /// <param name="mailboxId">The mailbox to validate.</param>
    /// <returns>True when successful; otherwise false.</returns>
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


