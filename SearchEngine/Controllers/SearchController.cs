using Microsoft.AspNetCore.Mvc;
using SearchEngine.Interfaces;
using SearchEngine.Services;

namespace SearchEngine.Controllers;

public class SearchController : Controller
{
    private readonly ISearchEngineService _searchEngineService;
    private readonly IAdvancedSearchEngine _advancedSearchEngine;
    private readonly IDocumentRepository _documentRepository;
    private readonly IInvertedIndex _invertedIndex;

    public SearchController(
        ISearchEngineService searchEngineService,
        IAdvancedSearchEngine advancedSearchEngine,
        IDocumentRepository documentRepository,
        IInvertedIndex invertedIndex)
    {
        _searchEngineService = searchEngineService;
        _advancedSearchEngine = advancedSearchEngine;
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

        if (!results.Any())
        {
            var suggestions = _advancedSearchEngine.GetSpellingSuggestions(query);
            if (suggestions.Any() && suggestions[0].EditDistance <= 2)
            {
                ViewBag.SpellingSuggestion = suggestions[0].Term;
            }
        }

        return View("Index", results);
    }

    [HttpGet]
    public IActionResult AdvancedSearch(string query, bool enableSpellCheck = true, int page = 0)
    {
        ViewBag.TotalDocuments = _documentRepository.GetAll().Count;
        ViewBag.IndexedTerms = _searchEngineService.GetIndexedTermCount();

        if (string.IsNullOrWhiteSpace(query))
        {
            return View("Advanced");
        }

        var request = new SearchRequest
        {
            Query = query,
            EnableSpellCheck = enableSpellCheck,
            EnableHighlighting = true,
            Page = page,
            PageSize = 10
        };

        var result = _advancedSearchEngine.Search(request);
        ViewBag.Query = query;

        return View("Advanced", result);
    }

    public IActionResult Analytics()
    {
        var analytics = _advancedSearchEngine.GetAnalytics();
        return View(analytics);
    }

    public IActionResult ViewIndex()
    {
        var terms = _invertedIndex.GetAllTerms();
        var stats = _invertedIndex.GetTermStatistics();

        ViewBag.TotalTerms = terms.Count;
        ViewBag.Stats = stats;

        return View(terms);
    }

    public IActionResult TreeVisualization()
    {
        ViewBag.TreeVisualization = _invertedIndex.GetTreeVisualization();
        return View();
    }

    [HttpPost]
    public IActionResult RebuildSpellIndex()
    {
        _advancedSearchEngine.RebuildSpellCheckIndex();
        return Json(new { success = true });
    }
}
