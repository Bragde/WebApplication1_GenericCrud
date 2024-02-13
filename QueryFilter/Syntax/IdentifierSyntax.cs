using System.Linq.Expressions;
using WebApplication1.QueryFilter.Utils;

namespace WebApplication1.QueryFilter.Syntax;

/// <summary>
/// Represents an identifier
/// </summary>
internal sealed class IdentifierSyntax : SyntaxRoot
{
    /// <summary>
    /// The name of the identifier
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Constructs an identifier
    /// </summary>
    public IdentifierSyntax(string name)
    {
        Name = name;
    }

    /// <inheritdoc/>
    public override Expression ToExpression(ParameterScope scopeParameter)
    {
        if (Name == "$")
        {
            return scopeParameter.Context;
        }

        return Expression.PropertyOrField(scopeParameter.Global, Name);
    }

    internal override bool HasContextReference()
    {
        return Name == "$";
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Name;
    }
}