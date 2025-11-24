namespace SearchEngine.Models;

public class Document
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
    public bool IsIndexed { get; set; }

    public Document()
    {
        Id = Guid.NewGuid();
        DateAdded = DateTime.UtcNow;
        IsIndexed = false;
    }
}
