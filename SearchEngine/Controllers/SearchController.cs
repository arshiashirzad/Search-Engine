using Microsoft.AspNetCore.Mvc;
using SearchEngine.Interfaces;

namespace SearchEngine.Controllers;

public class SearchController : Controller
{
    private readonly ISearchEngineService _searchEngineService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IInvertedIndex _invertedIndex;

    public SearchController(
        ISearchEngineService searchEngineService,
        IDocumentRepository documentRepository,
        IInvertedIndex invertedIndex)
    {
        _searchEngineService = searchEngineService;
        _documentRepository = documentRepository;
        _invertedIndex = invertedIndex;
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
    
    public IActionResult ViewIndex()
    {
        var terms = _invertedIndex.GetAllTerms();
        var stats = _invertedIndex.GetTermStatistics();
        
        ViewBag.TotalTerms = terms.Count;
        ViewBag.Stats = stats;
        
        return View(terms);
    }
}
