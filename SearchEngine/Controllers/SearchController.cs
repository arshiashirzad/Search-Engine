using Microsoft.AspNetCore.Mvc;
using SearchEngine.Interfaces;

namespace SearchEngine.Controllers;

public class SearchController : Controller
{
    private readonly ISearchEngineService _searchEngineService;
    private readonly IDocumentRepository _documentRepository;

    public SearchController(
        ISearchEngineService searchEngineService,
        IDocumentRepository documentRepository)
    {
        _searchEngineService = searchEngineService;
        _documentRepository = documentRepository;
    }

    public IActionResult Index()
    {
        ViewBag.TotalDocuments = _documentRepository.GetAll().Count;
        ViewBag.IndexedTerms = _searchEngineService.GetIndexedTermCount();
        return View();
    }

    [HttpGet]
    public IActionResult Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return View("Index");
        }

        var results = _searchEngineService.Search(query);
        ViewBag.Query = query;
        ViewBag.TotalDocuments = _documentRepository.GetAll().Count;
        ViewBag.IndexedTerms = _searchEngineService.GetIndexedTermCount();
        
        return View("Index", results);
    }
}
