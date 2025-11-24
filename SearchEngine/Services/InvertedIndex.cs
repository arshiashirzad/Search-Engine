using SearchEngine.DataStructures;
using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Services;

public class InvertedIndex : IInvertedIndex
{
    private readonly BPlusTree<string, PostingList> _index;
    private readonly Dictionary<Guid, Dictionary<string, List<int>>> _documentPositions;

    public InvertedIndex()
    {
        _index = new BPlusTree<string, PostingList>(4);
        _documentPositions = new Dictionary<Guid, Dictionary<string, List<int>>>();
    }

    public void AddDocument(Document document, List<string> tokens)
    {
        if (!_documentPositions.ContainsKey(document.Id))
        {
            _documentPositions[document.Id] = new Dictionary<string, List<int>>();
        }

        for (int position = 0; position < tokens.Count; position++)
        {
            var term = tokens[position];
            
            var postingList = _index.Search(term);
            if (postingList == null)
            {
                postingList = new PostingList();
                _index.Insert(term, postingList);
            }

            if (!postingList.DocumentIds.Contains(document.Id))
            {
                postingList.DocumentIds.Add(document.Id);
            }

            if (!_documentPositions[document.Id].ContainsKey(term))
            {
                _documentPositions[document.Id][term] = new List<int>();
            }
            _documentPositions[document.Id][term].Add(position);
        }
    }

    public HashSet<Guid> Search(string term)
    {
        var postingList = _index.Search(term);
        return postingList != null 
            ? new HashSet<Guid>(postingList.DocumentIds) 
            : new HashSet<Guid>();
    }

    public HashSet<Guid> SearchPhrase(List<string> terms)
    {
        if (terms.Count == 0)
            return new HashSet<Guid>();

        var result = Search(terms[0]);
        
        if (terms.Count == 1)
            return result;

        var candidates = new HashSet<Guid>(result);
        
        foreach (var docId in candidates.ToList())
        {
            if (!IsPhrasePresentInDocument(docId, terms))
            {
                result.Remove(docId);
            }
        }

        return result;
    }

    private bool IsPhrasePresentInDocument(Guid documentId, List<string> terms)
    {
        if (!_documentPositions.ContainsKey(documentId))
            return false;

        var docPositions = _documentPositions[documentId];
        
        if (!docPositions.ContainsKey(terms[0]))
            return false;

        var firstTermPositions = docPositions[terms[0]];

        foreach (var startPos in firstTermPositions)
        {
            bool phraseFound = true;
            
            for (int i = 1; i < terms.Count; i++)
            {
                var term = terms[i];
                if (!docPositions.ContainsKey(term))
                {
                    phraseFound = false;
                    break;
                }

                var expectedPosition = startPos + i;
                if (!docPositions[term].Contains(expectedPosition))
                {
                    phraseFound = false;
                    break;
                }
            }

            if (phraseFound)
                return true;
        }

        return false;
    }

    public void Clear()
    {
        _documentPositions.Clear();
    }

    public int GetTermCount()
    {
        return _documentPositions.Values.Sum(d => d.Keys.Count);
    }
}

public class PostingList
{
    public List<Guid> DocumentIds { get; set; }

    public PostingList()
    {
        DocumentIds = new List<Guid>();
    }
}
