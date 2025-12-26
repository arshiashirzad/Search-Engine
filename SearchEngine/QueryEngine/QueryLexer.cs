using System.Text;
using System.Text.RegularExpressions;

namespace SearchEngine.QueryEngine;

public class QueryLexer
{
    private readonly string _input;
    private int _position;
    private readonly List<QueryToken> _tokens;

    private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        "AND", "OR", "NOT", "NEAR"
    };

    private static readonly HashSet<string> ValidFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "content", "filename"
    };

    public QueryLexer(string input)
    {
        _input = input ?? string.Empty;
        _position = 0;
        _tokens = new List<QueryToken>();
    }

    public List<QueryToken> Tokenize()
    {
        _tokens.Clear();
        _position = 0;

        while (_position < _input.Length)
        {
            SkipWhitespace();
            if (_position >= _input.Length) break;

            var token = ReadNextToken();
            if (token != null)
            {
                _tokens.Add(token);
            }
        }

        _tokens.Add(new QueryToken(QueryTokenType.EndOfInput, "", _position));
        return _tokens;
    }

    private void SkipWhitespace()
    {
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
        {
            _position++;
        }
    }

    private QueryToken? ReadNextToken()
    {
        var startPos = _position;
        var c = _input[_position];

        if (c == '(')
        {
            _position++;
            return new QueryToken(QueryTokenType.LeftParen, "(", startPos);
        }

        if (c == ')')
        {
            _position++;
            return new QueryToken(QueryTokenType.RightParen, ")", startPos);
        }

        if (c == '"')
        {
            return ReadPhrase();
        }

        return ReadWord();
    }

    private QueryToken ReadPhrase()
    {
        var startPos = _position;
        _position++;

        var sb = new StringBuilder();
        while (_position < _input.Length && _input[_position] != '"')
        {
            sb.Append(_input[_position]);
            _position++;
        }

        if (_position < _input.Length)
        {
            _position++;
        }

        return new QueryToken(QueryTokenType.Phrase, sb.ToString().Trim(), startPos);
    }

    private QueryToken ReadWord()
    {
        var startPos = _position;
        var sb = new StringBuilder();
        var hasWildcard = false;
        var colonPos = -1;

        while (_position < _input.Length)
        {
            var c = _input[_position];

            if (char.IsWhiteSpace(c) || c == '(' || c == ')' || c == '"')
            {
                break;
            }

            if (c == '*' || c == '?')
            {
                hasWildcard = true;
            }

            if (c == ':' && colonPos == -1)
            {
                colonPos = sb.Length;
            }

            sb.Append(c);
            _position++;
        }

        var word = sb.ToString();

        if (word.StartsWith("NEAR/", StringComparison.OrdinalIgnoreCase) ||
            word.StartsWith("NEAR\\", StringComparison.OrdinalIgnoreCase))
        {
            var distStr = word.Substring(5);
            if (int.TryParse(distStr, out var distance))
            {
                return new QueryToken(QueryTokenType.Near, word, startPos)
                {
                    ProximityDistance = distance
                };
            }
        }

        if (Operators.Contains(word))
        {
            return word.ToUpperInvariant() switch
            {
                "AND" => new QueryToken(QueryTokenType.And, "AND", startPos),
                "OR" => new QueryToken(QueryTokenType.Or, "OR", startPos),
                "NOT" => new QueryToken(QueryTokenType.Not, "NOT", startPos),
                "NEAR" => new QueryToken(QueryTokenType.Near, "NEAR", startPos) { ProximityDistance = 1 },
                _ => new QueryToken(QueryTokenType.Term, word.ToLowerInvariant(), startPos)
            };
        }

        if (colonPos > 0 && colonPos < word.Length - 1)
        {
            var fieldName = word.Substring(0, colonPos);
            var termValue = word.Substring(colonPos + 1);

            if (ValidFields.Contains(fieldName))
            {
                return new QueryToken(QueryTokenType.Field, termValue.ToLowerInvariant(), startPos)
                {
                    FieldName = fieldName.ToLowerInvariant()
                };
            }
        }

        if (hasWildcard)
        {
            return new QueryToken(QueryTokenType.Wildcard, word.ToLowerInvariant(), startPos);
        }

        return new QueryToken(QueryTokenType.Term, word.ToLowerInvariant(), startPos);
    }
}
