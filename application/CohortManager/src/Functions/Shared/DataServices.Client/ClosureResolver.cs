namespace DataServices.Client;

using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

public class ClosureResolver : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Arguments.Count == 0)
        {
            var objExpr = Visit(node.Object);

            if (objExpr is ConstantExpression objConstExpr)
            {
                var res = node.Method.Invoke(objConstExpr.Value, new object[0]);
                return Expression.Constant(res);
            }
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var childExpr = Visit(node.Expression);

        if (childExpr is ConstantExpression constExpr)
        {
            if (node.Member is FieldInfo field)
            {
                var constVal = field.GetValue(constExpr.Value);
                return VisitConstant(Expression.Constant(constVal));
            }
            else if (node.Member is PropertyInfo prop)
            {
                var constVal = prop.GetValue(constExpr.Value);
                return VisitConstant(Expression.Constant(constVal));
            }
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Type == typeof(Guid) && node.Value is Guid guidValue)
        {
            /* 
            * Forces guid to be wrapped in quotes and and into the type Guid. 
            * This is because the data service wants the guid to be enclosed in quotes. 
            * This allows  deserialization into a correctly formatted expression 
            */
            return Expression.Parameter(typeof(Guid), $"\"{guidValue}\"");
        }
        else if (node.Type == typeof(DateTime) && node.Value is DateTime dateTimeValue)
        {
            return Expression.Parameter(typeof(DateTime), $"\"{dateTimeValue}\"");
        }
        return base.VisitConstant(node);
    }

}
