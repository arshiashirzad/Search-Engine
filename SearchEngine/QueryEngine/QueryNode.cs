namespace SearchEngine.QueryEngine;

public enum QueryNodeType
{
    Term,
    Phrase,
    And,
    Or,
    Not,
    Near,
    Wildcard,
    Field
}

public abstract class QueryNode
{
    public abstract QueryNodeType NodeType { get; }
    public abstract HashSet<Guid> Evaluate(IQueryEvaluator evaluator);
}

public interface IQueryEvaluator
{
    HashSet<Guid> EvaluateTerm(string term);
    HashSet<Guid> EvaluatePhrase(List<string> terms);
    HashSet<Guid> EvaluateWildcard(string pattern);
    HashSet<Guid> EvaluateField(string fieldName, string term);
    HashSet<Guid> EvaluateProximity(List<string> term1, List<string> term2, int distance);
    HashSet<Guid> GetAllDocumentIds();
}

public class TermNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.Term;
    public string Term { get; }

    public TermNode(string term)
    {
        Term = term;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        return evaluator.EvaluateTerm(Term);
    }

    public override string ToString() => $"Term({Term})";
}

public class PhraseNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.Phrase;
    public List<string> Terms { get; }

    public PhraseNode(List<string> terms)
    {
        Terms = terms;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        return evaluator.EvaluatePhrase(Terms);
    }

    public override string ToString() => $"Phrase(\"{string.Join(" ", Terms)}\")";
}

public class AndNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.And;
    public QueryNode Left { get; }
    public QueryNode Right { get; }

    public AndNode(QueryNode left, QueryNode right)
    {
        Left = left;
        Right = right;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        var leftResults = Left.Evaluate(evaluator);
        var rightResults = Right.Evaluate(evaluator);
        leftResults.IntersectWith(rightResults);
        return leftResults;
    }

    public override string ToString() => $"AND({Left}, {Right})";
}

public class OrNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.Or;
    public QueryNode Left { get; }
    public QueryNode Right { get; }

    public OrNode(QueryNode left, QueryNode right)
    {
        Left = left;
        Right = right;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        var leftResults = Left.Evaluate(evaluator);
        var rightResults = Right.Evaluate(evaluator);
        leftResults.UnionWith(rightResults);
        return leftResults;
    }

    public override string ToString() => $"OR({Left}, {Right})";
}

public class NotNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.Not;
    public QueryNode Operand { get; }

    public NotNode(QueryNode operand)
    {
        Operand = operand;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        var allDocs = evaluator.GetAllDocumentIds();
        var excludeDocs = Operand.Evaluate(evaluator);
        allDocs.ExceptWith(excludeDocs);
        return allDocs;
    }

    public override string ToString() => $"NOT({Operand})";
}

public class NearNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.Near;
    public QueryNode Left { get; }
    public QueryNode Right { get; }
    public int Distance { get; }

    public NearNode(QueryNode left, QueryNode right, int distance)
    {
        Left = left;
        Right = right;
        Distance = distance;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        var leftTerms = ExtractTerms(Left);
        var rightTerms = ExtractTerms(Right);

        return evaluator.EvaluateProximity(leftTerms, rightTerms, Distance);
    }

    private List<string> ExtractTerms(QueryNode node)
    {
        return node switch
        {
            TermNode term => new List<string> { term.Term },
            PhraseNode phrase => phrase.Terms,
            _ => new List<string>()
        };
    }

    public override string ToString() => $"NEAR/{Distance}({Left}, {Right})";
}

public class WildcardNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.Wildcard;
    public string Pattern { get; }

    public WildcardNode(string pattern)
    {
        Pattern = pattern;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        return evaluator.EvaluateWildcard(Pattern);
    }

    public override string ToString() => $"Wildcard({Pattern})";
}

public class FieldNode : QueryNode
{
    public override QueryNodeType NodeType => QueryNodeType.Field;
    public string FieldName { get; }
    public string Term { get; }

    public FieldNode(string fieldName, string term)
    {
        FieldName = fieldName;
        Term = term;
    }

    public override HashSet<Guid> Evaluate(IQueryEvaluator evaluator)
    {
        return evaluator.EvaluateField(FieldName, Term);
    }

    public override string ToString() => $"Field({FieldName}:{Term})";
}
