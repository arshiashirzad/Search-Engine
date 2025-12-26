using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Ranking;

public class StatisticsCollector : IStatisticsCollector
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ITokenizer _tokenizer;

    private CollectionStatistics? _cachedCollectionStats;
    private readonly Dictionary<Guid, DocumentStatistics> _cachedDocStats = new();
    private bool _isDirty = true;

    public StatisticsCollector(
        IDocumentRepository documentRepository,
        IInvertedIndex invertedIndex,
        ITokenizer tokenizer)
    {
        _documentRepository = documentRepository;
        _tokenizer = tokenizer;
    }

    public void InvalidateCache()
    {
        _isDirty = true;
        _cachedDocStats.Clear();
        _cachedCollectionStats = null;
    }

    public CollectionStatistics GetCollectionStatistics()
    {
        if (!_isDirty && _cachedCollectionStats != null)
        {
            return _cachedCollectionStats;
        }

        var documents = _documentRepository.GetAll().ToList();
        var stats = new CollectionStatistics
        {
            TotalDocuments = documents.Count,
            DocumentFrequencies = new Dictionary<string, int>()
        };

        if (documents.Count == 0)
        {
            _cachedCollectionStats = stats;
            return stats;
        }

        long totalTerms = 0;

        foreach (var doc in documents)
        {
            var docStats = GetDocumentStatistics(doc.Id);
            totalTerms += docStats.TotalTerms;

            foreach (var term in docStats.TermFrequencies.Keys)
            {
                if (!stats.DocumentFrequencies.ContainsKey(term))
                {
                    stats.DocumentFrequencies[term] = 0;
                }
                stats.DocumentFrequencies[term]++;
            }
        }

        stats.AverageDocumentLength = documents.Count > 0
            ? (double)totalTerms / documents.Count
            : 0;

        _cachedCollectionStats = stats;
        _isDirty = false;

        return stats;
    }

    public DocumentStatistics GetDocumentStatistics(Guid documentId)
    {
        if (_cachedDocStats.TryGetValue(documentId, out var cached))
        {
            return cached;
        }

        var document = _documentRepository.GetById(documentId);
        if (document == null)
        {
            return new DocumentStatistics { DocumentId = documentId };
        }

        var stats = new DocumentStatistics
        {
            DocumentId = documentId,
            TermFrequencies = new Dictionary<string, int>(),
            TitleTermFrequencies = new Dictionary<string, int>(),
            ContentTermFrequencies = new Dictionary<string, int>()
        };

        var titleTokens = _tokenizer.TokenizeWithStemming(document.Title);
        foreach (var token in titleTokens)
        {
            if (!stats.TitleTermFrequencies.ContainsKey(token))
                stats.TitleTermFrequencies[token] = 0;
            stats.TitleTermFrequencies[token]++;
        }

        var contentTokens = _tokenizer.TokenizeWithStemming(document.Content);
        foreach (var token in contentTokens)
        {
            if (!stats.ContentTermFrequencies.ContainsKey(token))
                stats.ContentTermFrequencies[token] = 0;
            stats.ContentTermFrequencies[token]++;
        }

        var allTerms = new HashSet<string>();
        allTerms.UnionWith(stats.TitleTermFrequencies.Keys);
        allTerms.UnionWith(stats.ContentTermFrequencies.Keys);

        foreach (var term in allTerms)
        {
            stats.TermFrequencies[term] =
                stats.TitleTermFrequencies.GetValueOrDefault(term, 0) +
                stats.ContentTermFrequencies.GetValueOrDefault(term, 0);
        }

        stats.TotalTerms = titleTokens.Count + contentTokens.Count;

        _cachedDocStats[documentId] = stats;
        return stats;
    }
}

public interface IStatisticsCollector
{
    CollectionStatistics GetCollectionStatistics();
    DocumentStatistics GetDocumentStatistics(Guid documentId);
    void InvalidateCache();
}
