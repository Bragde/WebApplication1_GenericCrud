using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using WebApplication1.QueryFilter.Extensions;

namespace WebApplication1.QueryFilter.Utils;

/// <summary>
/// Helper class to manage type conversions
/// </summary>
internal static class TypeConverter
{
    /// <summary>
    /// Ensures that the two expressions are the same type, or have conversions to make them the same type. The method ensures that we always convert the smaller or less precise type to the bigger, to avoid loss of data
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown if either expression represents a type we don't know how to convert</exception>
    internal static (Expression left, Expression right) NormalizeTypes(Expression left, Expression right)
    {
        var leftType = left.Type;
        var rightType = right.Type;

        // If the types are the same, or both are null, just return them as-is
        if (leftType == rightType || (left.IsNull() && right.IsNull()))
        {
            return (left, right);
        }

        if (left.IsNull())
        {
            return (left.Convert(rightType), right);
        }

        if (right.IsNull())
        {
            return (left, right.Convert(leftType));
        }

        // If either type is an enum we're gonna have to do a bit of conversion
        // magic, so we'll just handle that in its own place
        if (leftType.IsEnum || rightType.IsEnum)
        {
            return NormalizeEnums(left, leftType, right, rightType);
        }

        // The query handling can't differentiate between a string and date, so when the data reaches this point,
        // it is quite likely that one type will be string and the other a date type. If we find this, apply the
        // conversion for that
        if ((leftType.IsDateType() && rightType.Is<string>()) || (leftType.Is<string>() && rightType.IsDateType()))
        {
            return HandleDateConversion(left, leftType, right, rightType);
        }

        var leftPrio = GetPrecedence(leftType);
        var rightPrio = GetPrecedence(rightType);

        // If this happens, then we've yet to handle this type, so we'll just crash happily
        if (leftPrio == -1 || rightPrio == -1)
        {
            throw new NotImplementedException($"Type conversion between {leftType.FullName} and {rightType.FullName} is not implemented");
        }

        // We have two different types with the same priority, we're dealing with a signed and unsigned
        // variant of the same type, so we'll just convert both to a bigger type
        if (leftPrio == rightPrio)
        {
            if (leftType == typeof(long) || leftType == typeof(ulong))
            {
                throw new ArithmeticException("No implicit conversion between long and ulong exists, use ulong() or long() to explicitly convert");
            }

            var largerType = GetLargerType(leftType);

            return (Expression.Convert(left, largerType), Expression.Convert(right, largerType));
        }

        // Depending on the priority, apply the conversion (leftPrio and rightPrio) should never be equal
        return leftPrio < rightPrio ?
            (left, right.Convert(leftType)) :
            (left.Convert(rightType), right);
    }

    /// <summary>
    /// Wrapper method to handle enum conversions, see <see cref="ConvertEnum(Expression, Type)"/> for the actual implementation
    /// </summary>
    internal static (Expression left, Expression right) NormalizeEnums(Expression left, Type leftType, Expression right, Type rightType)
    {
        // We should only reach this point if one side is an enum, but not both.
        // So, find the enum side and attempt to convert the other side
        return leftType.IsEnum ?
            (left, ConvertEnum(right, leftType)) :
            (ConvertEnum(left, rightType), right);
    }

    /// <summary>
    /// If <paramref name="type"/> is a value type, creates a constant of the default value of <paramref name="type"/>, otherwise converts <paramref name="null"/> to <paramref name="type"/>
    /// </summary>
    internal static Expression HandleNullConversion(Expression @null, Type type)
    {
        return type.IsValueType ? Expression.Constant(Activator.CreateInstance(type)) : Expression.Convert(@null, type);
    }

    /// <summary>
    /// Wrapper method to handle the date conversions, see <see cref="ConvertDate(Expression, Type, Type)"/> for the actual implementation
    /// </summary>
    internal static (Expression left, Expression right) HandleDateConversion(Expression left, Type leftType, Expression right, Type rightType)
    {
        return (ConvertDate(left, leftType, rightType), ConvertDate(right, rightType, leftType));
    }

    /// <summary>
    /// This is a list, in descending order of precedence, of how to convert types.
    /// I.e., if we're comparing a double (a) and an int (b), the comparison would be a == (double)b,
    /// while if we had an int (a) and a long(b), the comparison would be (long)a == b.
    /// </summary>
    private static readonly Dictionary<Type, int> _typePrecedence = new()
    {
        [typeof(string)] = 0,
        [typeof(decimal)] = 1,
        [typeof(double)] = 2,
        [typeof(float)] = 3,
        [typeof(long)] = 4,
        [typeof(ulong)] = 4,
        [typeof(int)] = 5,
        [typeof(uint)] = 5,
        [typeof(short)] = 6,
        [typeof(ushort)] = 6,
        [typeof(byte)] = 7,
        [typeof(sbyte)] = 7,
        [typeof(char)] = 8,
        [typeof(bool)] = 9
    };

    private static readonly Dictionary<Type, Type> _commonLargerType = new()
    {
        [typeof(byte)] = typeof(short),
        [typeof(sbyte)] = typeof(short),
        [typeof(short)] = typeof(int),
        [typeof(ushort)] = typeof(int),
        [typeof(int)] = typeof(long),
        [typeof(uint)] = typeof(long),
    };

    /// <summary>
    /// Gets the precedence of the given type, will return -1 if the type is not classified
    /// </summary>
    private static int GetPrecedence(Type t)
    {
        return _typePrecedence.TryGetValue(t, out var prio) ? prio : -1;
    }

    private static Type GetLargerType(Type t)
    {
        return _commonLargerType[t];
    }

    /// <summary>
    /// Cache of <see cref="Enum.Parse(Type, string, bool)"/>, to speed up conversions. We use the version of Parse that's case-insensitive
    /// </summary>
    private static readonly MethodInfo _parseEnum = typeof(Enum).GetMethod(
        "ParseFilter",
        BindingFlags.Static | BindingFlags.Public,
#if NET8_0
        [typeof(Type), typeof(string), typeof(bool)]
#else
        new[] { typeof(Type), typeof(string), typeof(bool) }
#endif
    )!;

    /// <summary>
    /// Converts the given expression <paramref name="value"/> to an enum of type <paramref name="type"/>. Handled by calling <see cref="Enum.Parse(Type, string, bool)"/>, with a case-insensitive conversion
    /// </summary>
    private static UnaryExpression ConvertEnum(Expression value, Type type)
    {
        // TODO: Investigate enum conversions more
        // We're lazily assuming that the value is going to be a string constant
        // Given an enum type, say "Foo", and a string variable "value", this is
        // the equivalent to going (Foo)Enum.Parse(typeof(Foo), value, true)
        return Expression.Convert(
            Expression.Call(
                _parseEnum,
                Expression.Constant(type),
                value,
                Expression.Constant(true)),
            type);
    }

    /// <summary>
    /// A lookyp table of various Date/Time types and their conversion methods
    /// </summary>
    private static readonly Dictionary<Type, MethodInfo> _dateTimeConverters = new()
    {
        [typeof(DateTime)] = ReflectionCache.GetMethodInfo(() => ParseDateTime("")),
        [typeof(DateTimeOffset)] = ReflectionCache.GetMethodInfo(() => ParseDateTimeOffset("")),
        [typeof(DateOnly)] = ReflectionCache.GetMethodInfo(() => ParseDateOnly("")),
        [typeof(TimeOnly)] = ReflectionCache.GetMethodInfo(() => ParseTimeOnly(""))
    };

    /// <summary>
    /// Wrapper for <see cref="DateTime.Parse(string, IFormatProvider?)"/> with <see cref="CultureInfo.InvariantCulture"/>
    /// </summary>
    private static DateTime ParseDateTime(string s)
    {
        return DateTime.Parse(s, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Wrapper for <see cref="DateTimeOffset.Parse(string, IFormatProvider?)"/> with <see cref="CultureInfo.InvariantCulture"/>
    /// </summary>
    private static DateTimeOffset ParseDateTimeOffset(string s)
    {
        return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Wrapper for <see cref="DateOnly.Parse(string, IFormatProvider?, DateTimeStyles)"/> with <see cref="CultureInfo.InvariantCulture"/>
    /// </summary>
    private static DateOnly ParseDateOnly(string s)
    {
        return DateOnly.Parse(s, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Wrapper for <see cref="TimeOnly.Parse(string, IFormatProvider?, DateTimeStyles)"/> with <see cref="CultureInfo.InvariantCulture"/>
    /// </summary>
    private static TimeOnly ParseTimeOnly(string s)
    {
        return TimeOnly.Parse(s, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Wraps the <paramref name="value"/> in a converstion to <paramref name="targetType"/>, assuming that <paramref name="currentType"/> is a string. Otherwise, the <paramref name="value"/> is returned as-is
    /// </summary>
    private static Expression ConvertDate(Expression value, Type currentType, Type targetType)
    {
        // If we've got anything other than a string, just don't do anything. It's unlikely that
        // we'll be able to succeed so let it crash someplace else
        return !currentType.Is<string>() ? value :
            Expression.Call(_dateTimeConverters[targetType], value);
    }
}