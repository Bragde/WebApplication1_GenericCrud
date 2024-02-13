using System.Linq.Expressions;
using WebApplication1.QueryFilter.Utils;

namespace WebApplication1.QueryFilter.Syntax;

/// <summary>
/// Represents a lambda expression
/// </summary>
internal sealed class LambdaSyntax : SyntaxRoot
{
    /// <summary>
    /// The parameter of the lambda
    /// </summary>
    public IdentifierSyntax? Identifier { get; set; }

    /// <summary>
    /// The body of the lambda
    /// </summary>
    public SyntaxRoot Body { get; set; }

    /// <summary>
    /// Constructs a lambda expression
    /// </summary>
    public LambdaSyntax(IdentifierSyntax? identifier, SyntaxRoot body)
    {
        Identifier = identifier;
        Body = body;
    }

    /// <inheritdoc/>
    public override Expression ToExpression(ParameterScope scope)
    {
        // If the identifier is null, we are accessing a global variable
        var parameter = Identifier == null ? scope.Global : scope[Identifier.Name];
        return Expression.Lambda(Body.ToExpression(scope), parameter);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Identifier} => {Body}";
    }

    internal override bool HasContextReference()
    {
        return Body.HasContextReference();
    }
}