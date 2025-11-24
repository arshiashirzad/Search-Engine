using SearchEngine.Interfaces;

namespace SearchEngine.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _storagePath = Path.Combine(environment.ContentRootPath, "UploadedFiles");
        
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, Guid documentId)
    {
        var fileExtension = Path.GetExtension(file.FileName);
        var fileName = $"{documentId}{fileExtension}";
        var filePath = Path.Combine(_storagePath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return filePath;
    }

    public async Task<string> ReadFileContentAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        var extension = Path.GetExtension(filePath).ToLower();

        if (extension == ".txt" || extension == ".md")
        {
            return await File.ReadAllTextAsync(filePath);
        }
        else if (extension == ".pdf")
        {
            return "PDF content extraction not implemented";
        }
        else
        {
            return await File.ReadAllTextAsync(filePath);
        }
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public string GetStoragePath()
    {
        return _storagePath;
    }
}
