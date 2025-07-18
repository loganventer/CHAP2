using Microsoft.AspNetCore.Mvc;
using CHAP2.WebPortal.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CHAP2.WebPortal.Controllers;

public class HomeController : Controller
{
    private readonly IChorusApiService _chorusApiService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IChorusApiService chorusApiService, ILogger<HomeController> logger)
    {
        _chorusApiService = chorusApiService;
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
        _logger.LogInformation("=== CREATE POST ACTION START ===");
        _logger.LogInformation("Create chorus POST received");
        _logger.LogInformation("Model is null: {IsNull}", model == null);
        
        if (model != null)
        {
            _logger.LogInformation("Model details:");
            _logger.LogInformation("- Name: '{Name}'", model.Name ?? "NULL");
            _logger.LogInformation("- ChorusText: '{ChorusText}'", model.ChorusText ?? "NULL");
            _logger.LogInformation("- Key: {Key}", model.Key);
            _logger.LogInformation("- Type: {Type}", model.Type);
            _logger.LogInformation("- TimeSignature: {TimeSignature}", model.TimeSignature);
        }
        
        // Log form data from Request.Form
        _logger.LogInformation("Form data from Request.Form:");
        if (Request.Form.Keys.Count == 0)
        {
            _logger.LogWarning("NO FORM DATA RECEIVED - Request.Form is empty!");
        }
        else
        {
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("- {Key}: '{Value}'", key, Request.Form[key]);
            }
        }
        
        // Also log the raw request content
        _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
        _logger.LogInformation("Request Method: {Method}", Request.Method);
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for chorus creation");
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
            return View(model);
        }

        try
        {
            _logger.LogInformation("Creating chorus with name: {Name}", model.Name);
            
            var chorus = Chorus.Create(
                model.Name,
                model.ChorusText,
                model.Key,
                model.Type,
                model.TimeSignature
            );

            _logger.LogInformation("Chorus created successfully with ID: {Id}", chorus.Id);
            
            var result = await _chorusApiService.CreateChorusAsync(chorus);
            if (result)
            {
                _logger.LogInformation("Chorus saved successfully, redirecting to close window page");
                return RedirectToAction(nameof(CloseWindow), new { message = "Chorus created successfully!" });
            }
            else
            {
                _logger.LogWarning("Failed to save chorus to API");
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
        _logger.LogInformation("=== EDIT POST ACTION START ===");
        _logger.LogInformation("Edit POST received with model ID: {ModelId}", model.Id);
        _logger.LogInformation("Model details - Name: {Name}, Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
            model.Name, model.Key, model.Type, model.TimeSignature);
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for chorus edit");
            var modelStateErrors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            _logger.LogWarning("ModelState errors: {ModelStateErrors}", modelStateErrors);
            
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>();
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>();
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>();
            return View(model);
        }

        try
        {
            _logger.LogInformation("Model state is valid, calling _chorusApiService.UpdateChorusAsync");
            _logger.LogInformation("Update parameters - ID: {Id}, Name: {Name}, Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                model.Id, model.Name, model.Key, model.Type, model.TimeSignature);
            
            var result = await _chorusApiService.UpdateChorusAsync(model.Id, model.Name, model.ChorusText, model.Key, model.Type, model.TimeSignature);
            
            _logger.LogInformation("UpdateChorusAsync returned: {Result}", result);
            
            if (result)
            {
                _logger.LogInformation("Chorus updated successfully, redirecting to close window");
                return RedirectToAction(nameof(CloseWindow), new { message = "Chorus updated successfully!" });
            }
            else
            {
                _logger.LogWarning("Failed to update chorus in API");
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
}

public class ChorusCreateViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chorus text is required")]
    public string ChorusText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Musical key is required")]
    public MusicalKey Key { get; set; }

    [Required(ErrorMessage = "Chorus type is required")]
    public ChorusType Type { get; set; }

    [Required(ErrorMessage = "Time signature is required")]
    public TimeSignature TimeSignature { get; set; }
}

public class ChorusEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chorus text is required")]
    public string ChorusText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Musical key is required")]
    public MusicalKey Key { get; set; }

    [Required(ErrorMessage = "Chorus type is required")]
    public ChorusType Type { get; set; }

    [Required(ErrorMessage = "Time signature is required")]
    public TimeSignature TimeSignature { get; set; }
} 