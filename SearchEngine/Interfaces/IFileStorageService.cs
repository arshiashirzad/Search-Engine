namespace SearchEngine.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, Guid documentId);
    Task<string> ReadFileContentAsync(string filePath);
    void DeleteFile(string filePath);
    bool FileExists(string filePath);
    string GetStoragePath();
}
