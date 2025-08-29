namespace NHS.CohortManager.DemographicServices;

using System.Linq.Expressions;
using DataServices.Core;
using Model;

public class CaasNemsSubscriptionAccessor : IDataServiceAccessor<NemsSubscription>
{
    private readonly IDataServiceAccessor<NemsSubscription> _inner;
    public CaasNemsSubscriptionAccessor(IDataServiceAccessor<NemsSubscription> inner)
    {
        _inner = inner;
    }

    public Task<NemsSubscription> GetSingle(Expression<Func<NemsSubscription, bool>> predicate)
        => _inner.GetSingle(predicate);

    public Task<List<NemsSubscription>> GetRange(Expression<Func<NemsSubscription, bool>> predicates)
        => _inner.GetRange(predicates);

    public Task<bool> Remove(Expression<Func<NemsSubscription, bool>> predicate)
        => _inner.Remove(predicate);

    public async Task<bool> InsertSingle(NemsSubscription entity)
    {
        if (entity.SubscriptionSource == null)
        {
            entity.SubscriptionSource = SubscriptionSource.MESH;
        }
        return await _inner.InsertSingle(entity);
    }

    public async Task<bool> InsertMany(IEnumerable<NemsSubscription> entities)
    {
        foreach (var e in entities)
        {
            if (e.SubscriptionSource == null)
            {
                e.SubscriptionSource = SubscriptionSource.MESH;
            }
        }
        return await _inner.InsertMany(entities);
    }

    public async Task<NemsSubscription> Update(NemsSubscription entity, Expression<Func<NemsSubscription, bool>> predicate)
    {
        if (entity.SubscriptionSource == null)
        {
            entity.SubscriptionSource = SubscriptionSource.MESH;
        }
        return await _inner.Update(entity, predicate);
    }
}

