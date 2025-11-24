namespace SearchEngine.Models;

public class SearchResult
{
    public Document Document { get; set; } = null!;
    public double Relevance { get; set; }
    public List<int> TermPositions { get; set; } = new();
}
