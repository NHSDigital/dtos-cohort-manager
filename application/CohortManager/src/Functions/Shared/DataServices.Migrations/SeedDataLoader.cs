namespace DataServices.Migrations;

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;

public static class SeedDataLoader
{
    public static async Task LoadData<TEntity>(DbContext context, string filePath) where TEntity : class
    {
        var jsonData = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<TEntity>>(jsonData);

        if (data == null || !data.Any())
            throw new InvalidOperationException($"No data found in the SeedData file. {filePath}");

        await context.AddRangeAsync(data);
    }
}
