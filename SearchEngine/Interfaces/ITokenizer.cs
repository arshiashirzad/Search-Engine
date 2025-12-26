namespace SearchEngine.Interfaces;

public interface ITokenizer
{
    List<string> Tokenize(string text);
    List<string> TokenizeWithStemming(string text);
    List<string> GenerateKGrams(string term, int k = 3);
    string Normalize(string text);
    string Stem(string word);
}
