using WebApplication1.QueryFilter.Syntax;
using WebApplication1.QueryFilter.Tokens;

namespace WebApplication1.QueryFilter;

internal static class SyntaxParser
{
    private static readonly List<TokenKind> lambdaSequence = new List<TokenKind>
    {
        TokenKind.Identifier,
        TokenKind.Colon
    };

    private static readonly TokenKind[] equalityTokens = new TokenKind[2]
    {
        TokenKind.Equals,
        TokenKind.NotEquals
    };

    private static readonly TokenKind[] comparisonTokens = new TokenKind[4]
    {
        TokenKind.GreaterThan,
        TokenKind.LessThan,
        TokenKind.GreaterThanOrEqual,
        TokenKind.LessThanOrEqual
    };

    private static readonly TokenKind[] additiveTokens = new TokenKind[2]
    {
        TokenKind.Plus,
        TokenKind.Minus
    };

    private static readonly TokenKind[] multiplicativeTokens = new TokenKind[3]
    {
        TokenKind.Asterisk,
        TokenKind.Slash,
        TokenKind.Percent
    };

    private static readonly TokenKind[] memberAccessOrCallTokens = new TokenKind[2]
    {
        TokenKind.Dot,
        TokenKind.OpenParen
    };

    //
    // Summary:
    //     Parses a syntax tree from the given stream
    internal static SyntaxRoot Parse(TokenStream tokens)
    {
        return Lambda(tokens);
    }

    //
    // Summary:
    //     Parses a lambda expression
    private static SyntaxRoot Lambda(TokenStream tokens)
    {
        if (!tokens.TryMatchSequence(lambdaSequence, out var matched))
        {
            return Or(tokens);
        }

        return new LambdaSyntax(new IdentifierSyntax(matched[0].AsString()), Or(tokens));
    }

    //
    // Summary:
    //     Helper method to parse a binary expression
    private static SyntaxRoot Binary(TokenStream tokens, TokenKind[] operators, Func<Token, BinaryType> typeSelector, Func<TokenStream, SyntaxRoot> next)
    {
        SyntaxRoot syntaxRoot = next(tokens);
        Token token;
        while (tokens.TryMatch(operators, out token))
        {
            syntaxRoot = new BinarySyntax(typeSelector(token), syntaxRoot, next(tokens));
        }

        return syntaxRoot;
    }

    //
    // Summary:
    //     Helper method to parse a binary expression
    private static SyntaxRoot Binary(TokenStream tokens, TokenKind @operator, BinaryType type, Func<TokenStream, SyntaxRoot> next)
    {
        SyntaxRoot syntaxRoot = next(tokens);
        while (tokens.TryMatch(@operator))
        {
            syntaxRoot = new BinarySyntax(type, syntaxRoot, next(tokens));
        }

        return syntaxRoot;
    }

    //
    // Summary:
    //     Parses an or expression
    private static SyntaxRoot Or(TokenStream tokens)
    {
        return Binary(tokens, TokenKind.Or, BinaryType.Or, And);
    }

    //
    // Summary:
    //     Parses an and expression
    private static SyntaxRoot And(TokenStream tokens)
    {
        return Binary(tokens, TokenKind.And, BinaryType.And, EqNeq);
    }

    //
    // Summary:
    //     Parses an equality expression (eq , neq)
    private static SyntaxRoot EqNeq(TokenStream tokens)
    {
        return Binary(tokens, equalityTokens, (Token op) => (op.Kind != TokenKind.Equals) ? BinaryType.NotEqual : BinaryType.Equal, Relational);
    }

    //
    // Summary:
    //     Parses a relational expression (gt, lt, gte, lte)
    private static SyntaxRoot Relational(TokenStream tokens)
    {
        return Binary(tokens, comparisonTokens, (Token op) => op.RawValue switch
        {
            "gt" => BinaryType.GreaterThan,
            "lt" => BinaryType.LessThan,
            "gte" => BinaryType.GreaterThanEqual,
            "lte" => BinaryType.LessThanEqual,
            _ => throw new NotImplementedException(),
        }, Additive);
    }

    private static SyntaxRoot Additive(TokenStream tokens)
    {
        return Binary(tokens, additiveTokens, delegate (Token op)
        {
            string rawValue = op.RawValue;
            if (rawValue == "+")
            {
                return BinaryType.Add;
            }

            if (!(rawValue == "-"))
            {
                throw new NotImplementedException();
            }

            return BinaryType.Subtract;
        }, Multiplicative);
    }

    private static SyntaxRoot Multiplicative(TokenStream tokens)
    {
        return Binary(tokens, multiplicativeTokens, (Token op) => op.RawValue switch
        {
            "*" => BinaryType.Multiply,
            "/" => BinaryType.Divide,
            "%" => BinaryType.Modulo,
            _ => throw new NotImplementedException(),
        }, Not);
    }

    //
    // Summary:
    //     Parses a not expression
    private static SyntaxRoot Not(TokenStream tokens)
    {
        if (!tokens.TryMatch(TokenKind.Not))
        {
            if (!tokens.TryMatch(TokenKind.Minus))
            {
                return MemberOrCall(tokens);
            }

            return new UnarySyntax(UnaryType.Negate, Not(tokens));
        }

        return new UnarySyntax(UnaryType.Not, Not(tokens));
    }

    //
    // Summary:
    //     Parses a member access or call expression
    private static SyntaxRoot MemberOrCall(TokenStream tokens)
    {
        SyntaxRoot syntaxRoot = Const(tokens);
        Token token;
        while (tokens.TryMatch(memberAccessOrCallTokens, out token))
        {
            if (token.Kind == TokenKind.OpenParen)
            {
                List<SyntaxRoot> list = new List<SyntaxRoot>();
                while (!tokens.TryMatch(TokenKind.CloseParen))
                {
                    list.Add(Lambda(tokens));
                    if (!tokens.TryMatch(TokenKind.Comma))
                    {
                        tokens.Expect(TokenKind.CloseParen);
                        break;
                    }
                }

                syntaxRoot = new CallSyntax(syntaxRoot, list);
            }
            else
            {
                syntaxRoot = new MemberAccessSyntax(Identifier(tokens).Name, syntaxRoot);
            }
        }

        if (syntaxRoot is IdentifierSyntax identifierSyntax)
        {
            return new MemberAccessSyntax(identifierSyntax.Name, null);
        }

        return syntaxRoot;
    }

    private static IdentifierSyntax Identifier(TokenStream tokens)
    {
        return new IdentifierSyntax(tokens.Expect(TokenKind.Identifier).AsString());
    }

    private static SyntaxRoot Const(TokenStream tokens)
    {
        if (tokens.TryMatch(TokenKind.OpenParen))
        {
            SyntaxRoot result = Lambda(tokens);
            tokens.Expect(TokenKind.CloseParen);
            return result;
        }

        Token value = tokens.Next();
        return value.Kind switch
        {
            TokenKind.Number => new ConstSyntax(value.AsNumber()),
            TokenKind.Integer => new ConstSyntax(value.AsInt32()),
            TokenKind.String => new ConstSyntax(value.AsString()),
            TokenKind.True => new ConstSyntax(true),
            TokenKind.False => new ConstSyntax(false),
            TokenKind.Identifier => new IdentifierSyntax(value.AsString()),
            TokenKind.Null => ConstSyntax.Null,
            _ => throw new InvalidOperationException($"Unexpected token: {value}"),
        };
    }
}
