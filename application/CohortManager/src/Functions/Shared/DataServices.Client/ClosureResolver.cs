namespace DataServices.Client;

using System.Linq.Expressions;
using System.Reflection;

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
                return Expression.Constant(constVal);
            }
            else if (node.Member is PropertyInfo prop)
            {
                var constVal = prop.GetValue(constExpr.Value);
                return Expression.Constant(constVal);
            }
        }

        return base.VisitMember(node);
    }
}
