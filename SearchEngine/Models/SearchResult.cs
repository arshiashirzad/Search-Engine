namespace SearchEngine.Models;

public class SearchResult
{
    public Document Document { get; set; } = null!;
    public double Relevance { get; set; }
    public List<int> TermPositions { get; set; } = new();

    public string? HighlightedSnippet { get; set; }
    public string? HighlightedTitle { get; set; }
    public int MatchCount { get; set; }

    public string? ScoreExplanation { get; set; }
}
