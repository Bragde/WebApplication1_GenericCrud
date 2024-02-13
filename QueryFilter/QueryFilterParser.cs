using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using QueryFilter.Extensions;
using QueryFilter.Tokens;
using QueryFilter.Utils;

namespace QueryFilter;

/// <summary>
/// Service to parse a given expression into an expression tree for use with Entity Framework
/// </summary>
public static class QueryFilterParser
{
    /// <summary>
    /// Parses the given filter into an expression tree. This does NOT apply any default filters
    /// </summary>
#if NET7_0_OR_GREATER
    [return: NotNullIfNotNull(nameof(filter))]
#else
    [return: NotNullIfNotNull("filter")]
#endif
    public static Expression<Func<T, bool>>? Parse<T>(string? filter)
    {
        return Parse<T>(filter, null);
    }

    internal static Expression<Func<T, bool>>? Parse<T>(string? filter, IQueryable<T>? source = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        var tokens = Tokenizer.Read(filter);

        var syntaxTree = SyntaxParser.Parse(tokens);

        if (!tokens.Empty)
        {
            if (tokens.TryMatch(TokenKind.Comma))
            {
                throw new InvalidOperationException("Unexpected trailing comma, did you use the correct decimal separator?");
            }

            throw new InvalidOperationException($"Unexpected trailing content: {tokens.Next()}");
        }

        var parameter = Expression.Parameter(typeof(T));
        var scope = new ParameterScope(parameter);

        if (syntaxTree.HasContextReference())
        {
            var context = source
                .NotNull("Cannot use context reference without a source")
                .GetType()
                .GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(source)
                .NotNull("Unable to resolve source context");

            scope.SetContext(Expression.Constant(context));
        }

        return Expression.Lambda<Func<T, bool>>(syntaxTree.ToExpression(scope), parameter);
    }
}
