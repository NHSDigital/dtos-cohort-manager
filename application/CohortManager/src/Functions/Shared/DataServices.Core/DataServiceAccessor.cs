namespace DataServices.Core;

using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using DataServices.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DataServiceAccessor<TEntity> : IDataServiceAccessor<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly ILogger<DataServiceAccessor<TEntity>> _logger;
    public DataServiceAccessor(DataServicesContext context, ILogger<DataServiceAccessor<TEntity>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TEntity> GetSingle(Expression<Func<TEntity, bool>> predicate)
    {
        var result = await _context.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(predicate);
        return result;
    }

    public async Task<List<TEntity>> GetRange(Expression<Func<TEntity, bool>> predicates)
    {
        var result = await _context.Set<TEntity>().AsNoTracking().Where(predicates).ToListAsync();
        return result;
    }

    public async Task<bool> InsertSingle(TEntity entity)
    {
        await _context.AddAsync(entity);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> InsertMany(IEnumerable<TEntity> entities)
    {
        int rowsEffected = 0;
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(
            async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                await _context.AddRangeAsync(entities);
                rowsEffected = await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
        );

        return rowsEffected > 0;
    }

    public async Task<bool> Remove(Expression<Func<TEntity, bool>> predicate)
    {

        int rowsEffected = 0;
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(
            async () => {
                using var transaction = await _context.Database.BeginTransactionAsync();
                var result = await _context.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(predicate);

                if (result == null)
                {
                    return;
                }
                _context.Set<TEntity>().Remove(result);
                rowsEffected =  await _context.SaveChangesAsync();
                if(rowsEffected > 1)
                {
                    _logger.LogError("Multiple records ({RowsAffected}) were affected during delete operation. Rolling back transaction.", rowsEffected);
                    await transaction.RollbackAsync();
                    return;

                }

                await transaction.CommitAsync();
            }
        );

        return true;

    }

    public async Task<TEntity> Update(TEntity entity, Expression<Func<TEntity, bool>> predicate)
    {
        int rowsEffected = 0;
        var strategy = _context.Database.CreateExecutionStrategy();



        TEntity? dbEntity = await strategy.ExecuteAsync(
            async () => {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var existingEntity = await _context.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(predicate);

                if (existingEntity == null)
                {
                    return null;
                }
                _context.Update(entity);
                rowsEffected  = await _context.SaveChangesAsync();

                if(rowsEffected == 0)
                {
                    await transaction.RollbackAsync();
                    return existingEntity;
                }
                else if (rowsEffected > 1)
                {
                    await transaction.RollbackAsync();
                    return null;

                }
                await transaction.CommitAsync();
                return entity;

            }
        );

        if(rowsEffected == 0 && dbEntity == null)
        {
            _logger.LogWarning("Entity to be updated not found");
            return null;
        }
        else if(rowsEffected == 0 && dbEntity != null)
        {
            _logger.LogError("Records where found to be updated but the update failed");
            throw new MultipleRecordsFoundException("Records where found to be updated but the update failed");
        }
        else if(rowsEffected > 1)
        {
            _logger.LogError("Multiple Records were updated by PUT request, Changes have been Rolled-back");
            throw new MultipleRecordsFoundException("Multiple Records were updated by PUT request, Changes have been Rolled-back");
        }
        return dbEntity!;
    }

    public async Task<bool> Upsert(TEntity entity, Expression<Func<TEntity, bool>> predicate)
    {
        int rowsAffected = 0;
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(
            async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Check if entity exists
                    var existingEntity = await _context.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(predicate);

                    if (existingEntity == null)
                    {
                        // Insert new entity
                        await _context.AddAsync(entity);
                        _logger.LogInformation("Inserting new entity in Upsert operation");
                    }
                    else
                    {
                        // Update existing entity - preserve key fields
                        _logger.LogInformation("Updating existing entity in Upsert operation");

                        // Preserve ParticipantId (primary key) from existing record
                        var keyProperty = typeof(TEntity).GetProperty("ParticipantId");
                        if (keyProperty != null)
                        {
                            var existingKey = keyProperty.GetValue(existingEntity);
                            keyProperty.SetValue(entity, existingKey);
                            _logger.LogDebug("Preserved ParticipantId: {ParticipantId}", existingKey);
                        }

                        // Preserve RecordInsertDateTime from existing record to maintain audit trail
                        var insertDateProperty = typeof(TEntity).GetProperty("RecordInsertDateTime");
                        if (insertDateProperty != null)
                        {
                            var existingInsertDate = insertDateProperty.GetValue(existingEntity);
                            if (existingInsertDate != null)
                            {
                                insertDateProperty.SetValue(entity, existingInsertDate);
                                _logger.LogDebug("Preserved RecordInsertDateTime: {RecordInsertDateTime}", existingInsertDate);
                            }
                        }

                        _context.Update(entity);
                    }

                    rowsAffected = await _context.SaveChangesAsync();

                    if (rowsAffected == 0)
                    {
                        _logger.LogWarning("Upsert resulted in 0 rows affected");
                        await transaction.RollbackAsync();
                        return;
                    }
                    else if (rowsAffected > 1)
                    {
                        _logger.LogError("Multiple records ({RowsAffected}) were affected during upsert operation. Rolling back transaction.", rowsAffected);
                        await transaction.RollbackAsync();
                        return;
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Upsert operation completed successfully. Rows affected: {RowsAffected}", rowsAffected);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during upsert operation");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        );

        return rowsAffected > 0;
    }

}


