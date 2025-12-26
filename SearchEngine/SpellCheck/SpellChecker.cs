using SearchEngine.Interfaces;

namespace SearchEngine.SpellCheck;

public class SpellChecker : ISpellChecker
{
    private readonly IInvertedIndex _invertedIndex;
    private readonly ITokenizer _tokenizer;
    private readonly Dictionary<string, HashSet<string>> _kgramIndex;
    private readonly Dictionary<int, List<string>> _termsByLength;
    private readonly int _k;

    public SpellChecker(IInvertedIndex invertedIndex, ITokenizer tokenizer, int k = 2)
    {
        _invertedIndex = invertedIndex;
        _tokenizer = tokenizer;
        _kgramIndex = new Dictionary<string, HashSet<string>>();
        _termsByLength = new Dictionary<int, List<string>>();
        _k = k;
    }

    public void BuildIndex()
    {
        _kgramIndex.Clear();
        _termsByLength.Clear();
        var allTerms = _invertedIndex.GetAllTerms();

        foreach (var term in allTerms)
        {
            var len = term.Length;
            if (!_termsByLength.ContainsKey(len))
                _termsByLength[len] = new List<string>();
            _termsByLength[len].Add(term);

            var kgrams = GenerateKGrams(term);
            foreach (var kgram in kgrams)
            {
                if (!_kgramIndex.ContainsKey(kgram))
                    _kgramIndex[kgram] = new HashSet<string>();
                _kgramIndex[kgram].Add(term);
            }
        }
    }

    public List<SpellingSuggestion> GetSuggestions(string term, int maxSuggestions = 5)
    {
        if (string.IsNullOrWhiteSpace(term))
            return new List<SpellingSuggestion>();

        term = term.ToLowerInvariant();

        var allTerms = _invertedIndex.GetAllTerms();
        if (allTerms.Contains(term))
        {
            return new List<SpellingSuggestion>
            {
                new SpellingSuggestion { Term = term, EditDistance = 0, Score = 1.0 }
            };
        }

        var candidates = FindCandidates(term);
        var suggestions = new List<SpellingSuggestion>();

        foreach (var candidate in candidates)
        {
            var distance = CalculateEditDistanceWithThreshold(term, candidate, 3);
            if (distance > 3) continue;

            var jaccardSimilarity = CalculateJaccardSimilarity(term, candidate);
            var score = (1.0 / (1 + distance)) * (0.5 + 0.5 * jaccardSimilarity);

            suggestions.Add(new SpellingSuggestion
            {
                Term = candidate,
                EditDistance = distance,
                JaccardSimilarity = jaccardSimilarity,
                Score = score
            });
        }

        return suggestions
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.EditDistance)
            .Take(maxSuggestions)
            .ToList();
    }

    public QueryCorrection CorrectQuery(string query)
    {
        var result = new QueryCorrection
        {
            OriginalQuery = query,
            Corrections = new Dictionary<string, string>()
        };

        var tokens = _tokenizer.Tokenize(query);
        var allTerms = new HashSet<string>(_invertedIndex.GetAllTerms());
        var correctedTokens = new List<string>();

        foreach (var token in tokens)
        {
            var stemmed = _tokenizer.Stem(token);
            if (allTerms.Contains(token) || allTerms.Contains(stemmed))
            {
                correctedTokens.Add(token);
            }
            else
            {
                var suggestions = GetSuggestions(token, 1);
                if (suggestions.Count > 0 && suggestions[0].EditDistance <= 2)
                {
                    var correction = suggestions[0].Term;
                    result.Corrections[token] = correction;
                    correctedTokens.Add(correction);
                }
                else
                {
                    correctedTokens.Add(token);
                }
            }
        }

        result.CorrectedQuery = string.Join(" ", correctedTokens);
        result.HasCorrections = result.Corrections.Count > 0;

        return result;
    }

    private HashSet<string> FindCandidates(string term)
    {
        var candidates = new Dictionary<string, int>();
        var termKgrams = GenerateKGrams(term);
        var termLength = term.Length;

        foreach (var kgram in termKgrams)
        {
            if (_kgramIndex.TryGetValue(kgram, out var terms))
            {
                foreach (var t in terms)
                {
                    var lengthDiff = Math.Abs(t.Length - termLength);
                    if (lengthDiff > 3) continue;

                    if (!candidates.ContainsKey(t))
                        candidates[t] = 0;
                    candidates[t]++;
                }
            }
        }

        for (int len = Math.Max(2, termLength - 2); len <= termLength + 2; len++)
        {
            if (_termsByLength.TryGetValue(len, out var termsOfLength))
            {
                foreach (var t in termsOfLength)
                {
                    if (!candidates.ContainsKey(t) && t.Length > 0 && term.Length > 0 && t[0] == term[0])
                    {
                        candidates[t] = 1;
                    }
                }
            }
        }

        var minOverlap = Math.Max(1, termKgrams.Count / 4);
        return candidates.Where(c => c.Value >= minOverlap).Select(c => c.Key).ToHashSet();
    }

    private List<string> GenerateKGrams(string term)
    {
        var kgrams = new List<string>();
        if (string.IsNullOrEmpty(term)) return kgrams;

        var paddedTerm = "$" + term + "$";
        for (int i = 0; i <= paddedTerm.Length - _k; i++)
        {
            kgrams.Add(paddedTerm.Substring(i, _k));
        }
        return kgrams;
    }

    public int CalculateEditDistance(string s1, string s2)
    {
        return CalculateEditDistanceWithThreshold(s1, s2, int.MaxValue);
    }

    private int CalculateEditDistanceWithThreshold(string s1, string s2, int threshold)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;
        if (Math.Abs(s1.Length - s2.Length) > threshold) return threshold + 1;

        var m = s1.Length;
        var n = s2.Length;
        var prevRow = new int[n + 1];
        var currRow = new int[n + 1];

        for (int j = 0; j <= n; j++) prevRow[j] = j;

        for (int i = 1; i <= m; i++)
        {
            currRow[0] = i;
            int minInRow = i;

            for (int j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                currRow[j] = Math.Min(Math.Min(currRow[j - 1] + 1, prevRow[j] + 1), prevRow[j - 1] + cost);
                minInRow = Math.Min(minInRow, currRow[j]);
            }

            if (minInRow > threshold) return threshold + 1;
            (prevRow, currRow) = (currRow, prevRow);
        }

        return prevRow[n];
    }

    private double CalculateJaccardSimilarity(string s1, string s2)
    {
        var kgrams1 = GenerateKGrams(s1).ToHashSet();
        var kgrams2 = GenerateKGrams(s2).ToHashSet();
        var intersection = kgrams1.Intersect(kgrams2).Count();
        var union = kgrams1.Union(kgrams2).Count();
        return union > 0 ? (double)intersection / union : 0;
    }

    public KGramIndexVisualization GetKGramVisualization()
    {
        return new KGramIndexVisualization
        {
            K = _k,
            TotalKGrams = _kgramIndex.Count,
            TotalTerms = _invertedIndex.GetAllTerms().Count
        };
    }
}

public class SpellingSuggestion
{
    public string Term { get; set; } = string.Empty;
    public int EditDistance { get; set; }
    public double JaccardSimilarity { get; set; }
    public double Score { get; set; }
}

public class QueryCorrection
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string CorrectedQuery { get; set; } = string.Empty;
    public bool HasCorrections { get; set; }
    public Dictionary<string, string> Corrections { get; set; } = new();
}

public interface ISpellChecker
{
    void BuildIndex();
    List<SpellingSuggestion> GetSuggestions(string term, int maxSuggestions = 5);
    QueryCorrection CorrectQuery(string query);
    int CalculateEditDistance(string s1, string s2);
    KGramIndexVisualization GetKGramVisualization();
}

public class KGramIndexVisualization
{
    public int K { get; set; }
    public int TotalKGrams { get; set; }
    public int TotalTerms { get; set; }
}
