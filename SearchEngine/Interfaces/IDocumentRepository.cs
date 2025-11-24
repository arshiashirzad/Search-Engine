using SearchEngine.Models;

namespace SearchEngine.Interfaces;

public interface IDocumentRepository
{
    void Add(Document document);
    Document? GetById(Guid id);
    List<Document> GetAll();
    void Update(Document document);
    void Delete(Guid id);
    bool Exists(Guid id);
}
