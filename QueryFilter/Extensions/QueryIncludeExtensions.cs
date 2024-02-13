using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WebApplication1.Models;
using WebApplication1.QueryFilter.Syntax;
using WebApplication1.QueryFilter.Tokens;
using WebApplication1.QueryFilter.Utils;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WebApplication1.QueryFilter.Extensions;

public static class QueryIncludeExtensions
{
    public static IQueryable<T> ApplyIncludes<T>(this IQueryable<T> query, string[]? includes) where T : EntityBase
    {
        return includes == null || includes.Length == 0
            ? query
            : includes.Aggregate(query, (current, include) => current.ApplyIncludes(include));
    }

    private static IQueryable<T> ApplyIncludes<T>(this IQueryable<T> query, string? include) where T : EntityBase
    {
        if (string.IsNullOrWhiteSpace(include))
            return query;

        TokenStream tokenStream = Tokenizer.Read(include);
        Type type = null;
        MethodCallExpression methodCallExpression = null;
        Type type2 = typeof(T);
        ConstantExpression constantExpression = Expression.Constant(query.IgnoreAutoIncludes());
        while (!tokenStream.Empty)
        {
            Token token = tokenStream.Expect(TokenKind.Identifier);
            ParameterExpression parameterExpression = Expression.Parameter(type2.Unwrap());
            Expression expression = Expression.PropertyOrField(parameterExpression, token.AsString());
            if (tokenStream.TryMatch(TokenKind.OpenParen, out var _))
            {
                if (!expression.Type.IsCollection())
                    throw new InvalidOperationException($"Filtering is only supported for collections, but '{expression}' is not a collection");

                SyntaxRoot syntaxRoot = SyntaxParser.Parse(tokenStream);
                if (syntaxRoot is LambdaSyntax)
                    throw new InvalidOperationException("Do not use lambda expressions for filtered includes, use a simple expression instead");

                tokenStream.Expect(TokenKind.CloseParen);
                Type type3 = expression.Type.Unwrap();
                ParameterScope scope = new ParameterScope(Expression.Parameter(type3));
                LambdaExpression arg = (LambdaExpression)new LambdaSyntax(null, syntaxRoot).ToExpression(scope);
                expression = Expression.Call(ReflectionCache.Where.MakeGenericMethod(type3), expression, arg);
            }

            if (type == null)
            {
                methodCallExpression = Expression.Call(
                    ReflectionCache.Include.MakeGenericMethod(type2.Unwrap(), expression.Type),
                    (Expression)((object)methodCallExpression ?? constantExpression),
                    Expression.Lambda(expression, parameterExpression));
                type = type2;
                type2 = expression.Type;
            }
            else
            {
                methodCallExpression = Expression.Call((
                    type2.IsCollection()
                        ? ReflectionCache.ThenIncludeCollection
                        : ReflectionCache.ThenIncludeProperty).MakeGenericMethod(type, type2.Unwrap(), expression.Type),
                    methodCallExpression,
                    Expression.Lambda(expression, parameterExpression));
                type = type2;
                type2 = methodCallExpression.Type.Unwrap(1);
            }

            if (tokenStream.Empty)
                continue;

            if (tokenStream.TryMatch(TokenKind.Comma))
            {
                if (tokenStream.Empty)
                    throw new InvalidOperationException("Unexpected end of expression");

                type = null;
                type2 = typeof(T);
            }
            else
            {
                tokenStream.Expect(TokenKind.Dot);
                if (tokenStream.Empty)
                    throw new InvalidOperationException("Unexpected end of expression");
            }
        }
        return (IQueryable<T>)Expression.Lambda(methodCallExpression).Compile().DynamicInvoke();
    }

    //
    // Summary:
    //     Ensures that only explicitly included properties are set, as well as removing
    //     any circular references.
    //
    // Remarks:
    //     This method will enumerate the query, and should therefore be called last.
    public static IEnumerable<T> RemoveCycles<T>(this IQueryable<T> source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source, "source");
        List<string> paths = IncludedProperties.Parse(source);
        return IncludedProperties.Clean(source.ToList(), paths);
    }

    //
    // Summary:
    //     Ensures that only explicitly included properties are set, as well as removing
    //     any circular references.
    //
    // Remarks:
    //     This method will enumerate the query, and should therefore be called last.
    public static async Task<IEnumerable<T>> RemoveCyclesAsync<T>(this IQueryable<T> source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source, "source");
        return IncludedProperties.Clean(paths: IncludedProperties.Parse(source), value: await source.ToListAsync().ConfigureAwait(continueOnCapturedContext: false));
    }

    //
    // Summary:
    //     Ensures that only explicitly included properties are set, as well as removing
    //     any circular references.
    //
    // Remarks:
    //     This method will call FirstOrDefaultAsync, and should therefore be called last.
    public static async Task<T?> RemoveCyclesFirstOrDefaultAsync<T>(this IQueryable<T> source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source, "source");
        List<string> includedProperties = IncludedProperties.Parse(source);
        T val = await source.FirstOrDefaultAsync().ConfigureAwait(continueOnCapturedContext: false);
        return val == null ? null : IncludedProperties.Clean(val, includedProperties);
    }
}
