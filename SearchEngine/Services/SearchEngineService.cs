using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Services;

public class SearchEngineService : ISearchEngineService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IInvertedIndex _invertedIndex;
    private readonly ITokenizer _tokenizer;

    public SearchEngineService(
        IDocumentRepository documentRepository,
        IInvertedIndex invertedIndex,
        ITokenizer tokenizer)
    {
        _documentRepository = documentRepository;
        _invertedIndex = invertedIndex;
        _tokenizer = tokenizer;
    }

    public void IndexDocument(Guid documentId)
    {
        var document = _documentRepository.GetById(documentId);
        if (document == null)
            return;

        var titleTokens = _tokenizer.Tokenize(document.Title);
        var contentTokens = _tokenizer.Tokenize(document.Content);
        
        var allTokens = new List<string>();
        allTokens.AddRange(titleTokens);
        allTokens.AddRange(contentTokens);

        _invertedIndex.AddDocument(document, allTokens);
        
        document.IsIndexed = true;
        _documentRepository.Update(document);
    }

    public void IndexAllDocuments()
    {
        var documents = _documentRepository.GetAll();
        _invertedIndex.Clear();
        
        foreach (var document in documents)
        {
            IndexDocument(document.Id);
        }
    }

    public List<SearchResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResult>();

        var queryTokens = _tokenizer.Tokenize(query);
        if (queryTokens.Count == 0)
            return new List<SearchResult>();

        HashSet<Guid> matchingDocIds;

        if (queryTokens.Count == 1)
        {
            matchingDocIds = _invertedIndex.Search(queryTokens[0]);
        }
        else
        {
            matchingDocIds = _invertedIndex.SearchPhrase(queryTokens);
            
            if (matchingDocIds.Count == 0)
            {
                matchingDocIds = new HashSet<Guid>();
                foreach (var token in queryTokens)
                {
                    var tokenResults = _invertedIndex.Search(token);
                    matchingDocIds.UnionWith(tokenResults);
                }
            }
        }

        var results = new List<SearchResult>();
        foreach (var docId in matchingDocIds)
        {
            var document = _documentRepository.GetById(docId);
            if (document != null)
            {
                var relevance = CalculateRelevance(document, queryTokens);
                results.Add(new SearchResult
                {
                    Document = document,
                    Relevance = relevance
                });
            }
        }

        return results.OrderByDescending(r => r.Relevance).ToList();
    }

    private double CalculateRelevance(Document document, List<string> queryTokens)
    {
        var titleTokens = _tokenizer.Tokenize(document.Title);
        var contentTokens = _tokenizer.Tokenize(document.Content);
        
        double score = 0;

        foreach (var queryToken in queryTokens)
        {
            var titleMatches = titleTokens.Count(t => t == queryToken);
            var contentMatches = contentTokens.Count(t => t == queryToken);
            
            score += titleMatches * 2.0;
            score += contentMatches * 1.0;
        }

        return score;
    }

    public void ClearIndex()
    {
        _invertedIndex.Clear();
        
        var documents = _documentRepository.GetAll();
        foreach (var document in documents)
        {
            document.IsIndexed = false;
            _documentRepository.Update(document);
        }
    }

    public int GetIndexedTermCount()
    {
        return _invertedIndex.GetTermCount();
    }
}
