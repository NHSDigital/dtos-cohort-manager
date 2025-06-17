namespace DataServices.Migrations;

public interface ISeedDataLoader
{
    Task<bool> LoadData<TEntity>(string filePath, string tableName, bool hasIdentityColumn = true) where TEntity : class;
}
