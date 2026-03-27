namespace Common;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Settings for sending CAAS subscribe messages over MESH.
/// </summary>
public class MeshSendCaasSubscribeConfig
{

    /// <summary>The workflow identifier to apply to outbound CAAS messages.</summary>
    [Required]
    public required string SendCaasWorkflowId { get; set; }
}
