using SearchEngine.Interfaces;
using SearchEngine.Models;

namespace SearchEngine.Facets;

public class FacetedSearch : IFacetedSearch
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ITokenizer _tokenizer;

    public FacetedSearch(IDocumentRepository documentRepository, ITokenizer tokenizer)
    {
        _documentRepository = documentRepository;
        _tokenizer = tokenizer;
    }

    public FacetResults ExtractFacets(IEnumerable<Document> documents)
    {
        var docs = documents.ToList();
        var results = new FacetResults();

        results.FileTypeFacets = docs
            .Where(d => !string.IsNullOrEmpty(d.FileName))
            .GroupBy(d => GetFileExtension(d.FileName!))
            .Select(g => new FacetValue
            {
                Value = g.Key,
                DisplayName = GetFileTypeDisplayName(g.Key),
                Count = g.Count()
            })
            .OrderByDescending(f => f.Count)
            .ToList();

        results.DateFacets = docs
            .GroupBy(d => new DateTime(d.DateAdded.Year, d.DateAdded.Month, 1))
            .Select(g => new FacetValue
            {
                Value = g.Key.ToString("yyyy-MM"),
                DisplayName = g.Key.ToString("MMMM yyyy"),
                Count = g.Count()
            })
            .OrderByDescending(f => f.Value)
            .ToList();

        results.SizeFacets = docs
            .GroupBy(d => GetSizeCategory(d.FileSize))
            .Select(g => new FacetValue
            {
                Value = g.Key.ToString(),
                DisplayName = GetSizeCategoryDisplayName(g.Key),
                Count = g.Count()
            })
            .OrderBy(f => int.Parse(f.Value))
            .ToList();

        var allTerms = new Dictionary<string, int>();
        foreach (var doc in docs)
        {
            var terms = _tokenizer.Tokenize(doc.Title + " " + doc.Content);
            foreach (var term in terms.Distinct())
            {
                if (!allTerms.ContainsKey(term))
                    allTerms[term] = 0;
                allTerms[term]++;
            }
        }

        results.TermFacets = allTerms
            .Where(kv => kv.Value >= 2)
            .OrderByDescending(kv => kv.Value)
            .Take(20)
            .Select(kv => new FacetValue
            {
                Value = kv.Key,
                DisplayName = kv.Key,
                Count = kv.Value
            })
            .ToList();

        return results;
    }

    public List<Document> ApplyFilters(IEnumerable<Document> documents, FacetFilters filters)
    {
        var query = documents.AsQueryable();

        if (filters.FileTypes != null && filters.FileTypes.Count > 0)
        {
            query = query.Where(d =>
                !string.IsNullOrEmpty(d.FileName) &&
                filters.FileTypes.Contains(GetFileExtension(d.FileName)));
        }

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(d => d.DateAdded >= filters.DateFrom.Value);
        }
        if (filters.DateTo.HasValue)
        {
            query = query.Where(d => d.DateAdded <= filters.DateTo.Value);
        }

        if (filters.MinSize.HasValue)
        {
            query = query.Where(d => d.FileSize >= filters.MinSize.Value);
        }
        if (filters.MaxSize.HasValue)
        {
            query = query.Where(d => d.FileSize <= filters.MaxSize.Value);
        }

        if (filters.SizeCategories != null && filters.SizeCategories.Count > 0)
        {
            query = query.Where(d => filters.SizeCategories.Contains(GetSizeCategory(d.FileSize)));
        }

        if (filters.RequiredTerms != null && filters.RequiredTerms.Count > 0)
        {
            foreach (var term in filters.RequiredTerms)
            {
                var termLower = term.ToLowerInvariant();
                query = query.Where(d =>
                    d.Title.ToLowerInvariant().Contains(termLower) ||
                    d.Content.ToLowerInvariant().Contains(termLower));
            }
        }

        if (filters.ExcludedTerms != null && filters.ExcludedTerms.Count > 0)
        {
            foreach (var term in filters.ExcludedTerms)
            {
                var termLower = term.ToLowerInvariant();
                query = query.Where(d =>
                    !d.Title.ToLowerInvariant().Contains(termLower) &&
                    !d.Content.ToLowerInvariant().Contains(termLower));
            }
        }

        return query.ToList();
    }

    public FilterOptions GetFilterOptions(IEnumerable<Document> documents)
    {
        var docs = documents.ToList();

        return new FilterOptions
        {
            AvailableFileTypes = docs
                .Where(d => !string.IsNullOrEmpty(d.FileName))
                .Select(d => GetFileExtension(d.FileName!))
                .Distinct()
                .OrderBy(t => t)
                .ToList(),

            DateRange = docs.Count > 0
                ? (docs.Min(d => d.DateAdded), docs.Max(d => d.DateAdded))
                : (DateTime.MinValue, DateTime.MaxValue),

            SizeRange = docs.Count > 0
                ? (docs.Min(d => d.FileSize), docs.Max(d => d.FileSize))
                : (0, 0),

            TotalDocuments = docs.Count
        };
    }

    private string GetFileExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return string.IsNullOrEmpty(ext) ? "unknown" : ext.TrimStart('.');
    }

    private string GetFileTypeDisplayName(string extension)
    {
        return extension.ToUpperInvariant() switch
        {
            "PDF" => "PDF Documents",
            "TXT" => "Text Files",
            "MD" => "Markdown Files",
            "DOC" or "DOCX" => "Word Documents",
            "XLS" or "XLSX" => "Excel Spreadsheets",
            "HTML" or "HTM" => "Web Pages",
            "JSON" => "JSON Files",
            "XML" => "XML Files",
            _ => $"{extension.ToUpperInvariant()} Files"
        };
    }

    private SizeCategory GetSizeCategory(long sizeBytes)
    {
        return sizeBytes switch
        {
            < 1024 => SizeCategory.Tiny,
            < 10 * 1024 => SizeCategory.Small,
            < 100 * 1024 => SizeCategory.Medium,
            < 1024 * 1024 => SizeCategory.Large,
            _ => SizeCategory.VeryLarge
        };
    }

    private string GetSizeCategoryDisplayName(SizeCategory category)
    {
        return category switch
        {
            SizeCategory.Tiny => "< 1 KB",
            SizeCategory.Small => "1-10 KB",
            SizeCategory.Medium => "10-100 KB",
            SizeCategory.Large => "100 KB - 1 MB",
            SizeCategory.VeryLarge => "> 1 MB",
            _ => "Unknown"
        };
    }
}

public enum SizeCategory
{
    Tiny = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    VeryLarge = 4
}

public class FacetResults
{
    public List<FacetValue> FileTypeFacets { get; set; } = new();
    public List<FacetValue> DateFacets { get; set; } = new();
    public List<FacetValue> SizeFacets { get; set; } = new();
    public List<FacetValue> TermFacets { get; set; } = new();
}

public class FacetValue
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class FacetFilters
{
    public List<string>? FileTypes { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public List<SizeCategory>? SizeCategories { get; set; }
    public List<string>? RequiredTerms { get; set; }
    public List<string>? ExcludedTerms { get; set; }
}

public class FilterOptions
{
    public List<string> AvailableFileTypes { get; set; } = new();
    public (DateTime Min, DateTime Max) DateRange { get; set; }
    public (long Min, long Max) SizeRange { get; set; }
    public int TotalDocuments { get; set; }
}

public interface IFacetedSearch
{
    FacetResults ExtractFacets(IEnumerable<Document> documents);
    List<Document> ApplyFilters(IEnumerable<Document> documents, FacetFilters filters);
    FilterOptions GetFilterOptions(IEnumerable<Document> documents);
}
