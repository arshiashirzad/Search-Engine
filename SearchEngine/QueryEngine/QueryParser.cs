using SearchEngine.Interfaces;

namespace SearchEngine.QueryEngine;

public class QueryParser
{
    private readonly List<QueryToken> _tokens;
    private readonly ITokenizer _tokenizer;
    private int _position;

    public QueryParser(List<QueryToken> tokens, ITokenizer tokenizer)
    {
        _tokens = tokens;
        _tokenizer = tokenizer;
        _position = 0;
    }

    public QueryNode? Parse()
    {
        if (_tokens.Count == 0 || (_tokens.Count == 1 && _tokens[0].Type == QueryTokenType.EndOfInput))
        {
            return null;
        }

        var result = ParseOrExpression();

        while (Current.Type != QueryTokenType.EndOfInput && Current.Type != QueryTokenType.RightParen)
        {
            var right = ParseOrExpression();
            if (right != null)
            {
                result = new AndNode(result!, right);
            }
        }

        return result;
    }

    private QueryToken Current => _position < _tokens.Count
        ? _tokens[_position]
        : new QueryToken(QueryTokenType.EndOfInput, "", 0);

    private QueryToken Peek(int offset = 1) => _position + offset < _tokens.Count
        ? _tokens[_position + offset]
        : new QueryToken(QueryTokenType.EndOfInput, "", 0);

    private void Advance() => _position++;

    private QueryNode? ParseOrExpression()
    {
        var left = ParseAndExpression();
        if (left == null) return null;

        while (Current.Type == QueryTokenType.Or)
        {
            Advance();
            var right = ParseAndExpression();
            if (right == null)
            {
                throw new QueryParseException("Expected expression after OR", Current.Position);
            }
            left = new OrNode(left, right);
        }

        return left;
    }

    private QueryNode? ParseAndExpression()
    {
        var left = ParseNearExpression();
        if (left == null) return null;

        while (Current.Type == QueryTokenType.And)
        {
            Advance();
            var right = ParseNearExpression();
            if (right == null)
            {
                throw new QueryParseException("Expected expression after AND", Current.Position);
            }
            left = new AndNode(left, right);
        }

        return left;
    }

    private QueryNode? ParseNearExpression()
    {
        var left = ParseNotExpression();
        if (left == null) return null;

        while (Current.Type == QueryTokenType.Near)
        {
            var distance = Current.ProximityDistance;
            Advance();
            var right = ParseNotExpression();
            if (right == null)
            {
                throw new QueryParseException("Expected expression after NEAR", Current.Position);
            }
            left = new NearNode(left, right, distance);
        }

        return left;
    }

    private QueryNode? ParseNotExpression()
    {
        if (Current.Type == QueryTokenType.Not)
        {
            Advance();
            var operand = ParsePrimaryExpression();
            if (operand == null)
            {
                throw new QueryParseException("Expected expression after NOT", Current.Position);
            }
            return new NotNode(operand);
        }

        return ParsePrimaryExpression();
    }

    private QueryNode? ParsePrimaryExpression()
    {
        switch (Current.Type)
        {
            case QueryTokenType.Term:
                var term = Current.Value;
                Advance();
                var normalizedTerms = _tokenizer.Tokenize(term);
                if (normalizedTerms.Count == 0)
                {
                    return null;
                }
                if (normalizedTerms.Count == 1)
                {
                    return new TermNode(normalizedTerms[0]);
                }
                return new PhraseNode(normalizedTerms);

            case QueryTokenType.Phrase:
                var phraseText = Current.Value;
                Advance();
                var phraseTerms = _tokenizer.Tokenize(phraseText);
                if (phraseTerms.Count == 0)
                {
                    return null;
                }
                return new PhraseNode(phraseTerms);

            case QueryTokenType.Wildcard:
                var pattern = Current.Value;
                Advance();
                return new WildcardNode(pattern);

            case QueryTokenType.Field:
                var fieldName = Current.FieldName!;
                var fieldValue = Current.Value;
                Advance();
                return new FieldNode(fieldName, fieldValue);

            case QueryTokenType.LeftParen:
                Advance();
                var innerExpr = ParseOrExpression();
                if (Current.Type != QueryTokenType.RightParen)
                {
                    throw new QueryParseException("Expected closing parenthesis", Current.Position);
                }
                Advance();
                return innerExpr;

            case QueryTokenType.EndOfInput:
            case QueryTokenType.RightParen:
                return null;

            default:
                throw new QueryParseException($"Unexpected token: {Current.Type}", Current.Position);
        }
    }
}

public class QueryParseException : Exception
{
    public int Position { get; }

    public QueryParseException(string message, int position)
        : base($"{message} at position {position}")
    {
        Position = position;
    }
}

public static class QueryAnalyzer
{
    public static QueryNode? Parse(string query, ITokenizer tokenizer)
    {
        var lexer = new QueryLexer(query);
        var tokens = lexer.Tokenize();
        var parser = new QueryParser(tokens, tokenizer);
        return parser.Parse();
    }

    public static string GetAstString(QueryNode? node)
    {
        if (node == null) return "(empty)";
        return node.ToString();
    }

    public static bool ContainsBooleanOperators(string query)
    {
        var upperQuery = query.ToUpperInvariant();
        return upperQuery.Contains(" AND ") ||
               upperQuery.Contains(" OR ") ||
               upperQuery.Contains(" NOT ") ||
               upperQuery.StartsWith("NOT ");
    }
}
