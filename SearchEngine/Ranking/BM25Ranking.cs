using SearchEngine.Models;

namespace SearchEngine.Ranking;

public class BM25Ranking : IRankingAlgorithm
{
    public string Name => "BM25";
    public string Description => "Okapi BM25 probabilistic ranking";

    private const double K1 = 1.2;
    private const double B = 0.75;

    public double CalculateScore(
        Document document,
        List<string> queryTerms,
        DocumentStatistics docStats,
        CollectionStatistics collectionStats)
    {
        if (queryTerms.Count == 0 || collectionStats.TotalDocuments == 0)
            return 0;

        double score = 0;
        var docLength = Math.Max(docStats.TotalTerms, 1);
        var avgDocLength = Math.Max(collectionStats.AverageDocumentLength, 1.0);

        var lengthNorm = 1 - B + B * (docLength / avgDocLength);

        foreach (var term in queryTerms.Distinct())
        {
            var tf = docStats.TermFrequencies.GetValueOrDefault(term, 0);
            if (tf == 0) continue;

            var idf = CalculateIDF(term, collectionStats);

            var tfComponent = (tf * (K1 + 1)) / (tf + K1 * lengthNorm);

            score += idf * tfComponent;
        }

        return score;
    }

    private double CalculateIDF(string term, CollectionStatistics stats)
    {
        var df = stats.DocumentFrequencies.GetValueOrDefault(term, 0);
        var N = stats.TotalDocuments;

        if (df == 0) return 0;

        return Math.Log(1 + (N - df + 0.5) / (df + 0.5));
    }
}
