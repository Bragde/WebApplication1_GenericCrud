using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using QueryFilter.Utils;

namespace QueryFilter.Syntax;

//
// Summary:
//     Base class for all syntax nodes
internal abstract class SyntaxRoot
{
    //
    // Summary:
    //     To aid debugging
    public abstract override string ToString();

    //
    // Summary:
    //     Converts the syntax tree into an expression tree
    public abstract Expression ToExpression(ParameterScope scope);

    //
    // Summary:
    //     Returns true, and sets value if the instance is of type T
    public bool Is<T>([NotNullWhen(true)] out T? value) where T : SyntaxRoot
    {
        value = this as T;
        return value != null;
    }

    internal abstract bool HasContextReference();
}

