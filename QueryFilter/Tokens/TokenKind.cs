namespace WebApplication1.QueryFilter.Tokens;

//
// Summary:
//     A list of all possible token kinds
internal enum TokenKind
{
    //
    // Summary:
    //     Represents a true bool value
    True,
    //
    // Summary:
    //     Represents a false bool value
    False,
    //
    // Summary:
    //     Represents an integer value
    Integer,
    //
    // Summary:
    //     Represents a decimal value
    Number,
    //
    // Summary:
    //     A null value
    Null,
    //
    // Summary:
    //     Represents a string value
    String,
    //
    // Summary:
    //     Represents an identifier
    Identifier,
    //
    // Summary:
    //     Represents the eq operator
    Equals,
    //
    // Summary:
    //     Represents the neq operator
    NotEquals,
    //
    // Summary:
    //     Represents the gt operator
    GreaterThan,
    //
    // Summary:
    //     Represents the lt operator
    LessThan,
    //
    // Summary:
    //     Represents the gte operator
    GreaterThanOrEqual,
    //
    // Summary:
    //     Represents the lte operator
    LessThanOrEqual,
    //
    // Summary:
    //     Represents the and operator
    And,
    //
    // Summary:
    //     Represents the or operator
    Or,
    //
    // Summary:
    //     Represents the not operator
    Not,
    //
    // Summary:
    //     Represents the dot operator
    Dot,
    //
    // Summary:
    //     Represents an open parenthesis
    OpenParen,
    //
    // Summary:
    //     Represents a close parenthesis
    CloseParen,
    //
    // Summary:
    //     Represents a comma
    Comma,
    //
    // Summary:
    //     Represents a colon
    Colon,
    //
    // Summary:
    //     Represents a plus sign
    Plus,
    //
    // Summary:
    //     Represents a minus sign
    Minus,
    //
    // Summary:
    //     Represents an asterisk
    Asterisk,
    //
    // Summary:
    //     Represents a slash
    Slash,
    //
    // Summary:
    //     Represents a percent sign
    Percent
}
