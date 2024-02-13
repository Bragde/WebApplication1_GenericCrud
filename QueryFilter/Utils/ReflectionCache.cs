using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using WebApplication1.QueryFilter.Extensions;

namespace WebApplication1.QueryFilter.Utils;

internal static class MethodName
{
    public const string Any = nameof(Enumerable.Any);
    public const string Contains = nameof(string.Contains);
    public const string Count = nameof(Enumerable.Count);
    public const string EndsWith = nameof(string.EndsWith);
    public const string Include = nameof(EntityFrameworkQueryableExtensions.Include);
    public const string Length = nameof(string.Length);
    public const string Max = nameof(Enumerable.Max);
    public const string Sum = nameof(Enumerable.Sum);
    public const string OrderBy = nameof(Queryable.OrderBy);
    public const string OrderByDescending = nameof(Queryable.OrderByDescending);
    public const string StartsWith = nameof(string.StartsWith);
    public const string Substring = nameof(string.Substring);
    public const string ThenBy = nameof(Queryable.ThenBy);
    public const string ThenByDescending = nameof(Queryable.ThenByDescending);
    public const string ThenInclude = nameof(EntityFrameworkQueryableExtensions.ThenInclude);
    public const string ToLower = nameof(string.ToLower);
    public const string ToUpper = nameof(string.ToUpper);
    public const string Trim = nameof(string.Trim);
    public const string Where = nameof(Enumerable.Where);
}

// C#'s support of GetMethod when dealing with partially resolved generic methods is... limited, so we're doing this instead
internal static class ReflectionCache
{
    private static readonly Dictionary<string, MethodInfo> _cache = new();
    private static readonly Dictionary<string, MethodInfo[]> _cacheGroup = new();

    private static MethodInfo Resolve(string name, Func<MethodInfo> resolver)
    {
        return _cache.TryGetValue(name, out var method)
            ? method
            : _cache[name] = resolver();
    }

    private static MethodInfo[] Resolve(string name, Func<MethodInfo[]> resolver)
    {
        return _cacheGroup.TryGetValue(name, out var method)
            ? method
            : _cacheGroup[name] = resolver();
    }

    /// <summary>
    /// Cached instance of <see cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>
    /// </summary>
    public static MethodInfo Any => Resolve(nameof(Any), () => typeof(Enumerable).FindMethod(MethodName.Any, m => m.GetParameters().Length == 1));

    /// <summary>
    /// Cached instance of <see cref="Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// </summary>
    public static MethodInfo AnyPredicate => Resolve(nameof(AnyPredicate), () => typeof(Enumerable).FindMethod(MethodName.Any, m => m.GetParameters().Length == 2));

    /// <summary>
    /// Cached instance of <see cref="Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// </summary>
    public static MethodInfo All => Resolve(nameof(All), () => typeof(Enumerable).FindMethod(MethodName.Any, m => m.GetParameters().Length == 2));

    /// <summary>
    /// Cached instance of <see cref="Enumerable.Max{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
    /// </summary>
    public static MethodInfo Max => Resolve(nameof(Max), () => typeof(Enumerable).FindMethod(MethodName.Max, m => m.GetParameters().Length == 2 && m.GetGenericArguments().Length == 2));

    /// <summary>
    /// Cached instances of Sum methods
    /// </summary>
    public static MethodInfo[] SumProperty => Resolve(MethodName.Sum, () => typeof(Enumerable).GetMethods().Where(m => m.Name == MethodName.Sum).ToArray());

    /// <summary>
    /// Cached instance of <see cref="string.Contains(string)"/>
    /// </summary>
    public static MethodInfo Contains => Resolve(nameof(Contains), () => typeof(string).GetMethod(MethodName.Contains, new[] { typeof(string) })!);

    /// <summary>
    /// Cached instance of <see cref="Enumerable.Count{TSource}(IEnumerable{TSource})"/>
    /// </summary>
    public static MethodInfo Count => Resolve(nameof(Count), () => typeof(Enumerable).FindMethod(MethodName.Count, m => m.GetParameters().Length == 1));

    /// <summary>
    /// Cached instance of <see cref="Enumerable.Count{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// </summary>
    public static MethodInfo CountPredicate => Resolve(nameof(CountPredicate), () => typeof(Enumerable).FindMethod(MethodName.Count, m => m.GetParameters().Length == 2));

    /// <summary>
    /// Cached instance of <see cref="string.EndsWith(string)"/>
    /// </summary>
    public static MethodInfo EndsWith => Resolve(nameof(EndsWith), () => typeof(string).GetMethod(MethodName.EndsWith, new[] { typeof(string) })!);

    /// <summary>
    /// Cached instance of <see cref="EntityFrameworkQueryableExtensions.Include{TEntity, TProperty}(IQueryable{TEntity}, Expression{Func{TEntity, TProperty}})"/>
    /// </summary>
    public static MethodInfo Include => typeof(EntityFrameworkQueryableExtensions)
        .FindMethod(MethodName.Include, m => m.GetParameters()[1].ParameterType != typeof(string));

    /// <summary>
    /// Cached instance of <see cref="Queryable.OrderBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    public static MethodInfo OrderBy => Resolve(nameof(OrderBy), () => typeof(Queryable).FindMethod(MethodName.OrderBy, m => m.GetParameters().Length == 2));

    /// <summary>
    /// Cached instance of <see cref="Queryable.OrderByDescending{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    public static MethodInfo OrderByDescending => Resolve(nameof(OrderByDescending), () => typeof(Queryable).FindMethod(MethodName.OrderByDescending, m => m.GetParameters().Length == 2));

    /// <summary>
    /// Cached instance of <see cref="string.Substring(int)"/>
    /// </summary>
    public static MethodInfo SubstringFromStart => Resolve(nameof(SubstringFromStart), () => typeof(string).GetMethod(MethodName.Substring, new[] { typeof(int) })!);

    /// <summary>
    /// Cached instance of <see cref="string.Substring(int, int)"/>
    /// </summary>
    public static MethodInfo SubstringRange => Resolve(nameof(SubstringRange), () => typeof(string).GetMethod(MethodName.Substring, new[] { typeof(int), typeof(int) })!);

    /// <summary>
    /// Cached instance of <see cref="string.StartsWith(string)"/>
    /// </summary>
    public static MethodInfo StartsWith => Resolve(nameof(StartsWith), () => typeof(string).GetMethod(MethodName.StartsWith, new[] { typeof(string) })!);

    /// <summary>
    /// Cached instance of <see cref="Queryable.ThenBy{TSource, TKey}(IOrderedQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    public static MethodInfo ThenBy => Resolve(nameof(ThenBy), () => typeof(Queryable).FindMethod(MethodName.ThenBy, m => m.GetParameters().Length == 2));

    /// <summary>
    /// Cached instance of <see cref="Queryable.ThenByDescending{TSource, TKey}(IOrderedQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    public static MethodInfo ThenByDescending => Resolve(nameof(ThenByDescending), () => typeof(Queryable).FindMethod(MethodName.ThenByDescending, m => m.GetParameters().Length == 2));

    /// <summary>
    /// Cached instance of <see cref="EntityFrameworkQueryableExtensions.ThenInclude{TEntity, TPreviousProperty, TProperty}(Microsoft.EntityFrameworkCore.Query.IIncludableQueryable{TEntity, IEnumerable{TPreviousProperty}}, Expression{Func{TPreviousProperty, TProperty}})"/>
    /// </summary>
    public static MethodInfo ThenIncludeCollection => ThenIncludeMethods
        .First(m => m.parameters[0].ParameterType.GenericTypeArguments[1].IsGenericType)
        .method;

    /// <summary>
    /// Cached instance of <see cref="EntityFrameworkQueryableExtensions.ThenInclude{TEntity, TPreviousProperty, TProperty}(Microsoft.EntityFrameworkCore.Query.IIncludableQueryable{TEntity, TPreviousProperty}, Expression{Func{TPreviousProperty, TProperty}})"/>
    /// </summary>
    public static MethodInfo ThenIncludeProperty => ThenIncludeMethods
        .First(m => !m.parameters[0].ParameterType.GenericTypeArguments[1].IsGenericType)
        .method;

    /// <summary>
    /// Cached instance of <see cref="string.ToLower()"/>
    /// </summary>
    public static MethodInfo ToLower => Resolve(nameof(ToLower), () => typeof(string).GetMethod(MethodName.ToLower, Array.Empty<Type>())!);

    /// <summary>
    /// Cached instance of <see cref="string.ToUpper()"/>
    /// </summary>
    public static MethodInfo ToUpper => Resolve(nameof(ToUpper), () => typeof(string).GetMethod(MethodName.ToUpper, Array.Empty<Type>())!);

    /// <summary>
    /// Cached instance of <see cref="string.Trim()"/>
    /// </summary>
    public static MethodInfo Trim => Resolve(nameof(Trim), () => typeof(string).GetMethod(MethodName.Trim, Array.Empty<Type>())!);

    /// <summary>
    /// Cached instance of <see cref="Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// </summary>
    public static MethodInfo Where => Resolve(nameof(Where), () => typeof(Enumerable).FindMethod(MethodName.Where, m =>
    {
        // Get the method parameters, we are expecting a method with
        var parameters = m.GetParameters();
        return parameters.Length == 2 // two parameters
            && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) // where the first if an IEnumerable<T>
            && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>); // and the second is a Func<T, TRes>
    }));

    private static readonly List<(MethodInfo method, ParameterInfo[] parameters)> ThenIncludeMethods = typeof(EntityFrameworkQueryableExtensions)
        .FindMethods(MethodName.ThenInclude)
        .Select(method =>
        (
            method,
            method.GetParameters()
        ))
        .ToList();

    /// <summary>
    /// Given an expression "f => f.Foo()", will return a <see cref="MethodInfo"/> entry for Foo
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the expression body is anything but a method call</exception>
    public static MethodInfo GetMethodInfo(Expression<Action> expression)
    {
        return expression.Body is MethodCallExpression member
            ? member.Method
            : throw new ArgumentException("Expression is not a method", nameof(expression));
    }
}