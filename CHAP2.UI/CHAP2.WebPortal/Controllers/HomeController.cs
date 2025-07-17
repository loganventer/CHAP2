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
        if (!ModelState.IsValid)
        {
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
            return View(model);
        }

        try
        {
            var chorus = Chorus.Create(
                model.Name,
                model.ChorusText,
                model.Key,
                model.Type,
                model.TimeSignature
            );

            var result = await _chorusApiService.CreateChorusAsync(chorus);
            if (result)
            {
                return RedirectToAction(nameof(Detail), new { id = chorus.Id });
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
            var chorus = await _chorusApiService.GetChorusByIdAsync(id);
            if (chorus == null)
            {
                return NotFound();
            }

            var model = new ChorusEditViewModel
            {
                Id = chorus.Id,
                Name = chorus.Name,
                ChorusText = chorus.ChorusText,
                Key = chorus.Key,
                Type = chorus.Type,
                TimeSignature = chorus.TimeSignature
            };

            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chorus for edit with ID: {Id}", id);
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ChorusEditViewModel model)
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
            var result = await _chorusApiService.UpdateChorusAsync(model.Id, model.Name, model.ChorusText, model.Key, model.Type, model.TimeSignature);
            if (result)
            {
                return RedirectToAction(nameof(Detail), new { id = model.Id });
            }
            else
            {
                ModelState.AddModelError("", "Failed to update chorus. Please try again.");
                ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
                ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
                ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chorus with ID: {Id}", model.Id);
            ModelState.AddModelError("", "An error occurred while updating the chorus. Please try again.");
            ViewBag.MusicalKeys = Enum.GetValues<MusicalKey>().Where(k => k != MusicalKey.NotSet);
            ViewBag.ChorusTypes = Enum.GetValues<ChorusType>().Where(t => t != ChorusType.NotSet);
            ViewBag.TimeSignatures = Enum.GetValues<TimeSignature>().Where(t => t != TimeSignature.NotSet);
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