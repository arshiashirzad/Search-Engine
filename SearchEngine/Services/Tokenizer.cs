using System.Text;
using System.Text.RegularExpressions;
using SearchEngine.Interfaces;
using SearchEngine.TextProcessing;

namespace SearchEngine.Services;

public class Tokenizer : ITokenizer
{
    private readonly HashSet<string> _stopWords;
    private readonly HashSet<string> _stopNumbers;
    private readonly PorterStemmer _stemmer;

    public Tokenizer()
    {
        _stemmer = new PorterStemmer();

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
        if (token.Length < 2)
            return true;

        if (token.Length > 30)
            return true;

        if (_stopWords.Contains(token))
            return true;

        if (Regex.IsMatch(token, @"^[\d\-]+$"))
            return true;

        var digitCount = token.Count(char.IsDigit);
        if (digitCount > 0 && (double)digitCount / token.Length > 0.95)
            return true;

        var letterCount = token.Count(char.IsLetter);
        if (letterCount < 2)
            return true;

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

    public string Stem(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return word;
        return _stemmer.Stem(word.ToLowerInvariant());
    }

    public List<string> TokenizeWithStemming(string text)
    {
        var tokens = Tokenize(text);
        return tokens.Select(t => _stemmer.Stem(t)).ToList();
    }
}
