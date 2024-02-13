using System.Linq.Expressions;
using WebApplication1.QueryFilter.Utils;

namespace WebApplication1.QueryFilter.Syntax;

/// <summary>
/// Represents accessing a member of an object
/// </summary>
internal sealed class MemberAccessSyntax : SyntaxRoot
{
    /// <summary>
    /// The member to access
    /// </summary>
    public string Identifier { get; set; }

    /// <summary>
    /// The source that contains the member
    /// </summary>
    public SyntaxRoot? Target { get; set; }

    /// <summary>
    /// Constructs a member reference. If <paramref name="identifier"/> is not a <see cref="IdentifierSyntax"/> an exception will be generated
    /// </summary>
    public MemberAccessSyntax(string identifier, SyntaxRoot? target)
    {
        Identifier = identifier;
        Target = target;
    }

    /// <inheritdoc/>
    public override Expression ToExpression(ParameterScope scope)
    {
        var target = Target switch
        {
            // If the target is an identifier, we are accessing a scope variable
            IdentifierSyntax id => ResolveExpression(scope, id.Name),
            // If the target is null, we are accessing a global variable
            null => scope.Global,
            // Otherwise, we need to evaluate the target
            _ => Target.ToExpression(scope)
        };

        return Expression.PropertyOrField(target, Identifier);
    }

    private static Expression ResolveExpression(ParameterScope scope, string name)
    {
        // First step: Check if the identifier is a known parameter
        if (scope.TryGetParameter(name, out var param))
        {
            return param;
        }

        if (name == "$")
        {
            return scope.Context;
        }

        // Second step: Check if the identifier is a property of the global parameter
        var property = scope.Global.Type.GetProperty(name);
        if (property is not null)
        {
            return Expression.Property(scope.Global, property);
        }

        // If we've gotten here, we're out of luck
        throw new InvalidOperationException($"Unable to bind identifier '{name}'");
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Target}.{Identifier}";
    }

    internal override bool HasContextReference()
    {
        return Target?.HasContextReference() ?? false;
    }
}