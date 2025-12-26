namespace SearchEngine.QueryEngine;

public enum QueryTokenType
{
    Term,
    And,
    Or,
    Not,
    Near,
    LeftParen,
    RightParen,
    Phrase,
    Wildcard,
    Field,
    EndOfInput
}

public class QueryToken
{
    public QueryTokenType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Position { get; set; }

    public int ProximityDistance { get; set; } = 1;

    public string? FieldName { get; set; }

    public QueryToken(QueryTokenType type, string value = "", int position = 0)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public override string ToString()
    {
        return Type switch
        {
            QueryTokenType.Term => $"TERM({Value})",
            QueryTokenType.And => "AND",
            QueryTokenType.Or => "OR",
            QueryTokenType.Not => "NOT",
            QueryTokenType.Near => $"NEAR/{ProximityDistance}",
            QueryTokenType.LeftParen => "(",
            QueryTokenType.RightParen => ")",
            QueryTokenType.Phrase => $"PHRASE(\"{Value}\")",
            QueryTokenType.Wildcard => $"WILDCARD({Value})",
            QueryTokenType.Field => $"FIELD({FieldName}:{Value})",
            QueryTokenType.EndOfInput => "EOI",
            _ => $"UNKNOWN({Value})"
        };
    }
}
