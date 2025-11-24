using SearchEngine.Models;

namespace SearchEngine.Interfaces;

public interface ISearchEngineService
{
    void IndexDocument(Guid documentId);
    void IndexAllDocuments();
    List<SearchResult> Search(string query);
    void ClearIndex();
    int GetIndexedTermCount();
}
