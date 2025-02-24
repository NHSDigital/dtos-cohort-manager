namespace DataServices.Migrations;

using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Apache.Arrow;
using Azure.Messaging.EventGrid.SystemEvents;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Polly;

public class SeedDataLoader : ISeedDataLoader
{

    private readonly DataServicesContext _context;
    private readonly ILogger<SeedDataLoader> _logger;

    public SeedDataLoader(ILogger<SeedDataLoader> logger,DataServicesContext context)
    {
        _logger = logger;
        _context = context;
    }
    public async Task<bool> LoadData<TEntity>(string filePath, string tableName, bool hasIdentityColumn = true) where TEntity : class
    {

        if (string.IsNullOrWhiteSpace(tableName) || !IsValidTableName(tableName))
        {
            _logger.LogError("Invalid table name: {TableName}", tableName);
            throw new ArgumentException("Invalid table name.", nameof(tableName));
        }


        if(_context.Set<TEntity>().Any())
        {
            _logger.LogInformation("Data Already In Table Skipping Seed Data");
            return true;
        }

        var jsonData = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<TEntity>>(jsonData);

        if (data == null || !data.Any())
        {
            return true;
        }
        _logger.LogInformation("Inserting {Record} Records in to table {EntityName}",data.Count,typeof(TEntity).FullName);
        var transaction = await _context.Database.BeginTransactionAsync();
        if(hasIdentityColumn)
        {
            await _context.Database.ExecuteSqlRawAsync(string.Format("SET IDENTITY_INSERT DBO.{0} ON;",tableName));
        }

        await _context.AddRangeAsync(data);
        await _context.SaveChangesAsync();

        if(hasIdentityColumn)
        {
            await _context.Database.ExecuteSqlRawAsync(string.Format("SET IDENTITY_INSERT DBO.{0} OFF;",tableName));
        }
        await transaction.CommitAsync();
        return true;
    }

    private bool IsValidTableName(string tableName)
    {
        return Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$");
    }
}
