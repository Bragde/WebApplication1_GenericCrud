using System.Globalization;
using System.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using QueryFilter.Extensions;
using QueryFilter.Utils;

namespace QueryFilter.Tokens;

internal static class Tokenizer
{
    private static readonly Dictionary<string, string> errorHints = new Dictionary<string, string>
    {
        [">"] = "Did you intend to use 'gt'?",
        ["<"] = "Did you intend to use 'lt'?",
        [">="] = "Did you intend to use 'gte'?",
        ["<="] = "Did you intend to use 'lte'?",
        ["="] = "Did you intend to use 'eq'?",
        ["!="] = "Did you intend to use 'neq'?",
        ["!"] = "Did you intend to use 'not'?",
        ["||"] = "Did you intend to use 'or'?",
        ["&&"] = "Did you intend to use 'and'?"
    };

    internal static TokenStream Read(string source)
    {
        List<Token> list = new List<Token>();
        ReadOnlySpan<char> q = source.AsSpan();
        int ptr = 0;
        StringBuilder stringBuilder = new StringBuilder();
        while (ptr < q.Length)
        {
            char c = q[ptr++];
            if (char.IsWhiteSpace(c))
            {
                if (stringBuilder.Length > 0)
                {
                    list.Add(ParseToken(stringBuilder));
                }

                continue;
            }

            if (stringBuilder.Length == 0 && IsNumberStart(c, q, ptr))
            {
                list.Add(ParseNumber(c, q, ref ptr));
                continue;
            }

            if (Operators.TryGetOperator(c.ToString(), out var token))
            {
                if (stringBuilder.Length > 0)
                {
                    list.Add(ParseToken(stringBuilder));
                }

                list.Add(token);
                continue;
            }

            bool flag;
            switch (c)
            {
                case '[':
                    if (stringBuilder.Length > 0)
                    {
                        list.Add(ParseToken(stringBuilder));
                    }

                    while (ptr < q.Length && q[ptr] != ']')
                    {
                        c = q[ptr++];
                        stringBuilder.Append(c);
                    }

                    if (ptr >= q.Length)
                    {
                        throw new EndOfStreamException("Non-terminated member expression");
                    }

                    ptr++;
                    list.Add(new Token(TokenKind.Identifier, stringBuilder.ToString()));
                    stringBuilder.Clear();
                    continue;
                case '"':
                case '\'':
                    flag = true;
                    break;
                default:
                    flag = false;
                    break;
            }

            if (flag)
            {
                if (stringBuilder.Length > 0)
                {
                    list.Add(ParseToken(stringBuilder));
                }

                char c2 = c;
                while (ptr < q.Length && q[ptr] != c2)
                {
                    c = q[ptr++];
                    if (c == '\\' && ptr < q.Length)
                    {
                        c = q[ptr++].Out(out var outValue) switch
                        {
                            't' => '\t',
                            'n' => '\n',
                            'r' => '\r',
                            '"' => '"',
                            '\'' => '\'',
                            _ => throw new SyntaxException($"Invalid escape sequence: \\{outValue}"),
                        };
                    }

                    stringBuilder.Append(c);
                }

                if (ptr >= q.Length)
                {
                    throw new EndOfStreamException("Non-terminated string constant");
                }

                ptr++;
                list.Add(new Token(TokenKind.String, stringBuilder.ToString()));
                stringBuilder.Clear();
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        if (stringBuilder.Length > 0)
        {
            list.Add(ParseToken(stringBuilder));
        }

        return new TokenStream(list);
    }

    private static Token ParseToken(StringBuilder builder)
    {
        string text = builder.ToString();
        builder.Clear();
        if (Operators.TryGetOperator(text, out var token))
        {
            return token;
        }

        if (text == "null")
        {
            return Token.Null;
        }

        if (bool.TryParse(text, out var result))
        {
            return new Token(!result ? TokenKind.False : TokenKind.True, text);
        }

        return ParseIdentifier(text);
    }

    private static Token ParseIdentifier(string value)
    {
        if (value.Length == 0)
        {
            throw new SyntaxException("Empty identifier");
        }

        if (value.Length == 1 && value[0] == '$')
        {
            return new Token(TokenKind.Identifier, value);
        }

        if (!char.IsLetter(value[0]) && value[0] != '_')
        {
            ThrowInvalidIdentifier(value);
        }

        for (int i = 1; i < value.Length; i++)
        {
            if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
            {
                ThrowInvalidIdentifier(value);
            }
        }

        return new Token(TokenKind.Identifier, value);
    }

    [DoesNotReturn]
    private static void ThrowInvalidIdentifier(string value)
    {
        string text = "Invalid identifier: '" + value + "'";
        if (errorHints.TryGetValue(value, out string value2))
        {
            text = text + ". " + value2;
        }

        throw new SyntaxException(text);
    }

    private static bool IsNumberStart(char c, ReadOnlySpan<char> q, int ptr)
    {
        if (!char.IsDigit(c))
        {
            if (c == '.' && ptr < q.Length)
            {
                return char.IsDigit(q[ptr]);
            }

            return false;
        }

        return true;
    }

    private static Token ParseNumber(char prefix, ReadOnlySpan<char> q, ref int ptr)
    {
        TokenKind tokenKind = prefix == '.' ? TokenKind.Number : TokenKind.Integer;
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(prefix);
        while (ptr < q.Length && (char.IsDigit(q[ptr]) || q[ptr] == '.'))
        {
            stringBuilder.Append(q[ptr++]);
            if (stringBuilder[stringBuilder.Length - 1] == '.')
            {
                tokenKind = TokenKind.Number;
            }
        }

        string s = stringBuilder.ToString();
        object value = tokenKind == TokenKind.Integer ? int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture) : (object)decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        return new Token(tokenKind, value);
    }
}
