using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace QueryFilter.Utils;

//
// Summary:
//     Handles parameters scoping, so that we can handle nested lambda expressions
internal sealed class ParameterScope
{
    //
    // Summary:
    //     This contains all the parameters of the current scope
    private readonly Dictionary<string, ParameterExpression> _parameters = new Dictionary<string, ParameterExpression>();

    //
    // Summary:
    //     This is the global parameter, which is the parameter of the (implicit) top-level
    //     lambda expression
    public ParameterExpression Global { get; }

    public ConstantExpression Context { get; private set; } = Expression.Constant(null);


    //
    // Summary:
    //     Gets the parameter with the given name, throws an exception if it does not exist
    public ParameterExpression this[string name] => GetParameter(name);

    //
    // Summary:
    //     Defines a new parameter scope with a global parameter
    public ParameterScope(ParameterExpression global)
    {
        Global = global;
    }

    //
    // Summary:
    //     Returns true if the scope contains a parameter with the given name, and returns
    //     it in parameter
    public bool TryGetParameter(string name, [NotNullWhen(true)] out ParameterExpression? parameter)
    {
        return _parameters.TryGetValue(name, out parameter);
    }

    //
    // Summary:
    //     Gets the parameter with the given name, throws an exception if it does not exist
    private ParameterExpression GetParameter(string name)
    {
        if (_parameters.TryGetValue(name, out ParameterExpression value))
        {
            return value;
        }

        Dictionary<string, ParameterExpression>.KeyCollection keys = _parameters.Keys;
        throw new InvalidOperationException((keys.Count == 0) ? "Scope does not contain any parameters" : ("Scope does not contain a parameter with name '" + name + "', candidates are: " + string.Join(", ", keys)));
    }

    internal void SetContext(ConstantExpression context)
    {
        Context = context;
    }

    //
    // Summary:
    //     Enters a new scope with the given name and type, returns a disposable that will
    //     discard the scope parameter when disposed
    //
    // Exceptions:
    //   T:System.InvalidOperationException:
    public IDisposable EnterScope(string name, Type type)
    {
        string name2 = name;
        if (_parameters.ContainsKey(name2))
        {
            throw new InvalidOperationException("Scope already contains a parameter with name '" + name2 + "'");
        }

        _parameters.Add(name2, Expression.Parameter(type, name2));
        return new ScopedCallback(delegate
        {
            _parameters.Remove(name2);
        });
    }
}