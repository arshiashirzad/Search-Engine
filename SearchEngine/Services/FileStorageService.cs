using SearchEngine.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

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
            return await ExtractPdfTextAsync(filePath);
        }
        else if (extension == ".doc" || extension == ".docx")
        {
            return "[Word documents (.doc/.docx) are not yet supported. Please convert to .txt or .pdf]";
        }
        else
        {
            try
            {
                return await File.ReadAllTextAsync(filePath);
            }
            catch
            {
                return "[Unable to read file content]";
            }
        }
    }

    private async Task<string> ExtractPdfTextAsync(string filePath)
    {
        try
        {
            using (var pdfReader = new PdfReader(filePath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                var text = new System.Text.StringBuilder();

                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new LocationTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

                    text.AppendLine(pageText);
                    text.AppendLine();
                }

                return text.ToString();
            }
        }
        catch (Exception ex)
        {
            return $"[Error extracting PDF content: {ex.Message}]";
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
