namespace Common;

using NHS.MESH.Client.Models;

public interface IMeshToBlobTransferHandler
{
    Task<bool> MoveFilesFromMeshToBlob(Func<MessageMetaData,bool> predicate, Func<MessageMetaData,string> fileNameFunction, string mailboxId, Uri blobServiceUri, string destinationContainer, bool executeHandshake = false);
}
