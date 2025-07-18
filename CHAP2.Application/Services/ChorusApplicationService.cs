using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Shared.ViewModels;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class ChorusApplicationService : IChorusApplicationService
{
    private readonly IChorusCommandService _chorusCommandService;
    private readonly IChorusQueryService _chorusQueryService;
    private readonly ILogger<ChorusApplicationService> _logger;

    public ChorusApplicationService(
        IChorusCommandService chorusCommandService,
        IChorusQueryService chorusQueryService,
        ILogger<ChorusApplicationService> logger)
    {
        _chorusCommandService = chorusCommandService;
        _chorusQueryService = chorusQueryService;
        _logger = logger;
    }

    public async Task<bool> CreateChorusAsync(ChorusCreateViewModel model)
    {
        try
        {
            _logger.LogInformation("Creating chorus with name: {Name}", model.Name);
            
            var chorus = await _chorusCommandService.CreateChorusAsync(
                model.Name,
                model.ChorusText,
                model.Key,
                model.Type,
                model.TimeSignature
            );

            _logger.LogInformation("Chorus created successfully with ID: {Id}", chorus.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chorus");
            return false;
        }
    }

    public async Task<bool> UpdateChorusAsync(ChorusEditViewModel model)
    {
        try
        {
            _logger.LogInformation("Updating chorus with ID: {Id}", model.Id);
            
            var chorus = await _chorusCommandService.UpdateChorusAsync(
                model.Id,
                model.Name,
                model.ChorusText,
                model.Key,
                model.Type,
                model.TimeSignature
            );

            _logger.LogInformation("Chorus updated successfully with ID: {Id}", chorus.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chorus with ID: {Id}", model.Id);
            return false;
        }
    }

    public async Task<bool> DeleteChorusAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting chorus with ID: {Id}", id);
            await _chorusCommandService.DeleteChorusAsync(Guid.Parse(id));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chorus with ID: {Id}", id);
            return false;
        }
    }

    public async Task<Chorus?> GetChorusByIdAsync(string id)
    {
        try
        {
            return await _chorusQueryService.GetChorusByIdAsync(Guid.Parse(id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chorus by ID: {Id}", id);
            return null;
        }
    }

    public async Task<IEnumerable<Chorus>> SearchChorusesAsync(string query, string searchMode, string searchIn)
    {
        try
        {
            var searchModeEnum = Enum.Parse<SearchMode>(searchMode);
            var searchScopeEnum = Enum.Parse<SearchScope>(searchIn);
            return await _chorusQueryService.SearchChorusesAsync(query, searchModeEnum, searchScopeEnum);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching choruses with query: {Query}", query);
            return Enumerable.Empty<Chorus>();
        }
    }
} 