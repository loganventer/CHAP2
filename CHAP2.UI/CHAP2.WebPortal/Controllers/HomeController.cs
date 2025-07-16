using Microsoft.AspNetCore.Mvc;
using CHAP2.WebPortal.Interfaces;
using CHAP2.Domain.Entities;

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
} 