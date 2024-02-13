using System.Diagnostics.CodeAnalysis;

namespace QueryFilter.Tokens;

//
// Summary:
//     Static helper class for operators
internal static class Operators
{
    //
    // Summary:
    //     Internal dictionary of operators
    private static readonly Dictionary<string, Token> _operators = new Dictionary<string, Token>
    {
        ["eq"] = new Token(TokenKind.Equals, "eq"),
        ["neq"] = new Token(TokenKind.NotEquals, "neq"),
        ["or"] = new Token(TokenKind.Or, "or"),
        ["and"] = new Token(TokenKind.And, "and"),
        ["not"] = new Token(TokenKind.Not, "not"),
        ["gt"] = new Token(TokenKind.GreaterThan, "gt"),
        ["lt"] = new Token(TokenKind.LessThan, "lt"),
        ["gte"] = new Token(TokenKind.GreaterThanOrEqual, "gte"),
        ["lte"] = new Token(TokenKind.LessThanOrEqual, "lte"),
        ["("] = new Token(TokenKind.OpenParen, "("),
        [")"] = new Token(TokenKind.CloseParen, ")"),
        ["."] = new Token(TokenKind.Dot, "."),
        [":"] = new Token(TokenKind.Colon, ":"),
        [","] = new Token(TokenKind.Comma, ","),
        ["+"] = new Token(TokenKind.Plus, "+"),
        ["-"] = new Token(TokenKind.Minus, "-"),
        ["*"] = new Token(TokenKind.Asterisk, "*"),
        ["/"] = new Token(TokenKind.Slash, "/"),
        ["%"] = new Token(TokenKind.Percent, "%")
    };

    //
    // Summary:
    //     Try to get an operator from the given identifier, returning true if it exists
    public static bool TryGetOperator(string op, [NotNullWhen(true)] out Token token)
    {
        return _operators.TryGetValue(op, out token);
    }
}
