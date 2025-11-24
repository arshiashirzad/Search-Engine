using Microsoft.AspNetCore.Mvc;
using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Controllers;

public class DocumentController : Controller
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ISearchEngineService _searchEngineService;
    private readonly IFileStorageService _fileStorageService;

    public DocumentController(
        IDocumentRepository documentRepository,
        ISearchEngineService searchEngineService,
        IFileStorageService fileStorageService)
    {
        _documentRepository = documentRepository;
        _searchEngineService = searchEngineService;
        _fileStorageService = fileStorageService;
    }

    public IActionResult Index()
    {
        var documents = _documentRepository.GetAll();
        return View(documents);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Document document, IFormFile? file)
    {
        if (file != null && file.Length > 0)
        {
            document.Id = Guid.NewGuid();
            document.DateAdded = DateTime.UtcNow;
            document.IsIndexed = false;
            document.FileName = file.FileName;
            document.FileSize = file.Length;

            document.FilePath = await _fileStorageService.SaveFileAsync(file, document.Id);
            
            document.Content = await _fileStorageService.ReadFileContentAsync(document.FilePath);
            
            if (string.IsNullOrWhiteSpace(document.Title))
            {
                document.Title = Path.GetFileNameWithoutExtension(file.FileName);
            }
        }
        else if (!string.IsNullOrWhiteSpace(document.Content))
        {
            document.Id = Guid.NewGuid();
            document.DateAdded = DateTime.UtcNow;
            document.IsIndexed = false;
        }
        else
        {
            ModelState.AddModelError("", "Please either upload a file or enter content manually.");
            return View(document);
        }

        _documentRepository.Add(document);
        return RedirectToAction("Index");
    }

    public IActionResult Details(Guid id)
    {
        var document = _documentRepository.GetById(id);
        if (document == null)
            return NotFound();
        
        return View(document);
    }

    [HttpPost]
    public IActionResult Delete(Guid id)
    {
        var document = _documentRepository.GetById(id);
        if (document != null && !string.IsNullOrEmpty(document.FilePath))
        {
            _fileStorageService.DeleteFile(document.FilePath);
        }
        _documentRepository.Delete(id);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult IndexDocument(Guid id)
    {
        _searchEngineService.IndexDocument(id);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult IndexAll()
    {
        _searchEngineService.IndexAllDocuments();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult ClearIndex()
    {
        _searchEngineService.ClearIndex();
        return RedirectToAction("Index");
    }
}
