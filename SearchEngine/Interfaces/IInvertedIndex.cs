using SearchEngine.Models;

namespace SearchEngine.Interfaces;

public interface IInvertedIndex
{
    void AddDocument(Document document, List<string> tokens);
    HashSet<Guid> Search(string term);
    HashSet<Guid> SearchPhrase(List<string> terms);
    void Clear();
    int GetTermCount();
    List<string> GetAllTerms();
    Dictionary<string, int> GetTermStatistics();
}
