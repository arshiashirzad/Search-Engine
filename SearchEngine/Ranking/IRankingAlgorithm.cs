using SearchEngine.Models;

namespace SearchEngine.Ranking;

public interface IRankingAlgorithm
{
    string Name { get; }
    string Description { get; }

    double CalculateScore(
        Document document,
        List<string> queryTerms,
        DocumentStatistics docStats,
        CollectionStatistics collectionStats);
}

public class DocumentStatistics
{
    public Guid DocumentId { get; set; }
    public int TotalTerms { get; set; }
    public Dictionary<string, int> TermFrequencies { get; set; } = new();
    public Dictionary<string, int> TitleTermFrequencies { get; set; } = new();
    public Dictionary<string, int> ContentTermFrequencies { get; set; } = new();
}

public class CollectionStatistics
{
    public int TotalDocuments { get; set; }
    public double AverageDocumentLength { get; set; }
    public Dictionary<string, int> DocumentFrequencies { get; set; } = new();
}
