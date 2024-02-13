namespace WebApplication1.QueryFilter.Utils;

/// <summary>
/// Helps to map a method to a function name.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class FunctionAttribute : Attribute
{
    /// <summary>
    /// Function name.
    /// </summary>
    public string Name { get; }

    public FunctionAttribute(string name)
    {
        Name = name;
    }
}