using System.Text.Json;
using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly Dictionary<Guid, Document> _documents;
    private readonly object _lock = new object();
    private readonly string _dataFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public DocumentRepository()
    {
        _documents = new Dictionary<Guid, Document>();

        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        _dataFilePath = Path.Combine(dataDir, "documents.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        LoadFromFile();
    }

    public void Add(Document document)
    {
        lock (_lock)
        {
            if (!_documents.ContainsKey(document.Id))
            {
                _documents[document.Id] = document;
                SaveToFile();
            }
        }
    }

    public Document? GetById(Guid id)
    {
        lock (_lock)
        {
            return _documents.TryGetValue(id, out var document) ? document : null;
        }
    }

    public List<Document> GetAll()
    {
        lock (_lock)
        {
            return _documents.Values.OrderByDescending(d => d.DateAdded).ToList();
        }
    }

    public void Update(Document document)
    {
        lock (_lock)
        {
            if (_documents.ContainsKey(document.Id))
            {
                _documents[document.Id] = document;
                SaveToFile();
            }
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            if (_documents.Remove(id))
            {
                SaveToFile();
            }
        }
    }

    public bool Exists(Guid id)
    {
        lock (_lock)
        {
            return _documents.ContainsKey(id);
        }
    }

    private void SaveToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(_documents.Values.ToList(), _jsonOptions);
            File.WriteAllText(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving documents: {ex.Message}");
        }
    }

    private void LoadFromFile()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var documents = JsonSerializer.Deserialize<List<Document>>(json, _jsonOptions);

                if (documents != null)
                {
                    foreach (var doc in documents)
                    {
                        _documents[doc.Id] = doc;
                    }
                    Console.WriteLine($"Loaded {documents.Count} documents from storage.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading documents: {ex.Message}");
        }
    }
}
