namespace Common;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using ParquetSharp;
using ParquetSharp.IO;

public class MeshSendCaasSubscribe : IMeshSendCaasSubscribe
{
    private ILogger<MeshSendCaasSubscribe> _logger;
    private IMeshOutboxService _meshOutboxService;
    private MeshSendCaasSubscribeConfig _config;
    public MeshSendCaasSubscribe(ILogger<MeshSendCaasSubscribe> logger, IMeshOutboxService meshOutboxService, IOptions<MeshSendCaasSubscribeConfig> config)
    {
        _logger = logger;
        _meshOutboxService = meshOutboxService;
        _config = config.Value;
    }

    public async Task<string> SendSubscriptionRequest(long nhsNumber, string toMailbox, string fromMailbox)
    {

        var content = CreateParquetFile(nhsNumber);

        FileAttachment file = new FileAttachment
        {
            FileName = "CaaSSubscribe.parquet",
            Content = content,
            ContentType = "application/octet-stream"
        };

        await File.WriteAllBytesAsync("Testpremesh.parquet", content);

        var result = await _meshOutboxService.SendCompressedMessageAsync(fromMailbox, toMailbox, _config.SendCaasWorkflowId, file);
        if (!result.IsSuccessful)
        {
            _logger.LogError("Could't send mesh message Error Code: {ErrorCode}, Error Description: {ErrorDescription}, ", result.Error.ErrorCode, result.Error.ErrorDescription);
            return null;
        }

        return result.Response.MessageId;
    }

    private static byte[] CreateParquetFile(long nhsNumber)
    {
        var columns = new Column[]
        {
            new Column<long>("nhs_number"),
        };
        long[] nhsNumberList = { nhsNumber };

        using var stream = new MemoryStream();
        using var writer = new ManagedOutputStream(stream);
        using (var file = new ParquetFileWriter(writer, columns))
        {
            using var rowGroup = file.AppendRowGroup();
            using (var nhsNumberColumn = rowGroup.NextColumn().LogicalWriter<long>())
            {
                nhsNumberColumn.WriteBatch(nhsNumberList);
            }
        }

        return stream.ToArray();

    }
}
