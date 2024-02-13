using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using QueryFilter.Syntax;

namespace QueryFilter.Extensions;

internal static class QueryExtensions
{
    /// <summary>
    /// Returns true if <paramref name="type"/> is any of <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="DateOnly"/>, or <see cref="TimeOnly"/>
    /// </summary>
    internal static bool IsDateType(this Type type)
    {
        return type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly);
    }

    /// <summary>
    /// Returns true if the expression is a <cref name="ConstantExpression"/>
    /// </summary>
    internal static bool IsConst(this Expression expression)
    {
        return expression is ConstantExpression;
    }

    /// <summary>
    /// Returns true if the expression is a <cref name="ConstantExpression"/> with a null value
    /// </summary>
    internal static bool IsNull(this Expression expression)
    {
        return expression is ConstantExpression constant && constant.Value == null;
    }

    internal static Expression Convert(this Expression expression, Type type)
    {
        // If the expression is already of the given type, return it
        if (expression.Type == type)
        {
            return expression;
        }

        // If the expression is a constant, unwrap the value and convert it to the given type
        if (expression is ConstantExpression constant)
        {
            // Are we trying to convert null to a value type?
            if (type.IsValueType && constant.Value == null)
            {
                // This is not allowed, throw an exception
                throw new InvalidOperationException($"Cannot convert null to a value type {type.Name}");
            }

            return Expression.Constant(System.Convert.ChangeType(constant.Value, type, CultureInfo.InvariantCulture));
        }

        // If the expression is already doing a conversion, unwrap it and convert the operand
        if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
        {
            return unary.Operand.Convert(type);
        }

        // Otherwise, convert the expression to the given type
        return Expression.Convert(expression, type);
    }

    /// <summary>
    /// Creates a copy of the given value
    /// </summary>
    internal static T Out<T>(this T value, out T outValue)
    {
        outValue = value;
        return value;
    }

    /// <summary>
    /// Returns true if the given expression type is a comparison operator
    /// </summary>
    internal static bool IsComparison(this ExpressionType expressionType)
    {
        return expressionType is ExpressionType.Equal
            or ExpressionType.NotEqual
            or ExpressionType.GreaterThan
            or ExpressionType.GreaterThanOrEqual
            or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual;
    }

    /// <summary>
    /// Returns true if the given expression type is an arithmetic operator
    /// </summary>
    internal static bool IsArithmetic(this ExpressionType expressionType)
    {
        return expressionType is ExpressionType.Add
            or ExpressionType.Subtract
            or ExpressionType.Multiply
            or ExpressionType.Divide
            or ExpressionType.Modulo;
    }

    /// <summary>
    /// Grabs the nth type argument from a generic type, or returns the type if it's not generic
    /// </summary>
    internal static Type Unwrap(this Type t, int n)
    {
        return t.GenericTypeArguments.Length > 0 ? t.GenericTypeArguments[n].Unwrap() : t;
    }

    /// <summary>
    /// Unwraps a generic type, returning the type argument if it's a generic type with only 1 type argument, or the type itself if it's not generic
    /// </summary>
    internal static Type Unwrap(this Type t)
    {
        return t.GenericTypeArguments.Length > 1
            ? throw new AmbiguousMatchException("Unable to unwrap type with more than 1 type argument")
            : t.GenericTypeArguments.Length > 0 ? t.GenericTypeArguments[0].Unwrap() : t;
    }

    /// <summary>
    /// Extends <see cref="Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, bool})" /> with an index
    /// </summary>
    internal static bool All<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        var index = 0;
        foreach (var item in source)
        {
            if (!predicate(item, index++))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns true if <paramref name="type"/> references the same type as <typeparamref name="T"/>
    /// </summary>
    internal static bool Is<T>(this Type type)
    {
        return type == typeof(T);
    }

    /// <summary>
    /// Dequeues <paramref name="count"/> items from the given <paramref name="queue"/>
    /// </summary>
    internal static IEnumerable<T> Dequeue<T>(this Queue<T> queue, int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return queue.Dequeue();
        }
    }

    /// <summary>
    /// Enumerates the given <paramref name="source"/>, forcing it to be evaluated and discarding the result
    /// </summary>
    internal static void Enumerate<T>(this IEnumerable<T> source)
    {
        // Intentionally discarding the result
        _ = source.ToList();
    }

    /// <summary>
    /// Returns true if <paramref name="type"/> is assignable to <see cref="IEnumerable"/>, excluding <see cref="string"/>
    /// </summary>
    internal static bool IsCollection(this Type type)
    {
        return type != typeof(string) && type.IsAssignableTo(typeof(IEnumerable));
    }

    /// <summary>
    /// Converts a <see cref="BinaryType"/> to a string
    /// </summary>
    internal static string Stringify(this BinaryType binaryType)
    {
        return binaryType switch
        {
            BinaryType.And => "and",
            BinaryType.Equal => "eq",
            BinaryType.GreaterThan => "gt",
            BinaryType.GreaterThanEqual => "gte",
            BinaryType.LessThan => "lt",
            BinaryType.LessThanEqual => "lte",
            BinaryType.NotEqual => "neq",
            BinaryType.Or => "or",
            _ => ""
        };
    }

    /// <summary>
    /// Attempts to resolve a method from the given <paramref name="type"/> with the given <paramref name="name"/>, that fulfills the given <paramref name="predicate"/>. Will throw an exception if no method is found.
    /// </summary>
    internal static MethodInfo FindMethod(this Type type, string name, Func<MethodInfo, bool> predicate)
    {
        return type.GetMethods().First(m => m.Name == name && predicate(m));
    }

    /// <summary>
    /// Returns all methods from the given <paramref name="type"/> with the given <paramref name="name"/>
    /// </summary>
    internal static IEnumerable<MethodInfo> FindMethods(this Type type, string name)
    {
        return type.GetMethods().Where(m => m.Name == name);
    }

    [return: NotNull]
    internal static T NotNull<T>(this T? value, string error) where T : class
    {
        if (value is null)
        {
            throw new InvalidOperationException(error);
        }

        return value;
    }
}
