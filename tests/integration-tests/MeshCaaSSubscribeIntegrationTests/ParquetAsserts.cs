namespace MeshCaaSSubscribeIntegrationTests;

using System.IO;
using NHS.MESH.Client.Models;
using ParquetSharp;
using ParquetSharp.IO;
public static class ParquetAsserts
{
    public static void ContainsExpectedNhsNumber(byte[] parquetBytes, string expectedNhsNumber)
    {
        using var stream = new MemoryStream(parquetBytes);
        using var reader = new ManagedRandomAccessFile(stream);
        using var file = new ParquetFileReader(reader);

        var rowGroupCount = file.FileMetaData.NumRowGroups;
        Assert.AreEqual(1, rowGroupCount, "Expected exactly 1 row group.");

        using var rowGroup = file.RowGroup(0);

        var columnCount = rowGroup.MetaData.NumColumns;
        Assert.AreEqual(1, columnCount, "Expected exactly 1 column.");

        using var columnReader = rowGroup.Column(0).LogicalReader<string>();
        var values = columnReader.ReadAll(checked((int)rowGroup.MetaData.NumRows));

        Assert.AreEqual(1, values.Length, "Expected exactly 1 row.");
        Assert.AreEqual(expectedNhsNumber, values[0], "NHS number does not match.");
    }
}
