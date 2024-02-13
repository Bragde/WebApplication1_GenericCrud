using System.Diagnostics.CodeAnalysis;
using QueryFilter.Extensions;

namespace QueryFilter.Tokens;

//
// Summary:
//     Represents a forward-only stream of tokens
internal sealed class TokenStream
{
    private readonly Token[] _tokens;

    private int ptr;

    //
    // Summary:
    //     Returns true if no more tokens are available to process
    internal bool Empty => ptr >= _tokens.Length;

    //
    // Summary:
    //     Given a list of token, initialze the stream
    public TokenStream(IEnumerable<Token> tokens)
    {
        _tokens = tokens.ToArray();
    }

    //
    // Summary:
    //     Consume the next token, if one exists. If the stream is empty an exception will
    //     be thrown
    //
    // Exceptions:
    //   T:System.Exception:
    public Token Next()
    {
        if (!Empty)
        {
            return _tokens[ptr++];
        }

        throw new EndOfStreamException("Unexpected end of filter expression");
    }

    //
    // Summary:
    //     Throws an exception if the next token is not the expected token
    public Token Expect(TokenKind token)
    {
        if (Empty)
        {
            throw new EndOfStreamException($"Unexpected end of filter expression looking for {token}");
        }

        if (_tokens[ptr].Kind != token)
        {
            throw new ArgumentException($"Expected {token} but found {_tokens[ptr]}");
        }

        return _tokens[ptr++];
    }

    //
    // Summary:
    //     Attempts to match a sequence of tokens. If the sequence is matched, the tokens
    //     are consumed and the method returns true
    public bool TryMatchSequence(List<TokenKind> predicates, out Span<Token> matched)
    {
        matched = Span<Token>.Empty;
        if (predicates.Count + ptr >= _tokens.Length)
        {
            return false;
        }

        if (predicates.All((p, i) => p == _tokens[ptr + i].Kind))
        {
            matched = _tokens.AsSpan().Slice(ptr, predicates.Count);
            ptr += predicates.Count;
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     Try to match the next token given a token kind. If more tokens exist and the
    //     next token matches, consume the token and return true
    public bool TryMatch(TokenKind kind)
    {
        if (Empty || _tokens[ptr].Kind != kind)
        {
            return false;
        }

        ptr++;
        return true;
    }

    //
    // Summary:
    //     Try to match the next token given a token kind. If more tokens exist and the
    //     next token matches, consume the token and assign it to the out parameter token,
    //     and return true
    public bool TryMatch(TokenKind kind, [NotNullWhen(true)] out Token token)
    {
        token = Token.None;
        if (Empty || _tokens[ptr].Kind != kind)
        {
            return false;
        }

        token = _tokens[ptr++];
        return true;
    }

    public bool TryMatch(TokenKind[] kinds, [NotNullWhen(true)] out Token token)
    {
        token = Token.None;
        if (Empty || !kinds.Contains(_tokens[ptr].Kind))
        {
            return false;
        }

        token = _tokens[ptr++];
        return true;
    }
}
