namespace WebApplication1.QueryFilter.Tokens;

internal readonly struct Token
{
    //
    // Summary:
    //     Describes the kind of the token
    public readonly TokenKind Kind;

    //
    // Summary:
    //     The value of the token, if any
    private readonly object value;

    //
    // Summary:
    //     Represents a null value
    public static Token Null = new Token(TokenKind.Null, "null");

    public static Token None = new Token(TokenKind.True, "None");

    public string RawValue => value?.ToString() ?? "";

    //
    // Summary:
    //     Constructs a new token instance
    public Token(TokenKind kind, object value)
    {
        Kind = kind;
        this.value = value;
    }

    public override string ToString()
    {
        return $"{Kind}({value})";
    }

    //
    // Summary:
    //     Casts the value to an integer
    public int AsInt32()
    {
        return (int)value;
    }

    //
    // Summary:
    //     Casts the value to a string
    public string AsString()
    {
        return (string)value;
    }

    public decimal AsNumber()
    {
        return (decimal)value;
    }
}
