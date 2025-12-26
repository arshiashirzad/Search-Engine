using System.Text;
using System.Text.RegularExpressions;
using SearchEngine.Interfaces;

namespace SearchEngine.Analytics;

public class ResultHighlighter : IResultHighlighter
{
    private readonly ITokenizer _tokenizer;

    public ResultHighlighter(ITokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public HighlightedSnippet GetHighlightedSnippet(
        string content,
        List<string> queryTerms,
        int snippetLength = 200,
        string highlightStart = "<mark>",
        string highlightEnd = "</mark>")
    {
        if (string.IsNullOrWhiteSpace(content) || queryTerms.Count == 0)
        {
            return new HighlightedSnippet
            {
                Text = TruncateText(content, snippetLength),
                MatchPositions = new List<(int, int)>()
            };
        }

        var queryTermSet = new HashSet<string>(queryTerms.Select(t => t.ToLowerInvariant()));
        var normalizedContent = content.ToLowerInvariant();

        var matchPositions = FindMatchPositions(content, queryTermSet);

        if (matchPositions.Count == 0)
        {
            return new HighlightedSnippet
            {
                Text = TruncateText(content, snippetLength),
                MatchPositions = new List<(int, int)>()
            };
        }

        var (snippetStart, snippetEnd) = FindBestSnippetRange(matchPositions, snippetLength, content.Length);

        var snippet = ExtractSnippet(content, snippetStart, snippetEnd);
        var highlightedSnippet = HighlightTerms(snippet, queryTermSet, highlightStart, highlightEnd);

        var adjustedPositions = matchPositions
            .Where(p => p.Start >= snippetStart && p.End <= snippetEnd)
            .Select(p => (p.Start - snippetStart, p.End - snippetStart))
            .ToList();

        return new HighlightedSnippet
        {
            Text = highlightedSnippet,
            MatchPositions = adjustedPositions,
            SnippetStart = snippetStart,
            SnippetEnd = snippetEnd,
            TotalMatches = matchPositions.Count
        };
    }

    public string HighlightFullContent(
        string content,
        List<string> queryTerms,
        string highlightStart = "<mark>",
        string highlightEnd = "</mark>")
    {
        if (string.IsNullOrWhiteSpace(content) || queryTerms.Count == 0)
            return content;

        var queryTermSet = new HashSet<string>(queryTerms.Select(t => t.ToLowerInvariant()));
        return HighlightTerms(content, queryTermSet, highlightStart, highlightEnd);
    }

    public List<HighlightedSnippet> GetMultipleSnippets(
        string content,
        List<string> queryTerms,
        int maxSnippets = 3,
        int snippetLength = 150)
    {
        var snippets = new List<HighlightedSnippet>();
        var queryTermSet = new HashSet<string>(queryTerms.Select(t => t.ToLowerInvariant()));
        var matchPositions = FindMatchPositions(content, queryTermSet);

        if (matchPositions.Count == 0)
        {
            snippets.Add(new HighlightedSnippet
            {
                Text = TruncateText(content, snippetLength),
                MatchPositions = new List<(int, int)>()
            });
            return snippets;
        }

        var clusters = ClusterMatches(matchPositions, snippetLength);

        foreach (var cluster in clusters.Take(maxSnippets))
        {
            var centerPos = cluster.Sum(m => m.Start) / cluster.Count;
            var snippetStart = Math.Max(0, centerPos - snippetLength / 2);
            var snippetEnd = Math.Min(content.Length, snippetStart + snippetLength);

            (snippetStart, snippetEnd) = AdjustToWordBoundaries(content, snippetStart, snippetEnd);

            var snippet = ExtractSnippet(content, snippetStart, snippetEnd);
            var highlighted = HighlightTerms(snippet, queryTermSet, "<mark>", "</mark>");

            snippets.Add(new HighlightedSnippet
            {
                Text = highlighted,
                SnippetStart = snippetStart,
                SnippetEnd = snippetEnd,
                TotalMatches = cluster.Count
            });
        }

        return snippets;
    }

    private List<MatchPosition> FindMatchPositions(string content, HashSet<string> queryTerms)
    {
        var positions = new List<MatchPosition>();
        var normalizedContent = content.ToLowerInvariant();

        foreach (var term in queryTerms)
        {
            var index = 0;
            while ((index = normalizedContent.IndexOf(term, index, StringComparison.Ordinal)) != -1)
            {
                var isWordStart = index == 0 || !char.IsLetterOrDigit(normalizedContent[index - 1]);
                var isWordEnd = index + term.Length >= normalizedContent.Length ||
                               !char.IsLetterOrDigit(normalizedContent[index + term.Length]);

                if (isWordStart && isWordEnd)
                {
                    positions.Add(new MatchPosition
                    {
                        Start = index,
                        End = index + term.Length,
                        Term = term
                    });
                }

                index++;
            }
        }

        return positions.OrderBy(p => p.Start).ToList();
    }

    private (int Start, int End) FindBestSnippetRange(List<MatchPosition> positions, int snippetLength, int contentLength)
    {
        if (positions.Count == 0)
            return (0, Math.Min(snippetLength, contentLength));

        var bestStart = 0;
        var bestMatchCount = 0;

        for (int i = 0; i < positions.Count; i++)
        {
            var windowStart = Math.Max(0, positions[i].Start - 50);
            var windowEnd = windowStart + snippetLength;

            var matchCount = positions.Count(p => p.Start >= windowStart && p.End <= windowEnd);

            if (matchCount > bestMatchCount)
            {
                bestMatchCount = matchCount;
                bestStart = windowStart;
            }
        }

        var start = bestStart;
        var end = Math.Min(contentLength, start + snippetLength);

        return (start, end);
    }

    private string ExtractSnippet(string content, int start, int end)
    {
        (start, end) = AdjustToWordBoundaries(content, start, end);

        var snippet = content.Substring(start, end - start);

        var prefix = start > 0 ? "..." : "";
        var suffix = end < content.Length ? "..." : "";

        return prefix + snippet.Trim() + suffix;
    }

    private (int Start, int End) AdjustToWordBoundaries(string content, int start, int end)
    {
        while (start > 0 && !char.IsWhiteSpace(content[start - 1]))
        {
            start--;
        }

        while (end < content.Length && !char.IsWhiteSpace(content[end]))
        {
            end++;
        }

        return (start, end);
    }

    private string HighlightTerms(string text, HashSet<string> terms, string highlightStart, string highlightEnd)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var sortedTerms = terms.OrderByDescending(t => t.Length).ToList();

        foreach (var term in sortedTerms)
        {
            var pattern = $@"\b({Regex.Escape(term)})\b";
            text = Regex.Replace(text, pattern, $"{highlightStart}$1{highlightEnd}", RegexOptions.IgnoreCase);
        }

        return text;
    }

    private List<List<MatchPosition>> ClusterMatches(List<MatchPosition> positions, int clusterDistance)
    {
        var clusters = new List<List<MatchPosition>>();
        if (positions.Count == 0) return clusters;

        var currentCluster = new List<MatchPosition> { positions[0] };

        for (int i = 1; i < positions.Count; i++)
        {
            if (positions[i].Start - currentCluster.Last().End < clusterDistance)
            {
                currentCluster.Add(positions[i]);
            }
            else
            {
                clusters.Add(currentCluster);
                currentCluster = new List<MatchPosition> { positions[i] };
            }
        }

        clusters.Add(currentCluster);

        return clusters.OrderByDescending(c => c.Count).ToList();
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;

        var truncated = text.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > maxLength * 0.8)
        {
            truncated = truncated.Substring(0, lastSpace);
        }

        return truncated + "...";
    }
}

public class MatchPosition
{
    public int Start { get; set; }
    public int End { get; set; }
    public string Term { get; set; } = string.Empty;
}

public class HighlightedSnippet
{
    public string Text { get; set; } = string.Empty;
    public List<(int Start, int End)> MatchPositions { get; set; } = new();
    public int SnippetStart { get; set; }
    public int SnippetEnd { get; set; }
    public int TotalMatches { get; set; }
}

public interface IResultHighlighter
{
    HighlightedSnippet GetHighlightedSnippet(
        string content,
        List<string> queryTerms,
        int snippetLength = 200,
        string highlightStart = "<mark>",
        string highlightEnd = "</mark>");

    string HighlightFullContent(
        string content,
        List<string> queryTerms,
        string highlightStart = "<mark>",
        string highlightEnd = "</mark>");

    List<HighlightedSnippet> GetMultipleSnippets(
        string content,
        List<string> queryTerms,
        int maxSnippets = 3,
        int snippetLength = 150);
}
