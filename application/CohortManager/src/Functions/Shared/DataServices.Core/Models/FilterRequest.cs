namespace DataServices.Core;

using System.Linq.Expressions;

public class FilterRequest<TEntity> where TEntity : class
{
    public Expression<Func<TEntity,bool>> Where {get;set;}
    public List<string> Includes {get;set;}
    public int Limit {get;set;}
    public bool Single {get;set;}
    public string OrderBy {get;set;}

}
