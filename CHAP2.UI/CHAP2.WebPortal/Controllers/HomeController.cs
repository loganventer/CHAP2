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
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IChorusApiService chorusApiService, 
        IChorusApplicationService chorusApplicationService,
        IVectorSearchService vectorSearchService,
        IOllamaService ollamaService,
        ILogger<HomeController> logger)
    {
        _chorusApiService = chorusApiService;
        _chorusApplicationService = chorusApplicationService;
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public IActionResult Index()
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
    public async Task<IActionResult> AiSearch([FromBody] AiSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return Json(new { results = new List<object>() });
            }

            // Use LLM to search for choruses
            var results = await _vectorSearchService.SearchSimilarAsync(request.Query, 10);
            var response = results.Select(r => new
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

            return Json(new { results = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI search for term: {SearchTerm}", request.Query);
            return Json(new { results = new List<object>(), error = "AI search failed" });
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> LlmSearch([FromBody] LlmSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            // Return streaming response
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            
            // Send initial progress message
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "Starting AI search..." })}\n\n");
            await Response.Body.FlushAsync();
            
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "Searching vector database for relevant choruses..." })}\n\n");
            await Response.Body.FlushAsync();
            
            // Step 1: Search the vector store with the user's query
            var searchResults = await _vectorSearchService.SearchSimilarAsync(request.Query, 15);
            
            if (!searchResults.Any())
            {
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "No relevant choruses found in vector database." })}\n\n");
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "done" })}\n\n");
                await Response.Body.FlushAsync();
                return new EmptyResult();
            }
            
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = $"Found {searchResults.Count} relevant choruses. Using AI to analyze and provide explanations..." })}\n\n");
            await Response.Body.FlushAsync();
            
            // Step 2: Create context from search results for RAG
            var chorusContext = string.Join("\n\n", searchResults.Select((c, i) => 
                $"Chorus {i + 1}:\n" +
                $"ID: {c.Id}\n" +
                $"Title: {c.Name}\n" +
                $"Key: {c.Key}\n" +
                $"Type: {c.Type}\n" +
                $"Time Signature: {c.TimeSignature}\n" +
                $"Relevance Score: {c.Score:F3}\n" +
                $"Text: {c.ChorusText}"
            ));
            
            var enumInfo = "\n\nENUM VALUES:\n" +
                "Key values: 0=NotSet, 1=C, 2=C#, 3=D, 4=D#, 5=E, 6=F, 7=F#, 8=G, 9=G#, 10=A, 11=A#, 12=B, 13=C♭, 14=D♭, 15=E♭, 16=F♭, 17=G♭, 18=A♭, 19=B♭\n" +
                "Type values: 0=NotSet, 1=Praise, 2=Worship\n" +
                "TimeSignature values: 0=NotSet, 1=4/4, 2=3/4, 3=6/8, 4=2/4, 5=4/8, 6=3/8, 7=2/2, 8=5/4, 9=6/4, 10=9/8, 11=12/8, 12=7/4, 13=8/4, 14=5/8, 15=7/8, 16=8/8, 17=2/16, 18=3/16, 19=4/16, 20=5/16, 21=6/16, 22=7/16, 23=8/16, 24=9/16, 25=12/16\n";
            
            var validIds = searchResults.Select(c => c.Id).ToList();
            
            _logger.LogInformation("Search results - Total choruses: {Count}, Valid IDs: {ValidIdsCount}", 
                searchResults.Count, validIds.Count);
            
            // Step 3: Use RAG with Ollama to analyze the results
            var ragPrompt = @"You are a JSON generator for a musical chorus database search.

User query: """ + request.Query + @"""

Based on the chorus database context provided below, find the most relevant choruses and explain why they match the user's query.

CHORUS DATABASE CONTEXT:
" + chorusContext + @"

" + enumInfo + @"

CRITICAL JSON FORMATTING RULES:
1. ALL property names MUST be in double quotes: ""id"", ""name"", ""chorusText"", ""key"", ""type"", ""timeSignature"", ""explanation""
2. ALL string values MUST be in double quotes
3. Use ONLY colon (:) not any other characters
4. Use ONLY comma (,) to separate properties
5. Use ONLY square brackets [] for arrays
6. Use ONLY curly braces {} for objects
7. NO trailing commas
8. NO extra spaces or characters
9. ""id"" MUST be a string, NOT an array: ""id"":""uuid-here"" NOT ""id"":[""uuid-here""]
10. ""key"", ""type"", ""timeSignature"" MUST be integers, NOT strings: ""key"":19 NOT ""key"":""19""

INSTRUCTIONS:
1. Analyze the provided chorus database context
2. Find choruses that best match the user's query
3. Use ONLY exact chorus IDs from the context provided
4. Do NOT generate new IDs or make up data
5. Respond with ONLY a valid JSON array
6. No other text, no explanations, no markdown formatting
7. Include explanations for why each chorus matches the query

The JSON array must contain objects with exactly these fields:
- ""id"": the chorus ID (string) - MUST be from the context provided
- ""name"": the chorus name (string) - copy exactly from the context
- ""chorusText"": the full chorus text (string) - copy exactly from the context
- ""key"": the musical key (integer) - copy exactly from the context
- ""type"": the chorus type (integer) - copy exactly from the context
- ""timeSignature"": the time signature (integer) - copy exactly from the context
- ""explanation"": brief reason for match (string) - explain why this chorus matches the user query

CRITICAL: Copy the exact values from the context. Do NOT generate or modify any data.

Example response format:
[{""id"":""d0c5e020-4fa8-4227-ad39-e5982281ba89"",""name"":""O, Heer My God, As Ek In Eerbied Wonder"",""chorusText"":""en al U werke elke dag aanskou..."",""key"":19,""type"":0,""timeSignature"":0,""explanation"":""Contains religious themes and mentions God""}]

Return ONLY the JSON array, nothing else. Use ONLY IDs from the context provided.";

            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "Using AI to analyze search results and provide explanations..." })}\n\n");
            await Response.Body.FlushAsync();
            
            var llmResponse = await _ollamaService.GenerateResponseAsync(ragPrompt);
            
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "Processing AI analysis..." })}\n\n");
            await Response.Body.FlushAsync();
            
            _logger.LogInformation("Raw LLM response: {Response}", llmResponse);
            
            var cleanedResponse = CleanLlmResponse(llmResponse);
            _logger.LogInformation("Cleaned LLM response: {Response}", cleanedResponse);
            
            try
            {
                _logger.LogInformation("Attempting to parse JSON response: {Response}", cleanedResponse);
                
                var llmResults = System.Text.Json.JsonSerializer.Deserialize<List<LlmSearchResult>>(cleanedResponse);
                
                if (llmResults != null && llmResults.Any())
                {
                    _logger.LogInformation("Successfully parsed {Count} LLM results", llmResults.Count);
                    
                    var validResults = llmResults.Where(r => validIds.Contains(r.Id)).ToList();
                    var invalidResults = llmResults.Where(r => !validIds.Contains(r.Id)).ToList();
                    
                    if (invalidResults.Any())
                    {
                        _logger.LogWarning("Filtered out {Count} LLM results with invalid IDs: {InvalidIds}", 
                            invalidResults.Count, string.Join(", ", invalidResults.Select(r => r.Id)));
                    }
                    
                    if (validResults.Any())
                    {
                        await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = $"AI analyzed {validResults.Count} relevant choruses!" })}\n\n");
                        await Response.Body.FlushAsync();
                        
                        var detailedChoruses = new List<object>();
                        
                        foreach (var llmResult in validResults)
                        {
                            _logger.LogInformation("Processing LLM result - ID: {Id}, Name: {Name}, Explanation: {Explanation}", 
                                llmResult.Id, llmResult.Name, llmResult.Explanation);
                            
                            var actualChorus = searchResults.FirstOrDefault(c => c.Id == llmResult.Id);
                            
                            if (actualChorus != null)
                            {
                                detailedChoruses.Add(new
                                {
                                    id = actualChorus.Id,
                                    name = actualChorus.Name,
                                    key = actualChorus.Key,
                                    type = actualChorus.Type,
                                    timeSignature = actualChorus.TimeSignature,
                                    chorusText = actualChorus.ChorusText,
                                    createdAt = actualChorus.CreatedAt,
                                    aiExplanation = llmResult.Explanation,
                                    score = actualChorus.Score
                                });
                            }
                            else
                            {
                                _logger.LogWarning("LLM result with valid ID not found in search results: {Id}", llmResult.Id);
                            }
                        }
                        
                        _logger.LogInformation("Successfully processed {Count} choruses from LLM", detailedChoruses.Count);
                        
                        if (detailedChoruses.Any())
                        {
                            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "choruses", data = detailedChoruses })}\n\n");
                            await Response.Body.FlushAsync();
                        }
                        else
                        {
                            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "No valid choruses found after filtering." })}\n\n");
                        }
                    }
                    else
                    {
                        await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "No valid choruses found. The AI returned invalid IDs." })}\n\n");
                    }
                }
                else
                {
                    _logger.LogWarning("No LLM results found or empty results");
                    await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = "No relevant choruses found for your query." })}\n\n");
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error parsing LLM response: {Response}", llmResponse);
                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = $"Error parsing AI response. Raw response: {llmResponse}" })}\n\n");
            }

            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "done" })}\n\n");
            await Response.Body.FlushAsync();
            
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM search for query: {Query}", request.Query);
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "chunk", data = $"Error: {ex.Message}" })}\n\n");
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { type = "done" })}\n\n");
            await Response.Body.FlushAsync();
            return new EmptyResult();
        }
    }
    
    public class LlmSearchRequest
    {
        public string Query { get; set; } = string.Empty;
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

    private static string CleanLlmResponse(string llmResponse)
    {
        // Remove any leading/trailing whitespace
        llmResponse = llmResponse.Trim();

        // Try to find JSON array in the response
        var startIndex = llmResponse.IndexOf('[');
        var endIndex = llmResponse.LastIndexOf(']');
        
        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            // Extract only the JSON array
            var jsonArray = llmResponse.Substring(startIndex, endIndex - startIndex + 1);
            
            // Fix common JSON formatting issues
            jsonArray = FixJsonFormatting(jsonArray);
            
            return jsonArray;
        }
        
        // If no JSON array found, try to clean the response
        // Remove any markdown formatting
        llmResponse = llmResponse.Replace("```json", "").Replace("```", "");
        llmResponse = llmResponse.Replace("##", "").Replace("###", "").Replace("####", "");
        llmResponse = llmResponse.Replace("*", "").Replace("-", "");
        
        // Remove any extra newlines and carriage returns
        llmResponse = llmResponse.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Remove any leading/trailing whitespace from lines
        llmResponse = string.Join("\n", llmResponse.Split('\n').Select(line => line.Trim()));
        
        // Remove any leading/trailing newlines
        llmResponse = llmResponse.TrimStart('\n').TrimEnd('\n');
        
        return llmResponse;
    }
    
    private static string FixJsonFormatting(string json)
    {
        // Fix common JSON formatting issues
        var fixedJson = json;
        var originalJson = json;
        
        // Fix property names without quotes (more aggressive)
        var beforePropertyFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"(\s*)([a-zA-Z_][a-zA-Z0-9_]*)(\s*):", "$1\"$2\"$3:");
        if (beforePropertyFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed property names without quotes");
        }
        
        // Fix string values without quotes (but not numbers or booleans)
        var beforeStringFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @":\s*([^""\d\[\]{},]+?)(?=\s*[,}])", ": \"$1\"");
        if (beforeStringFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed string values without quotes");
        }
        
        // Fix invalid characters
        var beforeCharFix = fixedJson;
        fixedJson = fixedJson.Replace("：", ":");
        fixedJson = fixedJson.Replace("，", ",");
        fixedJson = fixedJson.Replace("'", "\"");
        if (beforeCharFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed invalid characters");
        }
        
        // Remove extra spaces around colons and commas
        var beforeSpaceFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"\s*:\s*", ": ");
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"\s*,\s*", ", ");
        if (beforeSpaceFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed extra spaces");
        }
        
        // Fix trailing commas
        var beforeTrailingFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @",(\s*[}\]])", "$1");
        if (beforeTrailingFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed trailing commas");
        }
        
        // Fix ID arrays - convert ["id"] to "id"
        var beforeIdFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"""id"":\s*\[""([^""]+)""\]", "\"id\":\"$1\"");
        if (beforeIdFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed ID arrays");
        }
        
        // Fix string enum values - convert "5" to 5 for type and timeSignature
        var beforeEnumFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"""type"":\s*""(\d+)""", "\"type\":$1");
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"""timeSignature"":\s*""(\d+)""", "\"timeSignature\":$1");
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"""key"":\s*""(\d+)""", "\"key\":$1");
        if (beforeEnumFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed string enum values");
        }
        
        // Fix specific issues from the logs - missing quotes around property names
        var beforeQuoteFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"""([^""]+)"":([^""][^,}]+)", "\"$1\":\"$2\"");
        if (beforeQuoteFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed missing quotes around property values");
        }
        
        // Fix timeSignature values that are strings like "7/4" - convert to integer
        var beforeTimeFix = fixedJson;
        fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @"""timeSignature"":\s*""(\d+/\d+)""", "\"timeSignature\":0");
        if (beforeTimeFix != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine("Fixed timeSignature string values");
        }
        
        // Fix incomplete JSON by finding the last complete object
        var lastBraceIndex = fixedJson.LastIndexOf('}');
        var lastBracketIndex = fixedJson.LastIndexOf(']');
        
        if (lastBraceIndex > lastBracketIndex)
        {
            // Find the start of the last complete object
            var braceCount = 0;
            var startIndex = lastBraceIndex;
            for (int i = lastBraceIndex; i >= 0; i--)
            {
                if (fixedJson[i] == '}')
                    braceCount++;
                else if (fixedJson[i] == '{')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        startIndex = i;
                        break;
                    }
                }
            }
            
            // Extract only the complete objects
            if (startIndex < lastBraceIndex)
            {
                var completeObjects = fixedJson.Substring(startIndex, lastBraceIndex - startIndex + 1);
                fixedJson = "[" + completeObjects + "]";
                System.Diagnostics.Debug.WriteLine("Fixed incomplete JSON");
            }
        }
        
        // Log the transformations if there were any changes
        if (originalJson != fixedJson)
        {
            System.Diagnostics.Debug.WriteLine($"JSON formatting applied - Original length: {originalJson.Length}, Fixed length: {fixedJson.Length}");
        }
        
        return fixedJson;
    }
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