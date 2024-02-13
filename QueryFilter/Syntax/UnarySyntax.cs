using System.Linq.Expressions;
using QueryFilter.Utils;

namespace QueryFilter.Syntax;

/// <summary>
/// Represents an unary operation
/// </summary>
internal sealed class UnarySyntax : SyntaxRoot
{
    /// <summary>
    /// Type of unary operation
    /// </summary>
    public UnaryType UnaryType { get; set; }

    /// <summary>
    /// Operand to operate on
    /// </summary>
    public SyntaxRoot Operand { get; set; }

    /// <summary>
    /// Constructs an unary operation
    /// </summary>
    public UnarySyntax(UnaryType unaryType, SyntaxRoot operand)
    {
        UnaryType = unaryType;
        Operand = operand;
    }

    /// <inheritdoc/>
    public override Expression ToExpression(ParameterScope scope)
    {
        return UnaryType == UnaryType.Negate ? Expression.Negate(Operand.ToExpression(scope)) : (Expression)Expression.Not(Operand.ToExpression(scope));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"(not {Operand})";
    }

    internal override bool HasContextReference()
    {
        return Operand.HasContextReference();
    }
}