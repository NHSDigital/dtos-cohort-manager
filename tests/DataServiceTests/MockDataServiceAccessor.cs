using System.Linq.Expressions;

using DataServices.Core;

public class MockDataServiceAccessor<TEntity> : IDataServiceAccessor<TEntity> where TEntity : class
{
    private readonly List<TEntity> _data;
    public MockDataServiceAccessor(List<TEntity> data)
    {
        _data = data;
    }
    public async Task<List<TEntity>> GetRange(Expression<Func<TEntity, bool>> predicates)
    {
        await Task.CompletedTask;
        return _data.Where(predicates.Compile()).ToList();
    }

    public async Task<TEntity> GetSingle(Expression<Func<TEntity, bool>> predicate)
    {
        await Task.CompletedTask;
        return _data.SingleOrDefault(predicate.Compile());
    }

    public async Task<bool> InsertSingle(TEntity entity)
    {
        await Task.CompletedTask;
        var index = _data.FindIndex(i => i == entity);
        if(index == -1)
        {
            _data.Add(entity);
            return true;
        }
        return false;
    }

    public async Task<bool> Remove(Expression<Func<TEntity, bool>> predicate)
    {
        var item = await this.GetSingle(predicate);
        if(item != null)
        {
            _data.Remove(item);
            return true;
        }
        return false;
    }

    public async Task<TEntity> Update(TEntity entity, Expression<Func<TEntity, bool>> predicate)
    {
        TEntity item = await this.GetSingle(predicate);

        if(item != null)
        {
            var index = _data.FindIndex(i => i == item);
            _data[index] = entity;
            return _data[index];
        }
        return null;
    }
}
