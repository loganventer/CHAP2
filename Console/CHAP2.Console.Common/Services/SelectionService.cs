using CHAP2.Console.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Console.Common.Services;

public class SelectionService : ISelectionService
{
    private readonly ILogger<SelectionService> _logger;

    public int SelectedIndex { get; set; } = 0;
    public int TotalItems { get; set; } = 0;
    public bool IsInDetailView { get; set; } = false;

    public SelectionService(ILogger<SelectionService> logger)
    {
        _logger = logger;
    }

    public void MoveUp()
    {
        if (IsInDetailView) 
        {
            _logger.LogDebug("MoveUp ignored - in detail view");
            return; // No navigation in detail view
        }
        
        if (SelectedIndex > 0)
        {
            SelectedIndex--;
            _logger.LogInformation("Moved selection up to index {Index}", SelectedIndex);
        }
        else
        {
            _logger.LogDebug("MoveUp ignored - already at top (index {Index})", SelectedIndex);
        }
    }

    public void MoveDown()
    {
        if (IsInDetailView) 
        {
            _logger.LogDebug("MoveDown ignored - in detail view");
            return; // No navigation in detail view
        }
        
        if (SelectedIndex < TotalItems - 1)
        {
            SelectedIndex++;
            _logger.LogInformation("Moved selection down to index {Index}", SelectedIndex);
        }
        else
        {
            _logger.LogDebug("MoveDown ignored - already at bottom (index {Index}, total {Total})", SelectedIndex, TotalItems);
        }
    }

    public void SelectCurrent()
    {
        if (IsInDetailView) return; // No selection in detail view
        
        if (SelectedIndex >= 0 && SelectedIndex < TotalItems)
        {
            _logger.LogInformation("Selected item at index {Index}", SelectedIndex);
        }
    }

    public void ResetSelection()
    {
        SelectedIndex = 0;
        IsInDetailView = false;
        _logger.LogDebug("Selection reset");
    }

    public void UpdateTotalItems(int totalItems)
    {
        TotalItems = totalItems;
        
        // Auto-select first item if there's only one result
        if (TotalItems == 1)
        {
            SelectedIndex = 0;
            _logger.LogDebug("Auto-selected single result at index 0");
        }
        else if (TotalItems == 0)
        {
            SelectedIndex = 0;
            _logger.LogDebug("No results, reset selection to 0");
        }
        else if (SelectedIndex >= TotalItems)
        {
            // If current selection is out of bounds, reset to first item
            SelectedIndex = 0;
            _logger.LogDebug("Selection out of bounds, reset to index 0");
        }
    }

    public bool TrySelectByNumber(int number)
    {
        if (IsInDetailView) return false; // No selection in detail view
        
        var songIndex = number - 1; // Convert 1-based to 0-based
        
        if (songIndex >= 0 && songIndex < TotalItems)
        {
            SelectedIndex = songIndex;
            _logger.LogInformation("Selected item by number {Number} at index {Index}", number, songIndex);
            return true;
        }
        
        _logger.LogDebug("Invalid number selection: {Number} (total items: {TotalItems})", number, TotalItems);
        return false;
    }
} 