using System.Linq.Expressions;
using System.Reflection;
using WebApplication1.QueryFilter.Utils;
using WebApplication1.QueryFilter.Extensions;

namespace WebApplication1.QueryFilter.Syntax;

/// <summary>
/// Represents a method call
/// </summary>
internal sealed class CallSyntax : SyntaxRoot
{
    // To make lookup a bit easier, we cache all methods with a FunctionAttribute
    // First we grab all the methods in the current class
    private static readonly Dictionary<string, List<MethodInfo>> _methodCache = typeof(CallSyntax).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
        // then we filter down to only the ones with a FunctionAttribute
        .Where(m => m.GetCustomAttribute<FunctionAttribute>() != null)
        // then we group them by their function name (to allow overloads)
        .GroupBy(m => m.GetCustomAttribute<FunctionAttribute>()!.Name)
        // and finally we convert the grouping to a dictionary
        .ToDictionary(g => g.Key.ToUpperInvariant(), g => g.ToList());

    /// <summary>
    /// The call target
    /// </summary>
    public SyntaxRoot Target { get; set; }

    /// <summary>
    /// A list of arguments to pass to the method
    /// </summary>
#if NET8_0_OR_GREATER
    public List<SyntaxRoot> Arguments { get; set; } = [];
#else
    public List<SyntaxRoot> Arguments { get; set; } = new();
#endif

    /// <summary>
    /// Constructor for method call syntax
    /// </summary>
    public CallSyntax(SyntaxRoot target, List<SyntaxRoot> arguments)
    {
        Target = target;
        Arguments = arguments;
    }

    internal override bool HasContextReference()
    {
        return Target.HasContextReference() || Arguments.Any(a => a.HasContextReference());
    }

    /// <inheritdoc/>
    public override Expression ToExpression(ParameterScope scope)
    {
        IdentifierSyntax? identifier = null;
        SyntaxRoot? thisArgument = null;

        // The next bit of code handles the two different ways of calling a method, i.e. `any(Items)` and `Items.any()`

        // If the target is a member access, we grab the identifier and the target
        if (Target.Is<MemberAccessSyntax>(out var member))
        {
            identifier = new IdentifierSyntax(member.Identifier);
            thisArgument = member.Target!;
        }
        // otherwise, we ensure that the target is an identifier
        else if (!Target.Is(out identifier))
        {
            throw new InvalidOperationException($"Expected alternative, got {Target}");
        }

        // Next, we find all methods with the given name
        var candidates = _methodCache.TryGetValue(identifier.Name.ToUpperInvariant(), out var methods) ?
            methods :
#if NET8_0_OR_GREATER
            [];
#else
            new();
#endif

        // Nothing found? Throw an exception
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"Unknown method '{identifier.Name}'");
        }

        // We create a list of arguments to pass to the method, prepending the scope
        var arguments = new object[] { scope }.Concat(Arguments).ToList();

        // If we have a "this", we're dealing with a `Items.any()` call, so we prepend "this" to the arguments
        if (thisArgument != null)
        {
            arguments.Insert(1, thisArgument);
        }

        // We find the first method that matches the number of arguments and their types
        var method = candidates.Find(m =>
        {
            var parameters = m.GetParameters();
            // If the method has no parameters, and we have a single argument, we cheat a little. This way, methods like `Now()` below doesn't need to have a scope parameter it doesn't use
            return (parameters.Length == 0 && arguments.Count == 1) ||
                // Otherwise, we check that the number of arguments match the number of parameters, and that each argument are of a compatible type with the parameter
                (parameters.Length == arguments.Count && parameters.All(p => p.ParameterType.IsAssignableFrom(arguments[p.Position].GetType())));
        }) ?? throw new InvalidOperationException($"No matching overload for {identifier.Name}({string.Join(", ", Arguments.Select(a => a.GetType().Name))})");

        // If we have found a method that require no parameters, we remove the previously prepended parameter
        if (arguments.Count == 1 && method.GetParameters().Length == 0)
        {
            arguments.Clear();
        }

        return (Expression)method.Invoke(this, arguments.ToArray())!;
    }

    // These methods are called via reflection
#if NET6_0 || NET7_0
#pragma warning disable IDE0051 // Remove unused private members
#endif
    /// <summary>
    /// Returns true if the collection contains any entries: <c>Items.any()</c>
    /// </summary>
    [Function("any")]
    private static MethodCallExpression Any(ParameterScope scope, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsCollection(target);

        var anyMethod = ReflectionCache.Any.MakeGenericMethod(target.Type.GenericTypeArguments[0]);
        return Expression.Call(anyMethod, target);
    }

    /// <summary>
    /// Returns true if the collection contains any entries that match the predicate: <c>Items.any(i: i.Name eq "Foo")</c>
    /// </summary>
    [Function("any")]
    private static MethodCallExpression Any(ParameterScope scope, SyntaxRoot identifier, LambdaSyntax predicate)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsCollection(target);

        var unwrappedType = target.Type.GenericTypeArguments[0];

        var anyMethod = ReflectionCache.AnyPredicate.MakeGenericMethod(unwrappedType);
        var lambdaArgument = CreateLambdaArgument(unwrappedType, predicate, scope);

        return Expression.Call(anyMethod, target, lambdaArgument);
    }

    /// <summary>
    /// Returns true if all of the entries in the collection matches the predicate: <c>Items.all(i: i.Name eq "Foo")</c>
    /// </summary>
    [Function("all")]
    public static Expression All(ParameterScope scope, SyntaxRoot identifier, LambdaSyntax predicate)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsCollection(target);

        var unwrappedType = target.Type.GenericTypeArguments[0];

        var allMethod = ReflectionCache.All.MakeGenericMethod(unwrappedType);
        var lambdaArgument = CreateLambdaArgument(unwrappedType, predicate, scope);

        return Expression.Call(allMethod, target, lambdaArgument);
    }

    /// <summary>
    /// Returns true if the string starts with the given value: <c>Name.startsWith("foo")</c>
    /// </summary>
    [Function("startsWith")]
    private static MethodCallExpression StartsWith(ParameterScope scope, SyntaxRoot identifier, SyntaxRoot match)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsString(target);

        return Expression.Call(target, ReflectionCache.StartsWith, match.ToExpression(scope));
    }

    /// <summary>
    /// Returns true if the string ends with the given value: <c>Name.endsWith("foo")</c>
    /// </summary>
    [Function("endsWith")]
    private static MethodCallExpression EndsWith(ParameterScope scope, SyntaxRoot identifier, SyntaxRoot match)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsString(target);

        return Expression.Call(target, ReflectionCache.EndsWith, match.ToExpression(scope));
    }

    /// <summary>
    /// Extracts characters to the end of a string, beginning at <paramref name="start"/>: <c>Name.substring(1)</c>
    /// </summary>
    [Function("substring")]
    private static MethodCallExpression Substring(ParameterScope scope, SyntaxRoot identifier, SyntaxRoot start)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsString(target);

        return Expression.Call(target, ReflectionCache.SubstringFromStart, start.ToExpression(scope));
    }

    /// <summary>
    /// Extracts <paramref name="length"/> characters from a string, beginning at <paramref name="start"/>: <c>Name.substring(1, 5)</c>
    /// </summary>
    [Function("substring")]
    private static MethodCallExpression Substring(ParameterScope scope, SyntaxRoot identifier, SyntaxRoot start, SyntaxRoot length)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsString(target);

        return Expression.Call(target, ReflectionCache.SubstringRange, start.ToExpression(scope), length.ToExpression(scope));
    }

    /// <summary>
    /// Extracts a part of a date: <c>SomeDate.datepart("day")</c>
    /// </summary>
    [Function("datepart")]
    private static MemberExpression Datepart(ParameterScope scope, SyntaxRoot identifier, ConstSyntax datePart)
    {
        var target = identifier.ToExpression(scope);
        // TODO: Ensure target is valid date type
        var part = EnsureConstType<string>(datePart);

        return Expression.PropertyOrField(target, part);
    }

    /// <summary>
    /// Increments or decrements a date: <c>SomeDate.dateadd("day", 1)</c>
    /// </summary>
    [Function("dateadd")]
    private static MethodCallExpression Dateadd(ParameterScope scope, SyntaxRoot identifier, ConstSyntax datePart, SyntaxRoot value)
    {
        var target = identifier.ToExpression(scope);
        var increment = Expression.Convert(value.ToExpression(scope), typeof(double));
        var part = EnsureConstType<string>(datePart);

        var method =
            (target.Type.GetMethod($"Add{part}s", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public) ??
            target.Type.GetMethod($"Add{part}", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)) ??
            throw new InvalidOperationException($"Unknown date part '{part}'");

        return Expression.Call(target, method, increment);
    }

    /// <summary>
    /// Converts a value to a string: <c>SomeNumber.toString()</c>
    /// </summary>
    [Function("tostring")]
    private static MethodCallExpression ToString(ParameterScope scope, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(scope);
        var method = target.Type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>()) ??
            throw new InvalidOperationException($"Unable to find ToString(IFormatProvider) on {target.Type}");

        return Expression.Call(target, method);
    }

    /// <summary>
    /// Removes leading and trailing whitespace from a string: <c>Name.trim()</c>
    /// </summary>
    [Function("trim")]
    private static MethodCallExpression Trim(ParameterScope scope, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsString(target);

        return Expression.Call(target, ReflectionCache.Trim);
    }

    /// <summary>
    /// Example: <c>Name.length()</c>
    /// </summary>
    [Function("length")]
    private static MemberExpression Length(ParameterScope scope, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsString(target);

        return Expression.PropertyOrField(target, MethodName.Length);
    }

    /// <summary>
    /// Example: <c>name.Contains("foo")</c>
    /// </summary>
    [Function("contains")]
    private static MethodCallExpression Contains(ParameterScope scope, SyntaxRoot identifier, SyntaxRoot arg)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsString(target);

        return Expression.Call(target, ReflectionCache.Contains, arg.ToExpression(scope));
    }

    /// <summary>
    /// Returns the number of entries in the collection: <c>Items.count()</c>
    /// </summary>
    [Function("count")]
    private static MethodCallExpression Count(ParameterScope scope, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsCollection(target);

        var countMethod = ReflectionCache.Count.MakeGenericMethod(target.Type.GenericTypeArguments[0]);
        return Expression.Call(countMethod, target);
    }

    /// <summary>
    /// Returns the number of entries in the collection that matches the predicate: <c>Items.count(i: i.Name eq "Foo")</c>
    /// </summary>
    [Function("count")]
    private static MethodCallExpression Count(ParameterScope scope, SyntaxRoot identifier, LambdaSyntax predicate)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsCollection(target);

        var unwrappedType = target.Type.GenericTypeArguments[0];

        var countMethod = ReflectionCache.CountPredicate.MakeGenericMethod(unwrappedType);
        var lambdaArgument = CreateLambdaArgument(unwrappedType, predicate, scope);

        return Expression.Call(countMethod, target, lambdaArgument);
    }

    /// <summary>
    /// Returns the sum of a property in all entries in the collection: <c>Items.sum(i: i.SomeNumber)</c>
    /// </summary>
    [Function("sum")]
    private static MethodCallExpression Sum(ParameterScope scope, SyntaxRoot identifier, LambdaSyntax selector)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsCollection(target);

        var lambdaArgument = CreateLambdaArgument(target.Type.GenericTypeArguments[0], selector, scope);

        var genericSumMethod = Array.Find(ReflectionCache.SumProperty, m => m.GetParameters().Length == 2 && m.ReturnType == lambdaArgument.ReturnType) ??
            throw new InvalidOperationException($"Unable to find Sum(IEnumerable<{target.Type.GenericTypeArguments[0]}>, Func<{target.Type.GenericTypeArguments[0]}, {lambdaArgument.ReturnType}>)");

        var sumMethod = genericSumMethod.MakeGenericMethod(target.Type.GenericTypeArguments[0]);

        return Expression.Call(sumMethod, target, lambdaArgument);
    }

    /// <summary>
    /// Returns the maximum value of a property in the collection: <c>Items.max(i: i.SomeNumber)</c>
    /// </summary>
    [Function("max")]
    private static MethodCallExpression Max(ParameterScope scope, SyntaxRoot identifier, LambdaSyntax selector)
    {
        var target = identifier.ToExpression(scope);
        EnsureTargetIsCollection(target);

        var lambdaArgument = CreateLambdaArgument(target.Type.GenericTypeArguments[0], selector, scope);
        var maxMethod = ReflectionCache.Max.MakeGenericMethod(target.Type.GenericTypeArguments[0]);

        return Expression.Call(maxMethod, target, lambdaArgument);
    }

    /// <summary>
    /// Returns the value of the property if it is not null, otherwise the supplied value will be returned: <c>Name.coalesce("bar")</c>
    /// </summary>
    [Function("coalesce")]
    private static BinaryExpression Coalesce(ParameterScope parameter, SyntaxRoot identifier, SyntaxRoot alternative)
    {
        var target = identifier.ToExpression(parameter);
        return Expression.Coalesce(target, alternative.ToExpression(parameter));
    }

    /// <summary>
    /// Returns true if the property is an empty string: <c>Name.empty()</c>
    /// </summary>
    [Function("empty")]
    private static BinaryExpression Empty(ParameterScope parameter, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(parameter);
        return Expression.Equal(target, Expression.Constant(string.Empty));
    }

    /// <summary>
    /// Returns true if the property is null: <c>Name.isnull()</c>
    /// </summary>
    [Function("isnull")]
    private static BinaryExpression IsNull(ParameterScope parameter, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(parameter);
        return Expression.Equal(target, Expression.Constant(null));
    }

    /// <summary>
    /// Converts a string to lower case: <c>Name.lower()</c>
    /// </summary>
    [Function("lower")]
    private static MethodCallExpression Lower(ParameterScope parameter, SyntaxRoot identifier)
    {
        var target = identifier.ToExpression(parameter);
        EnsureTargetIsString(target);
        return Expression.Call(target, ReflectionCache.ToLower);
    }

    /// <summary>
    /// Converts a string to upper case: <c>Name.upper()</c>
    /// </summary>
    [Function("upper")]
    private static MethodCallExpression Upper(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        EnsureTargetIsString(target);
        return Expression.Call(target, ReflectionCache.ToUpper);
    }

    /// <summary>
    /// Returns a DateTime object representing the current date and time: <c>now()</c>
    /// </summary>
    [Function("now")]
    private static MemberExpression Now()
    {
        return Expression.MakeMemberAccess(null, typeof(DateTime).GetProperty("Now")!);
    }

    /// <summary>
    /// Returns a DateTime object representing the current date and time in UTC: <c>utcNow()</c>
    /// </summary>
    [Function("utcNow")]
    private static MemberExpression UtcNow()
    {
        return Expression.MakeMemberAccess(null, typeof(DateTime).GetProperty("UtcNow")!);
    }

    /// <summary>
    /// Converts a value to a long: <c>SomeProp.long()</c>
    /// </summary>
    [Function("long")]
    public static UnaryExpression Long(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(long));
    }

    /// <summary>
    /// Converts a value to an unsigned long: <c>SomeProp.ulong()</c>
    /// </summary>
    [Function("ulong")]
    public static UnaryExpression ULong(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(ulong));
    }

    /// <summary>
    /// Converts a value to an int: <c>SomeProp.int()</c>
    /// </summary>
    [Function("int")]
    public static UnaryExpression Int(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(int));
    }

    /// <summary>
    /// Converts a value to an int: <c>SomeProp.uint()</c>
    /// </summary>
    [Function("uint")]
    public static UnaryExpression UInt(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(uint));
    }

    /// <summary>
    /// Converts a value to a short: <c>SomeProp.short()</c>
    /// </summary>
    [Function("short")]
    public static UnaryExpression Short(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(short));
    }

    /// <summary>
    /// Converts a value to an ushort: <c>SomeProp.ushort()</c>
    /// </summary>
    [Function("ushort")]
    public static UnaryExpression UShort(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(ushort));
    }

    /// <summary>
    /// Converts a value to a byte: <c>SomeProp.byte()</c>
    /// </summary>
    [Function("byte")]
    public static UnaryExpression Byte(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(byte));
    }

    /// <summary>
    /// Converts a value to an sbyte: <c>SomeProp.sbyte()</c>
    /// </summary>
    [Function("sbyte")]
    public static UnaryExpression SByte(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(sbyte));
    }

    /// <summary>
    /// Converts a value to a decimal: <c>SomeProp.decimal()</c>
    /// </summary>
    [Function("decimal")]
    public static UnaryExpression Decimal(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(decimal));
    }

    /// <summary>
    /// Converts a value to an double: <c>SomeProp.double()</c>
    /// </summary>
    [Function("double")]
    public static UnaryExpression Double(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(double));
    }

    /// <summary>
    /// Converts a value to an float: <c>SomeProp.float()</c>
    /// </summary>
    [Function("float")]
    public static UnaryExpression Float(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);
        return Expression.Convert(target, typeof(float));
    }

    /// <summary>
    /// Converts a value to an bool: <c>SomeProp.bool()</c>
    /// </summary>
    [Function("bool")]
    public static Expression Bool(ParameterScope parameter, SyntaxRoot value)
    {
        var target = value.ToExpression(parameter);

        return target switch
        {
            { } when target.Type == typeof(bool) => target,
            { } when target.Type == typeof(int) => Expression.NotEqual(target, Expression.Constant(0)),
            { } when target.Type == typeof(long) => Expression.NotEqual(target, Expression.Constant(0L)),
            { } when target.Type == typeof(double) => Expression.NotEqual(target, Expression.Constant(0d)),
            { } when target.Type == typeof(float) => Expression.NotEqual(target, Expression.Constant(0f)),
            { } when target.Type == typeof(decimal) => Expression.NotEqual(target, Expression.Constant(0m)),
            { } when target.Type == typeof(byte) => Expression.NotEqual(target, Expression.Constant((byte)0)),
            { } when target.Type == typeof(sbyte) => Expression.NotEqual(target, Expression.Constant((sbyte)0)),
            { } when target.Type == typeof(short) => Expression.NotEqual(target, Expression.Constant((short)0)),
            { } when target.Type == typeof(ushort) => Expression.NotEqual(target, Expression.Constant((ushort)0)),
            { } when target.Type == typeof(uint) => Expression.NotEqual(target, Expression.Constant((uint)0)),
            { } when target.Type == typeof(ulong) => Expression.NotEqual(target, Expression.Constant((ulong)0)),
            { } when target.Type == typeof(char) => Expression.NotEqual(target, Expression.Constant('\0')),
            { } when target.Type == typeof(DateTime) => Expression.NotEqual(target, Expression.Constant(default(DateTime))),
            { } when target.Type == typeof(DateTimeOffset) => Expression.NotEqual(target, Expression.Constant(default(DateTimeOffset))),
            { } when target.Type == typeof(TimeSpan) => Expression.NotEqual(target, Expression.Constant(default(TimeSpan))),
            { } when target.Type == typeof(Guid) => Expression.NotEqual(target, Expression.Constant(default(Guid))),
            null => Expression.Constant(false),
            _ => throw new InvalidOperationException($"Unable to convert {target.Type} to bool")
        };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Target}({string.Join(", ", Arguments)})";
    }

    private static void EnsureTargetIsString(Expression target)
    {
        if (target.Type != typeof(string))
        {
            throw new InvalidOperationException($"Property {target} is not a string");
        }
    }

    private static T EnsureConstType<T>(ConstSyntax arg)
    {
        return arg.Value is not T
            ? throw new InvalidOperationException($"Expected {typeof(T).Name}, got {arg.Value?.GetType().Name ?? "null"}")
            : (T)arg.Value!;
    }

    private static void EnsureTargetIsCollection(Expression target)
    {
        if (!target.Type.IsCollection())
        {
            throw new InvalidOperationException($"Property {target} is not a collection");
        }
    }

    private static LambdaExpression CreateLambdaArgument(Type argumentType, LambdaSyntax lambda, ParameterScope scope)
    {
        using (scope.EnterScope(lambda.Identifier!.Name, argumentType))
        {
            return (LambdaExpression)lambda.ToExpression(scope);
        }
    }
}