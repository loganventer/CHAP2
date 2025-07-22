using Microsoft.AspNetCore.Mvc;
using CHAP2.WebPortal.Interfaces;
using CHAP2.Shared.ViewModels;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Application.Interfaces;
using CHAP2.WebPortal.Services;
using CHAP2.WebPortal.DTOs;

namespace CHAP2.WebPortal.Controllers;

public class HomeController : Controller
{
    private readonly IChorusApiService _chorusApiService;
    private readonly IChorusApplicationService _chorusApplicationService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IOllamaService _ollamaService;
    private readonly IOllamaRagService _ollamaRagService;
    private readonly ITraditionalSearchWithAiService _traditionalSearchWithAiService;
    private readonly IIntelligentSearchService _intelligentSearchService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IChorusApiService chorusApiService, 
        IChorusApplicationService chorusApplicationService,
        IVectorSearchService vectorSearchService,
        IOllamaService ollamaService,
        IOllamaRagService ollamaRagService,
        ITraditionalSearchWithAiService traditionalSearchWithAiService,
        IIntelligentSearchService intelligentSearchService,
        ILogger<HomeController> logger)
    {
        _chorusApiService = chorusApiService;
        _chorusApplicationService = chorusApplicationService;
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _ollamaRagService = ollamaRagService;
        _traditionalSearchWithAiService = traditionalSearchWithAiService;
        _intelligentSearchService = intelligentSearchService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult CleanSearch()
    {
        return View();
    }

    public IActionResult TraditionalSearchWithAi()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q, string searchMode = "Contains", string searchIn = "all")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { results = new List<object>() });
            }

            var results = await _chorusApiService.SearchChorusesAsync(q, searchMode, searchIn);
            
            // Apply sorting logic similar to console app
            var sortedResults = results.OrderByDescending(r => 
                r.Key.ToString().Equals(q, StringComparison.OrdinalIgnoreCase))
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var response = sortedResults.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                key = (int)r.Key,
                type = (int)r.Type,
                timeSignature = (int)r.TimeSignature,
                chorusText = r.ChorusText,
                createdAt = r.CreatedAt
            });

            return Json(new { results = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during search for term: {SearchTerm}", q);
            return Json(new { results = new List<object>(), error = "Search failed" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id)
    {
        try
        {
            var chorus = await _chorusApiService.GetChorusByIdAsync(id);
            if (chorus == null)
            {
                return NotFound();
            }

            return View(chorus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chorus detail for ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ChorusDisplay(string id)
    {
        try
        {
            var chorus = await _chorusApiService.GetChorusByIdAsync(id);
            if (chorus == null)
            {
                return NotFound();
            }

            return View("ChorusDisplay", chorus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chorus display for ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> DetailPartial(string id)
    {
        try
        {
            var chorus = await _chorusApiService.GetChorusByIdAsync(id);
            if (chorus == null)
            {
                return NotFound();
            }

            return PartialView("_ChorusDetail", chorus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chorus detail partial for ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
        ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
        ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(ChorusCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
            return View(model);
        }

        try
        {
            // Create a Chorus entity from the ViewModel
            var chorus = Chorus.Create(model.Name, model.ChorusText, model.Key, model.Type, model.TimeSignature);
            
            // Call the API service directly
            var result = await _chorusApiService.CreateChorusAsync(chorus);
            if (result)
            {
                return RedirectToAction(nameof(CloseWindow), new { message = "Chorus created successfully!" });
            }
            else
            {
                ModelState.AddModelError("", "Failed to create chorus. Please try again.");
                ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
                ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
                ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chorus");
            ModelState.AddModelError("", "An error occurred while creating the chorus. Please try again.");
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        try
        {
            _logger.LogInformation("=== EDIT GET ACTION START ===");
            _logger.LogInformation("Attempting to load chorus for edit with ID: {Id}", id);
            
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("Edit action called with null or empty ID");
                return BadRequest("Invalid chorus ID");
            }
            
            _logger.LogInformation("Calling _chorusApiService.GetChorusByIdAsync with ID: {Id}", id);
            var chorus = await _chorusApiService.GetChorusByIdAsync(id);
            
            if (chorus == null)
            {
                _logger.LogWarning("Chorus not found for edit with ID: {Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Chorus loaded successfully for edit: {Name} (ID: {Id})", chorus.Name, chorus.Id);
            _logger.LogInformation("Chorus details - Name: {Name}, Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                chorus.Name, chorus.Key, chorus.Type, chorus.TimeSignature);
            
            var model = new ChorusEditViewModel
            {
                Id = chorus.Id,
                Name = chorus.Name ?? string.Empty,
                ChorusText = chorus.ChorusText ?? string.Empty,
                Key = chorus.Key,
                Type = chorus.Type,
                TimeSignature = chorus.TimeSignature
            };
            
            // Debug: Log the actual values being set
            _logger.LogInformation("Chorus values from API:");
            _logger.LogInformation("- Name: '{Name}'", chorus.Name);
            _logger.LogInformation("- ChorusText: '{ChorusText}'", chorus.ChorusText);
            _logger.LogInformation("- Key: {Key} (int: {KeyInt})", chorus.Key, (int)chorus.Key);
            _logger.LogInformation("- Type: {Type} (int: {TypeInt})", chorus.Type, (int)chorus.Type);
            _logger.LogInformation("- TimeSignature: {TimeSignature} (int: {TimeSignatureInt})", chorus.TimeSignature, (int)chorus.TimeSignature);

            _logger.LogInformation("Edit view model created successfully:");
            _logger.LogInformation("- Model ID: {ModelId}", model.Id);
            _logger.LogInformation("- Model Name: {ModelName}", model.Name);
            _logger.LogInformation("- Model Key: {ModelKey} (int value: {(int)model.Key})", model.Key, (int)model.Key);
            _logger.LogInformation("- Model Type: {ModelType} (int value: {(int)model.Type})", model.Type, (int)model.Type);
            _logger.LogInformation("- Model TimeSignature: {ModelTimeSignature} (int value: {(int)model.TimeSignature})", model.TimeSignature, (int)model.TimeSignature);

            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>();
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>();
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>();
            
            _logger.LogInformation("ViewBag populated with enums (including NotSet for imported choruses)");
            
            _logger.LogInformation("=== EDIT GET ACTION END - RETURNING VIEW ===");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chorus for edit with ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ChorusEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>();
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>();
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>();
            return View(model);
        }

        try
        {
            // Call the API service directly
            var result = await _chorusApiService.UpdateChorusAsync(model.Id, model.Name, model.ChorusText, model.Key, model.Type, model.TimeSignature);
            if (result)
            {
                return RedirectToAction(nameof(CloseWindow), new { message = "Chorus updated successfully!" });
            }
            else
            {
                ModelState.AddModelError("", "Failed to update chorus. Please try again.");
                ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>();
                ViewBag.ChorusTypes = Enum.GetValues<ChorusType>();
                ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>();
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chorus with ID: {Id}", model.Id);
            ModelState.AddModelError("", "An error occurred while updating the chorus. Please try again.");
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>();
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>();
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> TestConnectivity()
    {
        try
        {
            var isConnected = await _chorusApiService.TestConnectivityAsync();
            return Json(new { connected = isConnected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API connectivity");
            return Json(new { connected = false });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var result = await _chorusApiService.DeleteChorusAsync(id);
            if (result)
            {
                return Json(new { success = true, message = "Chorus deleted successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to delete chorus" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chorus with ID: {Id}", id);
            return Json(new { success = false, message = "An error occurred while deleting the chorus" });
        }
    }

    [HttpGet]
    public IActionResult CloseWindow(string message = "Operation completed successfully!")
    {
        ViewBag.Message = message;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AskQuestion([FromBody] AskQuestionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { error = "Question is required" });
            }

            // Search for relevant choruses
            var searchResults = await _vectorSearchService.SearchSimilarAsync(request.Question, 5);
            
            if (!searchResults.Any())
            {
                return Json(new { 
                    answer = "I couldn't find any relevant choruses for your question. Please try rephrasing your query.",
                    choruses = new List<object>()
                });
            }

            // Build context from search results
            var context = BuildContextFromResults(searchResults);
            
            // Create prompt with context
            var prompt = CreatePromptWithContext(request.Question, context);
            
            // Generate response using Ollama
            var response = await _ollamaService.GenerateResponseAsync(prompt);
            
            var chorusData = searchResults.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                key = r.Key,
                type = r.Type,
                timeSignature = r.TimeSignature,
                chorusText = r.ChorusText,
                createdAt = r.CreatedAt,
                score = r.Score
            });

            return Json(new { 
                answer = response,
                choruses = chorusData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question: {Question}", request.Question);
            return Json(new { 
                answer = $"Sorry, I encountered an error while processing your question: {ex.Message}",
                choruses = new List<object>()
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AskQuestionStream([FromBody] AskQuestionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { error = "Question is required" });
            }

            // Search for relevant choruses
            var searchResults = await _vectorSearchService.SearchSimilarAsync(request.Question, 5);
            
            if (!searchResults.Any())
            {
                return Json(new { 
                    answer = "I couldn't find any relevant choruses for your question. Please try rephrasing your query.",
                    choruses = new List<object>()
                });
            }

            // Build context from search results
            var context = BuildContextFromResults(searchResults);
            
            // Create prompt with context
            var prompt = CreatePromptWithContext(request.Question, context);
            
            // Return streaming response
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            
            var chorusData = searchResults.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                key = r.Key,
                type = r.Type,
                timeSignature = r.TimeSignature,
                chorusText = r.ChorusText,
                createdAt = r.CreatedAt,
                score = r.Score
            });

            // Send initial data with choruses
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "choruses", data = chorusData })}\n\n");
            await Response.Body.FlushAsync();

            // Stream the AI response
            await foreach (var chunk in _ollamaService.GenerateStreamingResponseAsync(prompt))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = chunk })}\n\n");
                await Response.Body.FlushAsync();
            }

            // Send completion signal
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "done" })}\n\n");
            await Response.Body.FlushAsync();
            
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming question: {Question}", request.Question);
            return Json(new { 
                answer = $"Sorry, I encountered an error while processing your question: {ex.Message}",
                choruses = new List<object>()
            });
        }
    }

    private static string BuildContextFromResults(List<ChorusSearchResult> results)
    {
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Here are some relevant choruses from the database:");
        contextBuilder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            contextBuilder.AppendLine($"{i + 1}. **{result.Name}** (Score: {result.Score:F3})");
            contextBuilder.AppendLine($"   Key: {result.Key}, Type: {result.Type}, Time Signature: {result.TimeSignature}");
            contextBuilder.AppendLine($"   Text: {result.ChorusText}");
            contextBuilder.AppendLine($"   Created: {result.CreatedAt:yyyy-MM-dd}");
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    private static string CreatePromptWithContext(string question, string context)
    {
        return $@"You are a helpful assistant that answers questions about a collection of choruses. 
Use the following context to answer the user's question. If the context doesn't contain enough information, 
say so and suggest what additional information might be needed.

Context:
{context}

User Question: {question}

Please provide a helpful and accurate response based on the chorus information provided above.";
    }

    [HttpPost]
    public async Task<IActionResult> RagSearch([FromBody] RagSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        try
        {
            _logger.LogInformation("Processing RAG search: {Query}", request.Query);

            var response = await _ollamaRagService.SearchWithRagAsync(request.Query, request.MaxResults);
            _logger.LogInformation("RAG search response: {Response}", response);
            
            return Json(new { response = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RAG search: {Query}", request.Query);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RagSearchStream([FromBody] RagSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        try
        {
            _logger.LogInformation("Processing streaming RAG search: {Query}", request.Query);

            // Set up streaming response headers
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Stream the RAG response
            await foreach (var chunk in _ollamaRagService.SearchWithRagStreamingAsync(request.Query, request.MaxResults))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = chunk })}\n\n");
                await Response.Body.FlushAsync();
            }

            // Send completion signal
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "done" })}\n\n");
            await Response.Body.FlushAsync();
            
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming RAG search: {Query}", request.Query);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> TraditionalSearchWithAi([FromBody] TraditionalSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        try
        {
            _logger.LogInformation("Processing traditional search with AI: {Query}", request.Query);
            
            // Log filter information if present
            if (request.Filters != null)
            {
                _logger.LogInformation("Applied filters - Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                    request.Filters.Key, request.Filters.Type, request.Filters.TimeSignature);
            }

            var result = await _traditionalSearchWithAiService.SearchWithAiAnalysisAsync(request.Query, request.MaxResults, request.Filters);
            _logger.LogInformation("Traditional search with AI completed. Found {Count} results, AI analysis: {HasAi}", 
                result.SearchResults.Count, result.HasAiAnalysis);
            
            // Convert to DTOs with readable names
            var searchResults = result.SearchResults.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                chorusText = c.ChorusText,
                key = GetKeyDisplayName(c.Key),
                type = GetTypeDisplayName(c.Type),
                timeSignature = GetTimeSignatureDisplayName(c.TimeSignature),
                createdAt = c.CreatedAt
            }).ToList();
            
            return Json(new { 
                searchResults = searchResults,
                aiAnalysis = result.AiAnalysis,
                hasAiAnalysis = result.HasAiAnalysis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during traditional search with AI: {Query}", request.Query);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> TraditionalSearchWithAiStream([FromBody] TraditionalSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        try
        {
            _logger.LogInformation("Processing streaming traditional search with AI: {Query}", request.Query);

            // Set up streaming response headers
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Stream the AI analysis
            await foreach (var chunk in _traditionalSearchWithAiService.SearchWithAiAnalysisStreamingAsync(request.Query, request.MaxResults, request.Filters))
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = chunk })}\n\n");
                await Response.Body.FlushAsync();
            }

            // Send completion signal
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "done" })}\n\n");
            await Response.Body.FlushAsync();
            
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming traditional search with AI: {Query}", request.Query);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private string GetKeyDisplayName(MusicalKey key)
    {
        return key switch
        {
            MusicalKey.NotSet => "Not Set",
            MusicalKey.C => "C",
            MusicalKey.CSharp => "C#",
            MusicalKey.D => "D",
            MusicalKey.DSharp => "D#",
            MusicalKey.E => "E",
            MusicalKey.F => "F",
            MusicalKey.FSharp => "F#",
            MusicalKey.G => "G",
            MusicalKey.GSharp => "G#",
            MusicalKey.A => "A",
            MusicalKey.ASharp => "A#",
            MusicalKey.B => "B",
            MusicalKey.CFlat => "C♭",
            MusicalKey.DFlat => "D♭",
            MusicalKey.EFlat => "E♭",
            MusicalKey.FFlat => "F♭",
            MusicalKey.GFlat => "G♭",
            MusicalKey.AFlat => "A♭",
            MusicalKey.BFlat => "B♭",
            _ => "Unknown"
        };
    }

    private string GetTypeDisplayName(ChorusType type)
    {
        return type switch
        {
            ChorusType.NotSet => "Not Set",
            ChorusType.Praise => "Praise",
            ChorusType.Worship => "Worship",
            _ => "Unknown"
        };
    }

    private string GetTimeSignatureDisplayName(TimeSignature timeSignature)
    {
        return timeSignature switch
        {
            TimeSignature.NotSet => "Not Set",
            TimeSignature.FourFour => "4/4",
            TimeSignature.ThreeFour => "3/4",
            TimeSignature.SixEight => "6/8",
            TimeSignature.TwoFour => "2/4",
            TimeSignature.FourEight => "4/8",
            TimeSignature.ThreeEight => "3/8",
            TimeSignature.TwoTwo => "2/2",
            TimeSignature.FiveFour => "5/4",
            TimeSignature.SixFour => "6/4",
            TimeSignature.NineEight => "9/8",
            TimeSignature.TwelveEight => "12/8",
            TimeSignature.SevenFour => "7/4",
            TimeSignature.EightFour => "8/4",
            TimeSignature.FiveEight => "5/8",
            TimeSignature.SevenEight => "7/8",
            TimeSignature.EightEight => "8/8",
            TimeSignature.TwoSixteen => "2/16",
            TimeSignature.ThreeSixteen => "3/16",
            TimeSignature.FourSixteen => "4/16",
            TimeSignature.FiveSixteen => "5/16",
            TimeSignature.SixSixteen => "6/16",
            TimeSignature.SevenSixteen => "7/16",
            TimeSignature.EightSixteen => "8/16",
            TimeSignature.NineSixteen => "9/16",
            TimeSignature.TwelveSixteen => "12/16",
            _ => "Unknown"
        };
    }

    [HttpPost]
    public async Task<IActionResult> IntelligentSearch([FromBody] IntelligentSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        try
        {
            _logger.LogInformation("Processing intelligent search: {Query}", request.Query);

            var result = await _intelligentSearchService.SearchWithIntelligenceAsync(request.Query, request.MaxResults);
            _logger.LogInformation("Intelligent search completed. Found {Count} results, AI analysis: {HasAi}", 
                result.SearchResults.Count, result.HasAiAnalysis);
            
            return Json(new { 
                searchResults = result.SearchResults,
                aiAnalysis = result.AiAnalysis,
                hasAiAnalysis = result.HasAiAnalysis,
                queryUnderstanding = result.QueryUnderstanding
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during intelligent search: {Query}", request.Query);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> IntelligentSearchStream([FromBody] IntelligentSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query is required" });
        }

        try
        {
            _logger.LogInformation("Processing streaming intelligent search: {Query}", request.Query);

            // Set up streaming response headers
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Stream the intelligent search response
            await foreach (var chunk in _intelligentSearchService.SearchWithIntelligenceStreamingAsync(request.Query, request.MaxResults))
            {
                await Response.WriteAsync($"data: {chunk}\n\n");
                await Response.Body.FlushAsync();
            }

            // Send completion signal
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "complete" })}\n\n");
            await Response.Body.FlushAsync();
            
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming intelligent search: {Query}", request.Query);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class TraditionalSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 10;
    public SearchFilters? Filters { get; set; }
}

public class AskQuestionRequest
{
    public string Question { get; set; } = string.Empty;
}

public class AiSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public bool UseAi { get; set; } = true;
}

public class RagSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 5;
}

public class IntelligentSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 10;
}

public class LlmSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ChorusText { get; set; } = string.Empty;
    public int Key { get; set; }
    public int Type { get; set; }
    public int TimeSignature { get; set; }
    public string Explanation { get; set; } = string.Empty;
} 