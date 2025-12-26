using System.Text.RegularExpressions;
using SearchEngine.Interfaces;

namespace SearchEngine.QueryEngine;

public class QueryEvaluator : IQueryEvaluator
{
    private readonly IInvertedIndex _index;
    private readonly IDocumentRepository _documentRepository;
    private readonly ITokenizer _tokenizer;

    public QueryEvaluator(
        IInvertedIndex index,
        IDocumentRepository documentRepository,
        ITokenizer tokenizer)
    {
        _index = index;
        _documentRepository = documentRepository;
        _tokenizer = tokenizer;
    }

    public HashSet<Guid> EvaluateTerm(string term)
    {
        return _index.Search(term);
    }

    public HashSet<Guid> EvaluatePhrase(List<string> terms)
    {
        if (terms.Count == 0) return new HashSet<Guid>();
        if (terms.Count == 1) return _index.Search(terms[0]);
        return _index.SearchPhrase(terms);
    }

    public HashSet<Guid> EvaluateWildcard(string pattern)
    {
        var results = new HashSet<Guid>();

        var regexPattern = "^" +
            Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") +
            "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

        var allTerms = _index.GetAllTerms();
        foreach (var term in allTerms)
        {
            if (regex.IsMatch(term))
            {
                var termResults = _index.Search(term);
                results.UnionWith(termResults);
            }
        }

        return results;
    }

    public HashSet<Guid> EvaluateField(string fieldName, string term)
    {
        var results = new HashSet<Guid>();
        var allDocuments = _documentRepository.GetAll();

        foreach (var doc in allDocuments)
        {
            string fieldContent = fieldName.ToLowerInvariant() switch
            {
                "title" => doc.Title,
                "content" => doc.Content,
                "filename" => doc.FileName ?? "",
                _ => ""
            };

            var tokens = _tokenizer.Tokenize(fieldContent);
            if (tokens.Contains(term))
            {
                results.Add(doc.Id);
            }
        }

        return results;
    }

    public HashSet<Guid> EvaluateProximity(List<string> terms1, List<string> terms2, int distance)
    {
        var results = new HashSet<Guid>();

        if (terms1.Count == 0 || terms2.Count == 0)
            return results;

        var docs1 = terms1.Count == 1
            ? _index.Search(terms1[0])
            : _index.SearchPhrase(terms1);

        var docs2 = terms2.Count == 1
            ? _index.Search(terms2[0])
            : _index.SearchPhrase(terms2);

        var candidates = new HashSet<Guid>(docs1);
        candidates.IntersectWith(docs2);

        foreach (var docId in candidates)
        {
            if (CheckProximity(docId, terms1, terms2, distance))
            {
                results.Add(docId);
            }
        }

        return results;
    }

    private bool CheckProximity(Guid docId, List<string> terms1, List<string> terms2, int distance)
    {
        var doc = _documentRepository.GetById(docId);
        if (doc == null) return false;

        var allTokens = _tokenizer.Tokenize(doc.Title + " " + doc.Content);

        var term1 = terms1.Last();
        var term2 = terms2.First();

        var positions1 = new List<int>();
        var positions2 = new List<int>();

        for (int i = 0; i < allTokens.Count; i++)
        {
            if (allTokens[i] == term1)
            {
                if (terms1.Count > 1)
                {
                    if (VerifyPhraseAtPosition(allTokens, terms1, i - terms1.Count + 1))
                    {
                        positions1.Add(i);
                    }
                }
                else
                {
                    positions1.Add(i);
                }
            }

            if (allTokens[i] == term2)
            {
                if (terms2.Count > 1)
                {
                    if (VerifyPhraseAtPosition(allTokens, terms2, i))
                    {
                        positions2.Add(i);
                    }
                }
                else
                {
                    positions2.Add(i);
                }
            }
        }

        foreach (var p1 in positions1)
        {
            foreach (var p2 in positions2)
            {
                var actualDistance = Math.Abs(p2 - p1) - 1;
                if (actualDistance <= distance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool VerifyPhraseAtPosition(List<string> tokens, List<string> phrase, int startPos)
    {
        if (startPos < 0 || startPos + phrase.Count > tokens.Count)
            return false;

        for (int i = 0; i < phrase.Count; i++)
        {
            if (tokens[startPos + i] != phrase[i])
                return false;
        }

        return true;
    }

    public HashSet<Guid> GetAllDocumentIds()
    {
        var allDocs = _documentRepository.GetAll();
        return new HashSet<Guid>(allDocs.Select(d => d.Id));
    }
}
