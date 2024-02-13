namespace QueryFilter.Syntax;

/// <summary>
/// Types of binary operation
/// </summary>
internal enum BinaryType
{
    /// <summary>
    /// A or B
    /// </summary>
    Or = 1 << 0,
    /// <summary>
    /// A and B
    /// </summary>
    And = 1 << 1,
    /// <summary>
    /// A == B
    /// </summary>
    Equal = 1 << 2,
    /// <summary>
    /// A != B
    /// </summary>
    NotEqual = 1 << 3,
    /// <summary>
    /// A > B
    /// </summary>
    GreaterThan = 1 << 4,
    /// <summary>
    /// A &lt; B
    /// </summary>
    LessThan = 1 << 5,
    /// <summary>
    /// A >= B
    /// </summary>
    GreaterThanEqual = 1 << 6,
    /// <summary>
    /// A &lt;= B
    /// </summary>
    LessThanEqual = 1 << 7,
    /// <summary>
    /// A + B
    /// </summary>
    Add = 1 << 8,
    /// <summary>
    /// A - B
    /// </summary>
    Subtract = 1 << 9,
    /// <summary>
    /// A * B
    /// </summary>
    Multiply = 1 << 10,
    /// <summary>
    /// A / B
    /// </summary>
    Divide = 1 << 11,
    /// <summary>
    /// A % B
    /// </summary>
    Modulo = 1 << 12,
}

