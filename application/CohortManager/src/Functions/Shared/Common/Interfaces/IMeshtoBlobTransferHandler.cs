namespace Common;

using NHS.MESH.Client.Models;

public interface IMeshToBlobTransferHandler
{
    Task<bool> MoveFilesFromMeshToBlob(Func<MessageMetaData,bool> predicate, string mailboxId, string blobConnectionString, string destinationContainer);
}
