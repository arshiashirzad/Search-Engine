namespace SearchEngine.TextProcessing;

public class PorterStemmer : IStemmer
{
    public string Stem(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 3)
            return word;

        word = word.ToLowerInvariant();

        if (!word.All(char.IsLetter))
            return word;

        word = Step1a(word);
        word = Step1b(word);
        word = Step1c(word);
        word = Step2(word);
        word = Step3(word);
        word = Step4(word);
        word = Step5a(word);
        word = Step5b(word);

        return word;
    }

    public List<string> StemAll(IEnumerable<string> words)
    {
        return words.Select(Stem).ToList();
    }

    #region Step Implementations

    private string Step1a(string word)
    {
        if (word.EndsWith("sses"))
            return word[..^2];
        if (word.EndsWith("ies"))
            return word[..^2];
        if (word.EndsWith("ss"))
            return word;
        if (word.EndsWith("s"))
            return word[..^1];
        return word;
    }

    private string Step1b(string word)
    {
        if (word.EndsWith("eed"))
        {
            var stem = word[..^3];
            if (Measure(stem) > 0)
                return stem + "ee";
            return word;
        }

        var modified = false;

        if (word.EndsWith("ed"))
        {
            var stem = word[..^2];
            if (ContainsVowel(stem))
            {
                word = stem;
                modified = true;
            }
        }
        else if (word.EndsWith("ing"))
        {
            var stem = word[..^3];
            if (ContainsVowel(stem))
            {
                word = stem;
                modified = true;
            }
        }

        if (modified)
        {
            if (word.EndsWith("at") || word.EndsWith("bl") || word.EndsWith("iz"))
                return word + "e";

            if (EndsWithDoubleConsonant(word) && !word.EndsWith("l") && !word.EndsWith("s") && !word.EndsWith("z"))
                return word[..^1];

            if (Measure(word) == 1 && EndsWithCVC(word))
                return word + "e";
        }

        return word;
    }

    private string Step1c(string word)
    {
        if (word.EndsWith("y"))
        {
            var stem = word[..^1];
            if (ContainsVowel(stem))
                return stem + "i";
        }
        return word;
    }

    private string Step2(string word)
    {
        var suffixes = new Dictionary<string, string>
        {
            { "ational", "ate" },
            { "tional", "tion" },
            { "enci", "ence" },
            { "anci", "ance" },
            { "izer", "ize" },
            { "abli", "able" },
            { "alli", "al" },
            { "entli", "ent" },
            { "eli", "e" },
            { "ousli", "ous" },
            { "ization", "ize" },
            { "ation", "ate" },
            { "ator", "ate" },
            { "alism", "al" },
            { "iveness", "ive" },
            { "fulness", "ful" },
            { "ousness", "ous" },
            { "aliti", "al" },
            { "iviti", "ive" },
            { "biliti", "ble" },
            { "logi", "log" }
        };

        foreach (var (suffix, replacement) in suffixes)
        {
            if (word.EndsWith(suffix))
            {
                var stem = word[..^suffix.Length];
                if (Measure(stem) > 0)
                    return stem + replacement;
                return word;
            }
        }

        return word;
    }

    private string Step3(string word)
    {
        var suffixes = new Dictionary<string, string>
        {
            { "icate", "ic" },
            { "ative", "" },
            { "alize", "al" },
            { "iciti", "ic" },
            { "ical", "ic" },
            { "ful", "" },
            { "ness", "" }
        };

        foreach (var (suffix, replacement) in suffixes)
        {
            if (word.EndsWith(suffix))
            {
                var stem = word[..^suffix.Length];
                if (Measure(stem) > 0)
                    return stem + replacement;
                return word;
            }
        }

        return word;
    }

    private string Step4(string word)
    {
        var suffixes = new[]
        {
            "al", "ance", "ence", "er", "ic", "able", "ible", "ant",
            "ement", "ment", "ent", "ion", "ou", "ism", "ate", "iti",
            "ous", "ive", "ize"
        };

        foreach (var suffix in suffixes)
        {
            if (word.EndsWith(suffix))
            {
                var stem = word[..^suffix.Length];
                if (Measure(stem) > 1)
                {
                    if (suffix == "ion" && stem.Length > 0 && (stem.EndsWith("s") || stem.EndsWith("t")))
                        return stem;
                    if (suffix != "ion")
                        return stem;
                }
            }
        }

        return word;
    }

    private string Step5a(string word)
    {
        if (word.EndsWith("e"))
        {
            var stem = word[..^1];
            var m = Measure(stem);
            if (m > 1)
                return stem;
            if (m == 1 && !EndsWithCVC(stem))
                return stem;
        }
        return word;
    }

    private string Step5b(string word)
    {
        if (Measure(word) > 1 && EndsWithDoubleConsonant(word) && word.EndsWith("l"))
            return word[..^1];
        return word;
    }

    #endregion

    #region Helper Methods

    private bool IsConsonant(string word, int index)
    {
        if (index < 0 || index >= word.Length)
            return false;

        var c = word[index];

        if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u')
            return false;

        if (c == 'y')
            return index == 0 || !IsConsonant(word, index - 1);

        return true;
    }

    private bool ContainsVowel(string word)
    {
        for (int i = 0; i < word.Length; i++)
        {
            if (!IsConsonant(word, i))
                return true;
        }
        return false;
    }

    private int Measure(string word)
    {
        int n = 0;
        int i = 0;

        while (i < word.Length && IsConsonant(word, i))
            i++;

        while (i < word.Length)
        {
            while (i < word.Length && !IsConsonant(word, i))
                i++;

            if (i >= word.Length)
                break;

            while (i < word.Length && IsConsonant(word, i))
                i++;

            n++;
        }

        return n;
    }

    private bool EndsWithDoubleConsonant(string word)
    {
        if (word.Length < 2)
            return false;

        var last = word.Length - 1;
        return word[last] == word[last - 1] && IsConsonant(word, last);
    }

    private bool EndsWithCVC(string word)
    {
        if (word.Length < 3)
            return false;

        var last = word.Length - 1;

        if (!IsConsonant(word, last) || IsConsonant(word, last - 1) || !IsConsonant(word, last - 2))
            return false;

        var c = word[last];
        return c != 'w' && c != 'x' && c != 'y';
    }

    #endregion
}

public interface IStemmer
{
    string Stem(string word);
    List<string> StemAll(IEnumerable<string> words);
}
