namespace SearchEngine.TextProcessing;

public class QueryExpander : IQueryExpander
{
    private readonly Dictionary<string, HashSet<string>> _synonyms;
    private readonly IStemmer? _stemmer;

    public QueryExpander(IStemmer? stemmer = null)
    {
        _stemmer = stemmer;
        _synonyms = BuildSynonymDatabase();
    }

    public List<string> ExpandTerms(List<string> terms, int maxExpansions = 3)
    {
        var expanded = new HashSet<string>(terms);

        foreach (var term in terms)
        {
            var synonyms = GetSynonyms(term);
            var count = 0;
            foreach (var syn in synonyms)
            {
                if (count >= maxExpansions) break;
                expanded.Add(syn);
                count++;
            }
        }

        return expanded.ToList();
    }

    public List<string> GetSynonyms(string term)
    {
        var result = new HashSet<string>();
        var normalizedTerm = term.ToLowerInvariant();

        if (_synonyms.TryGetValue(normalizedTerm, out var syns))
        {
            result.UnionWith(syns);
        }

        foreach (var (key, values) in _synonyms)
        {
            if (values.Contains(normalizedTerm))
            {
                result.Add(key);
                result.UnionWith(values);
            }
        }

        if (_stemmer != null)
        {
            var stemmed = _stemmer.Stem(normalizedTerm);
            if (_synonyms.TryGetValue(stemmed, out var stemmedSyns))
            {
                result.UnionWith(stemmedSyns);
            }
        }

        result.Remove(normalizedTerm);
        return result.ToList();
    }

    public List<string> GetRelatedTerms(string term)
    {
        var related = new HashSet<string>();
        var synonyms = GetSynonyms(term);
        related.UnionWith(synonyms);

        var normalizedTerm = term.ToLowerInvariant();

        var variations = GenerateVariations(normalizedTerm);
        related.UnionWith(variations);

        related.Remove(normalizedTerm);
        return related.Take(10).ToList();
    }

    private List<string> GenerateVariations(string term)
    {
        var variations = new List<string>();

        if (term.EndsWith("ing"))
        {
            var base1 = term[..^3];
            variations.Add(base1);
            variations.Add(base1 + "e");
            variations.Add(base1 + "ed");
            variations.Add(base1 + "s");
        }
        else if (term.EndsWith("ed"))
        {
            var base1 = term[..^2];
            variations.Add(base1);
            variations.Add(base1 + "e");
            variations.Add(base1 + "ing");
            variations.Add(base1 + "s");
        }
        else if (term.EndsWith("s") && !term.EndsWith("ss"))
        {
            var base1 = term[..^1];
            variations.Add(base1);
            variations.Add(base1 + "ing");
            variations.Add(base1 + "ed");
        }
        else if (term.EndsWith("er"))
        {
            var base1 = term[..^2];
            variations.Add(base1);
            variations.Add(base1 + "e");
            variations.Add(base1 + "est");
            variations.Add(base1 + "ing");
        }
        else
        {
            variations.Add(term + "s");
            variations.Add(term + "ed");
            variations.Add(term + "ing");
            variations.Add(term + "er");
            variations.Add(term + "ly");
        }

        return variations.Where(v => v.Length > 2).ToList();
    }

    private Dictionary<string, HashSet<string>> BuildSynonymDatabase()
    {
        return new Dictionary<string, HashSet<string>>
        {
            { "computer", new HashSet<string> { "pc", "machine", "system", "workstation", "laptop", "desktop" } },
            { "software", new HashSet<string> { "program", "application", "app", "code", "system" } },
            { "search", new HashSet<string> { "find", "query", "lookup", "seek", "locate", "retrieve" } },
            { "data", new HashSet<string> { "information", "records", "content", "dataset" } },
            { "algorithm", new HashSet<string> { "method", "procedure", "process", "technique" } },
            { "network", new HashSet<string> { "internet", "web", "connection", "system" } },
            { "file", new HashSet<string> { "document", "record", "data" } },
            { "user", new HashSet<string> { "client", "customer", "person", "account" } },
            { "error", new HashSet<string> { "bug", "fault", "issue", "problem", "defect", "mistake" } },
            { "fix", new HashSet<string> { "repair", "solve", "correct", "patch", "resolve" } },

            { "research", new HashSet<string> { "study", "investigation", "analysis", "examination" } },
            { "paper", new HashSet<string> { "article", "publication", "document", "report", "study" } },
            { "method", new HashSet<string> { "approach", "technique", "procedure", "process", "way" } },
            { "result", new HashSet<string> { "outcome", "finding", "conclusion", "output" } },
            { "analysis", new HashSet<string> { "study", "examination", "evaluation", "assessment" } },
            { "model", new HashSet<string> { "framework", "system", "approach", "design" } },
            { "theory", new HashSet<string> { "concept", "principle", "hypothesis", "framework" } },

            { "index", new HashSet<string> { "catalog", "listing", "directory", "inventory" } },
            { "document", new HashSet<string> { "file", "text", "record", "article", "page" } },
            { "query", new HashSet<string> { "search", "request", "question", "lookup" } },
            { "relevance", new HashSet<string> { "pertinence", "importance", "significance" } },
            { "ranking", new HashSet<string> { "ordering", "scoring", "sorting", "rating" } },
            { "term", new HashSet<string> { "word", "token", "keyword" } },
            { "frequency", new HashSet<string> { "count", "occurrence", "rate" } },

            { "big", new HashSet<string> { "large", "huge", "great", "major", "significant" } },
            { "small", new HashSet<string> { "little", "tiny", "minor", "slight" } },
            { "fast", new HashSet<string> { "quick", "rapid", "speedy", "swift" } },
            { "slow", new HashSet<string> { "sluggish", "gradual", "leisurely" } },
            { "good", new HashSet<string> { "great", "excellent", "fine", "positive" } },
            { "bad", new HashSet<string> { "poor", "negative", "terrible", "awful" } },
            { "new", new HashSet<string> { "recent", "modern", "latest", "fresh" } },
            { "old", new HashSet<string> { "ancient", "previous", "former", "outdated" } },
            { "start", new HashSet<string> { "begin", "initiate", "commence", "launch" } },
            { "stop", new HashSet<string> { "end", "halt", "terminate", "cease" } },
            { "create", new HashSet<string> { "make", "build", "generate", "produce", "construct" } },
            { "delete", new HashSet<string> { "remove", "erase", "eliminate", "discard" } },
            { "change", new HashSet<string> { "modify", "alter", "update", "revise", "transform" } },
            { "show", new HashSet<string> { "display", "present", "reveal", "demonstrate" } },
            { "hide", new HashSet<string> { "conceal", "mask", "obscure" } },
            { "help", new HashSet<string> { "assist", "support", "aid", "guide" } },
            { "problem", new HashSet<string> { "issue", "challenge", "difficulty", "trouble" } },
            { "solution", new HashSet<string> { "answer", "resolution", "fix", "remedy" } },
            { "important", new HashSet<string> { "significant", "crucial", "essential", "vital", "key" } },
            { "simple", new HashSet<string> { "easy", "basic", "straightforward", "plain" } },
            { "complex", new HashSet<string> { "complicated", "difficult", "intricate", "elaborate" } },
        };
    }
}

public interface IQueryExpander
{
    List<string> ExpandTerms(List<string> terms, int maxExpansions = 3);
    List<string> GetSynonyms(string term);
    List<string> GetRelatedTerms(string term);
}
