using System.Text;
using System.Text.RegularExpressions;
using SearchEngine.Interfaces;

namespace SearchEngine.Services;

public class Tokenizer : ITokenizer
{
    private readonly HashSet<string> _stopWords;
    private readonly HashSet<string> _stopNumbers;

    public Tokenizer()
    {
        _stopWords = new HashSet<string>
        {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from",
            "has", "he", "in", "is", "it", "its", "of", "on", "that", "the",
            "to", "was", "were", "will", "with", "this", "but", "they", "have",
            "been", "being", "had", "having", "do", "does", "did", "doing",
            "would", "should", "could", "ought", "i", "you", "we", "she",
            "him", "her", "their", "them", "what", "which", "who", "when",
            "where", "why", "how", "all", "each", "every", "both", "few",
            "more", "most", "other", "some", "such", "no", "nor", "not",
            "only", "own", "same", "so", "than", "too", "very", "can", "just"
        };

        // Filter out meaningless standalone numbers (0-100)
        _stopNumbers = new HashSet<string>();
        for (int i = 0; i <= 100; i++)
        {
            _stopNumbers.Add(i.ToString());
        }
    }

    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.ToLowerInvariant();
        
        // Keep alphanumeric, hyphens, underscores, and whitespace
        // This preserves: COVID-19, python_3, data-science, etc.
        text = Regex.Replace(text, @"[^\w\s\-]", " ");
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    public List<string> Tokenize(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrEmpty(normalized))
            return new List<string>();

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !IsStopToken(token))
            .ToList();

        return tokens;
    }

    private bool IsStopToken(string token)
    {
        // Minimum length requirement
        if (token.Length < 2)
            return true;

        // Maximum length (reject garbage merged words like 'whichoutperformedmostoftheexistingworkapartfromglobaljittering')
        if (token.Length > 30)
            return true;

        // Filter stop words
        if (_stopWords.Contains(token))
            return true;

        // REJECT all pure numbers (including ranges like 0-4, 10-20)
        if (Regex.IsMatch(token, @"^[\d\-]+$"))
            return true; // Filters: 0, 10, 100, 0-4, 10-20, etc.

        // REJECT tokens that are mostly numbers (95%+ digits)
        var digitCount = token.Count(char.IsDigit);
        if (digitCount > 0 && (double)digitCount / token.Length > 0.95)
            return true;

        // Keep meaningful alphanumeric terms (must have at least 2 letters)
        var letterCount = token.Count(char.IsLetter);
        if (letterCount < 2)
            return true; // Filters: 1a, 2b, 3-, etc.

        // Keep technical terms with numbers (covid-19, h2o, ipv6, python3)
        // These pass because they have 2+ letters and aren't pure numbers

        return false;
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
