using System.Text;
using System.Text.RegularExpressions;
using SearchEngine.Interfaces;

namespace SearchEngine.Services;

public class Tokenizer : ITokenizer
{
    private readonly HashSet<string> _stopWords;

    public Tokenizer()
    {
        _stopWords = new HashSet<string>
        {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
            "has", "he", "in", "is", "it", "its", "of", "on", "that", "the",
            "to", "was", "were", "will", "with", "this", "but", "they", "have"
        };
    }

    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.ToLowerInvariant();
        text = Regex.Replace(text, @"[^\w\s]", " ");
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    public List<string> Tokenize(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrEmpty(normalized))
            return new List<string>();

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !_stopWords.Contains(token))
            .ToList();

        return tokens;
    }

    public List<string> GenerateKGrams(string term, int k = 3)
    {
        var kgrams = new List<string>();
        
        if (string.IsNullOrEmpty(term) || term.Length < k)
            return kgrams;

        var paddedTerm = $"${term}$";
        
        for (int i = 0; i <= paddedTerm.Length - k; i++)
        {
            kgrams.Add(paddedTerm.Substring(i, k));
        }

        return kgrams;
    }
}
