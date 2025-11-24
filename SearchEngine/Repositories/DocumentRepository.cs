using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly Dictionary<Guid, Document> _documents;
    private readonly object _lock = new object();

    public DocumentRepository()
    {
        _documents = new Dictionary<Guid, Document>();
    }

    public void Add(Document document)
    {
        lock (_lock)
        {
            if (!_documents.ContainsKey(document.Id))
            {
                _documents[document.Id] = document;
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
            }
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            _documents.Remove(id);
        }
    }

    public bool Exists(Guid id)
    {
        lock (_lock)
        {
            return _documents.ContainsKey(id);
        }
    }
}
