namespace Common;

/// <summary>
/// Settings for sending CAAS subscribe messages over MESH.
/// </summary>
public class MeshSendCaasSubscribeConfig
{
    /// <summary>The workflow identifier to apply to outbound CAAS messages.</summary>
    public required string SendCaasWorkflowId { get; set; }
}
