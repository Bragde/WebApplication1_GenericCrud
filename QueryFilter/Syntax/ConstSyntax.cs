using System.Linq.Expressions;
using WebApplication1.QueryFilter.Utils;

namespace WebApplication1.QueryFilter.Syntax;

/// <summary>
/// Represents a constant value
/// </summary>
internal sealed class ConstSyntax : SyntaxRoot
{
    /// <summary>
    /// Represents a null value
    /// </summary>
    public static ConstSyntax Null { get; } = new(null);

    /// <summary>
    /// The value, boxed in an object
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Constructs a new constant
    /// </summary>
    public ConstSyntax(object? value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    public override Expression ToExpression(ParameterScope _)
    {
        return Expression.Constant(Value);
    }

    internal override bool HasContextReference()
    {
        return false;
    }

    /// <summary>
    /// To aid debugging
    /// </summary>
    public override string ToString()
    {
        return "Const: " + (Value?.GetType() == typeof(string) ? $"'{Value}'" : Value?.ToString() ?? "(null)");
    }
}