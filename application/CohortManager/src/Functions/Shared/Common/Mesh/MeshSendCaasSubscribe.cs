namespace Common;

using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ParquetSharp;

public class MeshSendCaasSubscribe
{
    private ILogger<MeshSendCaasSubscribe> _logger;
    private IMeshOutboxService _meshOutboxService;
    public MeshSendCaasSubscribe(ILogger<MeshSendCaasSubscribe> logger, IMeshOutboxService meshOutboxService)
    {
        _logger = logger;
        _meshOutboxService = meshOutboxService;
    }

    public string SendSubscriptionRequest(long nhsNumber)
    {


        _meshOutboxService.SendCompressedMessageAsync()
    }

    private byte[] CreateParquetFile(long nhsNumber)
    {
        var columns = new Column[]
        {
            new Column<long>("NhsNumber"),
        };
                using var file = new ParquetFileWriter("float_timeseries.parquet", columns);
        using var rowGroup = file.AppendRowGroup();

        using (var nhsnumber = rowGroup.NextColumn().LogicalWriter<long>())
        {
            nhsnumber.WriteBatch()
            timestampWriter.WriteBatch(timestamps);
        }


    }
}
