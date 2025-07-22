using CHAP2.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class AiSearchService : IAiSearchService
{
    private readonly ILogger<AiSearchService> _logger;

    public AiSearchService(ILogger<AiSearchService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<string>> GenerateSearchTermsAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating AI search terms for query: {Query}", query);
        
        var searchTerms = new List<string> { query };
        
        // Enhanced semantic variations for musical and religious content
        var semanticVariations = new Dictionary<string, List<string>>
        {
            // Religious terms
            ["god"] = new List<string> { "lord", "jesus", "christ", "savior", "redeemer", "almighty", "creator" },
            ["lord"] = new List<string> { "god", "jesus", "christ", "savior", "redeemer", "almighty" },
            ["jesus"] = new List<string> { "christ", "savior", "redeemer", "lord", "son of god" },
            ["christ"] = new List<string> { "jesus", "savior", "redeemer", "messiah", "anointed one" },
            
            // Worship terms
            ["praise"] = new List<string> { "worship", "glorify", "exalt", "magnify", "honor", "adore" },
            ["worship"] = new List<string> { "praise", "glorify", "exalt", "magnify", "honor", "adore" },
            ["glory"] = new List<string> { "praise", "honor", "majesty", "splendor", "magnificence" },
            
            // Love and grace
            ["love"] = new List<string> { "grace", "mercy", "kindness", "compassion", "charity", "affection" },
            ["grace"] = new List<string> { "love", "mercy", "favor", "blessing", "kindness" },
            ["mercy"] = new List<string> { "grace", "love", "compassion", "forgiveness", "pity" },
            
            // Faith and trust
            ["faith"] = new List<string> { "trust", "belief", "hope", "confidence", "assurance" },
            ["trust"] = new List<string> { "faith", "belief", "hope", "confidence", "rely" },
            ["hope"] = new List<string> { "faith", "trust", "expectation", "confidence", "assurance" },
            
            // Prayer and spiritual life
            ["prayer"] = new List<string> { "pray", "supplication", "intercession", "petition", "worship" },
            ["pray"] = new List<string> { "prayer", "supplication", "intercession", "petition" },
            
            // Power and greatness
            ["great"] = new List<string> { "mighty", "powerful", "awesome", "wonderful", "amazing", "magnificent" },
            ["mighty"] = new List<string> { "powerful", "strong", "great", "awesome", "almighty" },
            ["powerful"] = new List<string> { "mighty", "strong", "great", "awesome", "almighty" },
            
            // Creation and nature
            ["creation"] = new List<string> { "world", "earth", "universe", "nature", "skepping" },
            ["world"] = new List<string> { "earth", "creation", "universe", "nature" },
            ["heaven"] = new List<string> { "paradise", "glory", "eternal", "divine", "celestial" },
            
            // Salvation and redemption
            ["salvation"] = new List<string> { "redemption", "deliverance", "rescue", "saving", "liberation" },
            ["redemption"] = new List<string> { "salvation", "deliverance", "rescue", "saving" },
            
            // Music and worship
            ["sing"] = new List<string> { "praise", "worship", "glorify", "exalt", "honor" },
            ["music"] = new List<string> { "song", "melody", "harmony", "praise", "worship" },
            ["song"] = new List<string> { "music", "melody", "praise", "worship", "chorus" }
        };

        var queryLower = query.ToLowerInvariant();
        var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Add variations for each word in the query
        foreach (var word in queryWords)
        {
            if (semanticVariations.ContainsKey(word))
            {
                searchTerms.AddRange(semanticVariations[word]);
            }
        }
        
        // Add variations for multi-word phrases
        foreach (var variation in semanticVariations)
        {
            if (queryLower.Contains(variation.Key))
            {
                searchTerms.AddRange(variation.Value);
            }
        }

        // Add language-specific variations (English/Afrikaans)
        var languageVariations = new Dictionary<string, List<string>>
        {
            ["god"] = new List<string> { "heer", "god", "vader" },
            ["lord"] = new List<string> { "heer", "god", "heer" },
            ["praise"] = new List<string> { "prys", "eer", "verheerlik" },
            ["worship"] = new List<string> { "aanbid", "prys", "eer" },
            ["love"] = new List<string> { "liefde", "genade", "barmhartigheid" },
            ["grace"] = new List<string> { "genade", "liefde", "gunst" },
            ["faith"] = new List<string> { "geloof", "vertroue", "hoop" },
            ["prayer"] = new List<string> { "gebed", "aanbidding", "smeking" }
        };

        foreach (var word in queryWords)
        {
            if (languageVariations.ContainsKey(word))
            {
                searchTerms.AddRange(languageVariations[word]);
            }
        }

        // Remove duplicates and limit to reasonable number
        var distinctTerms = searchTerms.Distinct().Take(8).ToList();
        
        _logger.LogInformation("Generated {Count} search terms: {Terms}", distinctTerms.Count, string.Join(", ", distinctTerms));
        
        return distinctTerms;
    }

    public async Task<string> AnalyzeSearchContextAsync(string query, List<string> searchTerms, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing search context for query: {Query} with {Count} terms", query, searchTerms.Count);
        
        // Analyze the search context to provide insights
        var analysis = new List<string>();
        
        // Detect search intent
        var queryLower = query.ToLowerInvariant();
        if (queryLower.Contains("praise") || queryLower.Contains("worship") || queryLower.Contains("prys"))
        {
            analysis.Add("Worship-focused search");
        }
        else if (queryLower.Contains("god") || queryLower.Contains("lord") || queryLower.Contains("jesus") || queryLower.Contains("heer"))
        {
            analysis.Add("Divine-focused search");
        }
        else if (queryLower.Contains("love") || queryLower.Contains("grace") || queryLower.Contains("liefde"))
        {
            analysis.Add("Love and grace-focused search");
        }
        else if (queryLower.Contains("faith") || queryLower.Contains("trust") || queryLower.Contains("geloof"))
        {
            analysis.Add("Faith-focused search");
        }
        else
        {
            analysis.Add("General search");
        }
        
        // Detect language
        var afrikaansWords = new[] { "heer", "prys", "liefde", "genade", "geloof", "gebed", "aanbid" };
        var hasAfrikaans = searchTerms.Any(term => afrikaansWords.Any(af => term.ToLowerInvariant().Contains(af)));
        
        if (hasAfrikaans)
        {
            analysis.Add("Includes Afrikaans terms");
        }
        
        // Provide search strategy
        analysis.Add($"Using {searchTerms.Count} search terms for comprehensive results");
        
        var context = string.Join("; ", analysis);
        _logger.LogInformation("Search context analysis: {Context}", context);
        
        return context;
    }
} 