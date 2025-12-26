using System.Text.Json;
using SearchEngine.DataStructures;
using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Services;

public class InvertedIndex : IInvertedIndex
{
    private BPlusTree<string, PostingList> _index;
    private readonly Dictionary<Guid, Dictionary<string, List<int>>> _documentPositions;
    private readonly HashSet<string> _allTerms;
    private readonly string _indexFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public InvertedIndex()
    {
        _index = new BPlusTree<string, PostingList>(4);
        _documentPositions = new Dictionary<Guid, Dictionary<string, List<int>>>();
        _allTerms = new HashSet<string>();

        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        _indexFilePath = Path.Combine(dataDir, "inverted_index.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        LoadFromFile();
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
            _allTerms.Add(term);

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

        SaveToFile();
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
        _index = new BPlusTree<string, PostingList>(4);
        _documentPositions.Clear();
        _allTerms.Clear();
        SaveToFile();
    }

    public int GetTermCount()
    {
        return _allTerms.Count;
    }

    public List<string> GetAllTerms()
    {
        return _allTerms.OrderBy(t => t).ToList();
    }

    public Dictionary<string, int> GetTermStatistics()
    {
        var stats = new Dictionary<string, int>();
        foreach (var term in _allTerms)
        {
            var postingList = _index.Search(term);
            if (postingList != null)
            {
                stats[term] = postingList.DocumentIds.Count;
            }
        }
        return stats;
    }

    public TreeVisualization GetTreeVisualization()
    {
        return _index.GetTreeVisualization();
    }

    private void SaveToFile()
    {
        try
        {
            var data = new IndexPersistenceData
            {
                Terms = _allTerms.ToList(),
                PostingLists = new Dictionary<string, List<Guid>>(),
                DocumentPositions = _documentPositions.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value
                )
            };

            foreach (var term in _allTerms)
            {
                var postingList = _index.Search(term);
                if (postingList != null)
                {
                    data.PostingLists[term] = postingList.DocumentIds;
                }
            }

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(_indexFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving index: {ex.Message}");
        }
    }

    private void LoadFromFile()
    {
        try
        {
            if (File.Exists(_indexFilePath))
            {
                var json = File.ReadAllText(_indexFilePath);
                var data = JsonSerializer.Deserialize<IndexPersistenceData>(json, _jsonOptions);

                if (data != null)
                {
                    foreach (var term in data.Terms)
                    {
                        _allTerms.Add(term);

                        if (data.PostingLists.TryGetValue(term, out var docIds))
                        {
                            var postingList = new PostingList { DocumentIds = docIds };
                            _index.Insert(term, postingList);
                        }
                    }

                    foreach (var kvp in data.DocumentPositions)
                    {
                        if (Guid.TryParse(kvp.Key, out var docId))
                        {
                            _documentPositions[docId] = kvp.Value;
                        }
                    }

                    Console.WriteLine($"Loaded index with {_allTerms.Count} terms from storage.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading index: {ex.Message}");
        }
    }
}

public class IndexPersistenceData
{
    public List<string> Terms { get; set; } = new();
    public Dictionary<string, List<Guid>> PostingLists { get; set; } = new();
    public Dictionary<string, Dictionary<string, List<int>>> DocumentPositions { get; set; } = new();
}

public class PostingList
{
    public List<Guid> DocumentIds { get; set; }

    public PostingList()
    {
        DocumentIds = new List<Guid>();
    }
}
