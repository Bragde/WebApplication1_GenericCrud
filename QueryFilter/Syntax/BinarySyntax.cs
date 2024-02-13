using System.Linq.Expressions;
using WebApplication1.QueryFilter.Extensions;
using WebApplication1.QueryFilter.Utils;

namespace WebApplication1.QueryFilter.Syntax;

/// <summary>
/// Represents a binary operation
/// </summary>
internal sealed class BinarySyntax : SyntaxRoot
{
    /// <summary>
    /// The kind of binary operation
    /// </summary>
    public BinaryType BinaryType { get; set; }

    /// <summary>
    /// The left hand operand
    /// </summary>
    public SyntaxRoot Left { get; set; }

    /// <summary>
    /// The right hand operand
    /// </summary>
    public SyntaxRoot Right { get; set; }

    /// <summary>
    /// Constructor for binary operation syntax
    /// </summary>
    public BinarySyntax(BinaryType binaryType, SyntaxRoot left, SyntaxRoot right)
    {
        BinaryType = binaryType;
        Left = left;
        Right = right;
    }

    /// <summary>
    /// To aid debugging
    /// </summary>
    public override string ToString()
    {
        return $"({Left} {BinaryType.Stringify()} {Right})";
    }

    /// <inheritdoc/>
    public override Expression ToExpression(ParameterScope scope)
    {
        var left = Left.ToExpression(scope);
        var right = Right.ToExpression(scope);
        var expressionType = GetExpressionType();

        // Comparisons require the types to be normalized,
        // that is, we must find a common type that both sides can be converted to
        if (expressionType.IsComparison() || expressionType.IsArithmetic())
        {
            (left, right) = TypeConverter.NormalizeTypes(left, right);
        }

        return Expression.MakeBinary(
            expressionType,
            left,
            right);
    }

    private ExpressionType GetExpressionType()
    {
        return BinaryType switch
        {
            BinaryType.Or => ExpressionType.OrElse,
            BinaryType.And => ExpressionType.AndAlso,
            BinaryType.Equal => ExpressionType.Equal,
            BinaryType.NotEqual => ExpressionType.NotEqual,
            BinaryType.GreaterThan => ExpressionType.GreaterThan,
            BinaryType.GreaterThanEqual => ExpressionType.GreaterThanOrEqual,
            BinaryType.LessThan => ExpressionType.LessThan,
            BinaryType.LessThanEqual => ExpressionType.LessThanOrEqual,
            BinaryType.Add => ExpressionType.Add,
            BinaryType.Subtract => ExpressionType.Subtract,
            BinaryType.Multiply => ExpressionType.Multiply,
            BinaryType.Divide => ExpressionType.Divide,
            BinaryType.Modulo => ExpressionType.Modulo,
            _ => throw new NotImplementedException()
        };
    }

    internal override bool HasContextReference()
    {
        return Left.HasContextReference() || Right.HasContextReference();
    }
}