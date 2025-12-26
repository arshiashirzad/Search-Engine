using System.Collections.Concurrent;

namespace SearchEngine.Analytics;

public class SearchAnalytics : ISearchAnalytics
{
    private readonly ConcurrentDictionary<string, QueryStats> _queryStats;
    private readonly ConcurrentQueue<SearchEvent> _recentSearches;
    private readonly ConcurrentDictionary<Guid, int> _documentClickCounts;
    private readonly int _maxRecentSearches;

    public SearchAnalytics(int maxRecentSearches = 1000)
    {
        _queryStats = new ConcurrentDictionary<string, QueryStats>();
        _recentSearches = new ConcurrentQueue<SearchEvent>();
        _documentClickCounts = new ConcurrentDictionary<Guid, int>();
        _maxRecentSearches = maxRecentSearches;
    }

    public void RecordSearch(string query, int resultCount, double searchTimeMs)
    {
        var normalizedQuery = query.ToLowerInvariant().Trim();

        _queryStats.AddOrUpdate(
            normalizedQuery,
            _ => new QueryStats
            {
                Query = normalizedQuery,
                SearchCount = 1,
                TotalResultsReturned = resultCount,
                TotalSearchTimeMs = searchTimeMs,
                FirstSearched = DateTime.UtcNow,
                LastSearched = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.SearchCount++;
                existing.TotalResultsReturned += resultCount;
                existing.TotalSearchTimeMs += searchTimeMs;
                existing.LastSearched = DateTime.UtcNow;
                return existing;
            });

        var searchEvent = new SearchEvent
        {
            Query = normalizedQuery,
            Timestamp = DateTime.UtcNow,
            ResultCount = resultCount,
            SearchTimeMs = searchTimeMs
        };

        _recentSearches.Enqueue(searchEvent);

        while (_recentSearches.Count > _maxRecentSearches)
        {
            _recentSearches.TryDequeue(out _);
        }
    }

    public void RecordDocumentClick(Guid documentId, string query)
    {
        _documentClickCounts.AddOrUpdate(documentId, 1, (_, count) => count + 1);

        var normalizedQuery = query.ToLowerInvariant().Trim();
        if (_queryStats.TryGetValue(normalizedQuery, out var stats))
        {
            stats.ClickThroughCount++;
            if (!stats.ClickedDocuments.Contains(documentId))
            {
                stats.ClickedDocuments.Add(documentId);
            }
        }
    }

    public List<QueryStats> GetTopQueries(int count = 10)
    {
        return _queryStats.Values
            .OrderByDescending(q => q.SearchCount)
            .Take(count)
            .ToList();
    }

    public List<string> GetZeroResultQueries(int count = 10)
    {
        return _queryStats.Values
            .Where(q => q.AverageResultCount == 0)
            .OrderByDescending(q => q.SearchCount)
            .Take(count)
            .Select(q => q.Query)
            .ToList();
    }

    public List<(Guid DocumentId, int ClickCount)> GetPopularDocuments(int count = 10)
    {
        return _documentClickCounts
            .OrderByDescending(kv => kv.Value)
            .Take(count)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    public List<SearchEvent> GetRecentSearches(int count = 50)
    {
        return _recentSearches
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    public Dictionary<DateTime, int> GetSearchVolumeByHour(int hours = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hours);

        return _recentSearches
            .Where(e => e.Timestamp >= cutoff)
            .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public AnalyticsSummary GetSummary()
    {
        var allStats = _queryStats.Values.ToList();

        return new AnalyticsSummary
        {
            TotalSearches = allStats.Sum(q => q.SearchCount),
            UniqueQueries = allStats.Count,
            AverageResultCount = allStats.Count > 0 ? allStats.Average(q => q.AverageResultCount) : 0,
            AverageSearchTimeMs = allStats.Count > 0 ? allStats.Average(q => q.AverageSearchTimeMs) : 0,
            TotalDocumentClicks = _documentClickCounts.Values.Sum(),
            ZeroResultQueryCount = allStats.Count(q => q.AverageResultCount == 0)
        };
    }

    public List<string> GetQuerySuggestions(string prefix, int count = 5)
    {
        var normalizedPrefix = prefix.ToLowerInvariant();

        return _queryStats.Values
            .Where(q => q.Query.StartsWith(normalizedPrefix))
            .OrderByDescending(q => q.SearchCount)
            .Take(count)
            .Select(q => q.Query)
            .ToList();
    }
}

public class QueryStats
{
    public string Query { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public long TotalResultsReturned { get; set; }
    public double TotalSearchTimeMs { get; set; }
    public DateTime FirstSearched { get; set; }
    public DateTime LastSearched { get; set; }
    public int ClickThroughCount { get; set; }
    public HashSet<Guid> ClickedDocuments { get; set; } = new();

    public double AverageResultCount => SearchCount > 0 ? (double)TotalResultsReturned / SearchCount : 0;
    public double AverageSearchTimeMs => SearchCount > 0 ? TotalSearchTimeMs / SearchCount : 0;
    public double ClickThroughRate => SearchCount > 0 ? (double)ClickThroughCount / SearchCount : 0;
}

public class SearchEvent
{
    public string Query { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int ResultCount { get; set; }
    public double SearchTimeMs { get; set; }
}

public class AnalyticsSummary
{
    public int TotalSearches { get; set; }
    public int UniqueQueries { get; set; }
    public double AverageResultCount { get; set; }
    public double AverageSearchTimeMs { get; set; }
    public int TotalDocumentClicks { get; set; }
    public int ZeroResultQueryCount { get; set; }
}

public interface ISearchAnalytics
{
    void RecordSearch(string query, int resultCount, double searchTimeMs);
    void RecordDocumentClick(Guid documentId, string query);
    List<QueryStats> GetTopQueries(int count = 10);
    List<string> GetZeroResultQueries(int count = 10);
    List<(Guid DocumentId, int ClickCount)> GetPopularDocuments(int count = 10);
    List<SearchEvent> GetRecentSearches(int count = 50);
    Dictionary<DateTime, int> GetSearchVolumeByHour(int hours = 24);
    AnalyticsSummary GetSummary();
    List<string> GetQuerySuggestions(string prefix, int count = 5);
}
