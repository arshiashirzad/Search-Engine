using System.Diagnostics;
using SearchEngine.Analytics;
using SearchEngine.Interfaces;
using SearchEngine.Models;
using SearchEngine.Ranking;
using SearchEngine.SpellCheck;

namespace SearchEngine.Services;

public class AdvancedSearchEngine : IAdvancedSearchEngine
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IInvertedIndex _invertedIndex;
    private readonly ITokenizer _tokenizer;
    private readonly IStatisticsCollector _statisticsCollector;
    private readonly ISpellChecker _spellChecker;
    private readonly ISearchAnalytics _analytics;
    private readonly IResultHighlighter _highlighter;
    private readonly IRankingAlgorithm _rankingAlgorithm;

    public AdvancedSearchEngine(
        IDocumentRepository documentRepository,
        IInvertedIndex invertedIndex,
        ITokenizer tokenizer)
    {
        _documentRepository = documentRepository;
        _invertedIndex = invertedIndex;
        _tokenizer = tokenizer;

        _statisticsCollector = new StatisticsCollector(documentRepository, invertedIndex, tokenizer);
        _spellChecker = new SpellChecker(invertedIndex, tokenizer);
        _analytics = new SearchAnalytics();
        _highlighter = new ResultHighlighter(tokenizer);
        _rankingAlgorithm = new BM25Ranking();
    }

    public AdvancedSearchResult Search(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new AdvancedSearchResult
        {
            OriginalQuery = request.Query
        };

        try
        {
            if (request.EnableSpellCheck)
            {
                var correction = _spellChecker.CorrectQuery(request.Query);
                if (correction.HasCorrections)
                {
                    result.SpellCorrection = correction;
                }
            }

            var queryTerms = _tokenizer.TokenizeWithStemming(request.Query);

            var matchingDocIds = ExecuteQuery(queryTerms);

            var matchingDocs = matchingDocIds
                .Select(id => _documentRepository.GetById(id))
                .Where(d => d != null)
                .Cast<Document>()
                .ToList();

            var collectionStats = _statisticsCollector.GetCollectionStatistics();
            var rankedResults = new List<SearchResult>();

            foreach (var doc in matchingDocs)
            {
                var docStats = _statisticsCollector.GetDocumentStatistics(doc.Id);
                var score = _rankingAlgorithm.CalculateScore(doc, queryTerms, docStats, collectionStats);

                var searchResult = new SearchResult
                {
                    Document = doc,
                    Relevance = score
                };

                if (request.EnableHighlighting)
                {
                    var snippet = _highlighter.GetHighlightedSnippet(doc.Content, queryTerms, 200);
                    searchResult.HighlightedSnippet = snippet.Text;
                }

                rankedResults.Add(searchResult);
            }

            result.Results = rankedResults.OrderByDescending(r => r.Relevance).ToList();
            result.TotalResults = result.Results.Count;

            if (request.PageSize > 0)
            {
                result.Results = result.Results
                    .Skip(request.Page * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();
            }

            result.Page = request.Page;
            result.PageSize = request.PageSize;
            result.TotalPages = request.PageSize > 0
                ? (int)Math.Ceiling((double)result.TotalResults / request.PageSize)
                : 1;
        }
        catch (Exception ex)
        {
            result.Error = $"Search error: {ex.Message}";
        }

        stopwatch.Stop();
        result.SearchTimeMs = stopwatch.ElapsedMilliseconds;
        _analytics.RecordSearch(request.Query, result.TotalResults, result.SearchTimeMs);

        return result;
    }

    private HashSet<Guid> ExecuteQuery(List<string> queryTerms)
    {
        var results = new HashSet<Guid>();
        if (queryTerms.Count == 0) return results;

        var firstTermResults = _invertedIndex.Search(queryTerms[0]);
        results.UnionWith(firstTermResults);

        for (int i = 1; i < queryTerms.Count; i++)
        {
            var termResults = _invertedIndex.Search(queryTerms[i]);
            results.IntersectWith(termResults);
        }

        return results;
    }

    public void RebuildSpellCheckIndex() => _spellChecker.BuildIndex();
    public void InvalidateCache() => _statisticsCollector.InvalidateCache();
    public AnalyticsSummary GetAnalytics() => _analytics.GetSummary();
    public List<SpellingSuggestion> GetSpellingSuggestions(string term) => _spellChecker.GetSuggestions(term);
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public bool EnableSpellCheck { get; set; } = true;
    public bool EnableHighlighting { get; set; } = true;
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 10;
}

public class AdvancedSearchResult
{
    public string OriginalQuery { get; set; } = string.Empty;
    public List<SearchResult> Results { get; set; } = new();
    public int TotalResults { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public QueryCorrection? SpellCorrection { get; set; }
    public long SearchTimeMs { get; set; }
    public string? Error { get; set; }
}

public interface IAdvancedSearchEngine
{
    AdvancedSearchResult Search(SearchRequest request);
    void RebuildSpellCheckIndex();
    void InvalidateCache();
    AnalyticsSummary GetAnalytics();
    List<SpellingSuggestion> GetSpellingSuggestions(string term);
}
