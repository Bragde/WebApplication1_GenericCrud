using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using WebApplication1.QueryFilter.Extensions;

namespace WebApplication1.QueryFilter.Utils;

internal static class IncludedProperties
{
    /// <summary>
    /// Ensure that the given value only contains the properties that are included in the given query, and that no cyclic references are present.
    /// </summary>
#if NET7_0_OR_GREATER
    [return: NotNullIfNotNull(nameof(value))]
#else
    [return: NotNullIfNotNull("value")]
#endif
    public static T? Clean<T>(T value, List<string> paths) where T : class
    {
        // If the value is null, we don't need to do anything
        if (value is null)
        {
            return value;
        }

        // Set up a queue of items to process, and their paths relative to the root
        var pending = new Queue<(object? item, string path)>();

        // Also keep track of the items we have already processed, so that we don't process them twice
        var processed = new HashSet<object>();

        // If the value is a collection, we need to process each item individually
        if (value is IEnumerable list)
        {
            foreach (var entry in list)
            {
                pending.Enqueue((entry, ""));
            }
        }
        else
        {
            pending.Enqueue((value, ""));
        }

        while (pending.TryDequeue(out var next))
        {
            var (item, path) = next;

            // If the item is null, or we have already processed it, we don't need to do anything
            if (item is null || !processed.Add(item))
            {
                continue;
            }

            foreach (var property in item.GetType().GetProperties())
            {
                // The property is not interesting if it is a value type, a string, a byte array, or if it is not readable or writable
                if (property.PropertyType == typeof(string) || property.PropertyType == typeof(byte[]) || property.PropertyType.IsValueType || !property.CanWrite || !property.CanRead || property.IsSpecialName)
                {
                    continue;
                }

                // The path is set to the path of its parent, plus the property name
                var propertyPath = $"{path}.{property.Name}";

                // If the property is not included in the query, we set it to null and continue
                if (!paths.Contains(propertyPath))
                {
                    property.SetValue(item, null);
                    continue;
                }

                // Otherwise, we need to process the property value
                var propertyValue = property.GetValue(item);

                // If the property value is null, we don't need to do anything
                if (propertyValue is null)
                {
                    continue;
                }

                // If the property value is a collection, we need to process each item individually
                if (propertyValue is IEnumerable listValue)
                {
                    foreach (var entry in listValue)
                    {
                        pending.Enqueue((entry, propertyPath));
                    }
                }
                else
                {
                    pending.Enqueue((propertyValue, propertyPath));
                }
            }
        }

        return value;
    }

    /// <summary>
    /// Exctracts the included properties from the given query, where multiple levels of inclusion are separated by dots.
    /// </summary>
    public static List<string> Parse(IQueryable query)
    {
        var result = new List<string>();

        // Is this the root query? If so, we don't need to do anything
        if (query.Expression.NodeType == ExpressionType.Extension)
        {
            return result;
        }

        // Set up a queue to do a breadth-first traversal of the expression tree
        var toProcess = new Queue<Expression>();
        toProcess.Enqueue(query.Expression);

        while (toProcess.TryDequeue(out var next))
        {
            // If the next expression is a constant, a lambda, or an extension (i.e., the root query), we don't need to do anything
            if (next is ConstantExpression or LambdaExpression || next.NodeType == ExpressionType.Extension)
            {
                continue;
            }

            // Unwrap the next expression if it is a conversion
            if (next is UnaryExpression unary)
            {
                toProcess.Enqueue(unary.Operand);
                continue;
            }

            // If we've found a method call, we need to check if it is an Include or a ThenInclude
            if (next is MethodCallExpression call)
            {
                // If it is an Include, we need to add the property name to the result
                if (call.Method.Name == "Include")
                {
                    result.Add($".{GetPropertyName(call.Arguments[1])}");
                }
                // If it is a ThenInclude, we need to add the property name to the result, and the root of the property to the queue
                else if (call.Method.Name == "ThenInclude")
                {
                    result.Add($".{GetPropertyRoot(call.Arguments[0])}.{GetPropertyName(call.Arguments[1])}");
                }

                // Given that we may have multiple levels of inclusion, we need to process the arguments of the method call as well
                foreach (var arg in call.Arguments)
                {
                    toProcess.Enqueue(arg);
                }

                continue;
            }

            throw new NotImplementedException($"Unable to extract included properties from '{next.NodeType}'");
        }

        return result;
    }

    private static string GetPropertyRoot(Expression e)
    {
        return e is MethodCallExpression call
            ? call.Method.Name == "Include"
                ? GetPropertyName(call.Arguments[1])
                : $"{GetPropertyRoot(call.Arguments[0])}.{GetPropertyName(call.Arguments[1])}"
            : throw new NotImplementedException($"Unable to extract property root from '{e.NodeType}'");
    }

    private static string GetPropertyName(Expression e)
    {
        return e is ConstantExpression @const
            ? (string)@const.Value!
            : e is UnaryExpression unary
            ? GetPropertyName(unary.Operand)
            : e is LambdaExpression lambda
            ? GetPropertyName(lambda.Body)
            : e is MethodCallExpression call
            ? GetPropertyName(call.Arguments[0])
            : e is MemberExpression member
            ? member.Member.Name
            :
        throw new NotImplementedException($"Unable to extract property name from '{e.NodeType}'");
    }
}
