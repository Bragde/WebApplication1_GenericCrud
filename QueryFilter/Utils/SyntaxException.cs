namespace QueryFilter.Utils;

//
// Summary:
//     Exception thrown when the syntax is invalid
public sealed class SyntaxException : Exception
{
    //
    // Summary:
    //     Initializes a new instance of the Dekiru.QueryFilter.Utils.SyntaxException class.
    public SyntaxException()
    {
    }

    //
    // Summary:
    //     Initializes a new instance of the Dekiru.QueryFilter.Utils.SyntaxException class.
    public SyntaxException(string message)
        : base(message)
    {
    }

    //
    // Summary:
    //     Initializes a new instance of the Dekiru.QueryFilter.Utils.SyntaxException class.
    public SyntaxException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
