using Microsoft.AspNetCore.Mvc;
using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Controllers;

public class DocumentController : Controller
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ISearchEngineService _searchEngineService;

    public DocumentController(
        IDocumentRepository documentRepository,
        ISearchEngineService searchEngineService)
    {
        _documentRepository = documentRepository;
        _searchEngineService = searchEngineService;
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
    public IActionResult Create(Document document)
    {
        if (ModelState.IsValid)
        {
            document.Id = Guid.NewGuid();
            document.DateAdded = DateTime.UtcNow;
            document.IsIndexed = false;
            
            _documentRepository.Add(document);
            
            return RedirectToAction("Index");
        }
        return View(document);
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
