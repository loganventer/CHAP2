using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
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
        _chorusCommandService = chorusCommandService ?? throw new ArgumentNullException(nameof(chorusCommandService));
        _chorusQueryService = chorusQueryService ?? throw new ArgumentNullException(nameof(chorusQueryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Chorus> CreateChorusAsync(CreateChorusCommand command)
    {
        _logger.LogInformation("Creating chorus with name: {Name}", command.Name);

        var chorus = await _chorusCommandService.CreateChorusAsync(
            command.Name,
            command.ChorusText,
            command.Key,
            command.Type,
            command.TimeSignature
        );

        _logger.LogInformation("Chorus created successfully with ID: {Id}", chorus.Id);
        return chorus;
    }

    public async Task<Chorus> UpdateChorusAsync(UpdateChorusCommand command)
    {
        _logger.LogInformation("Updating chorus with ID: {Id}", command.Id);

        var chorus = await _chorusCommandService.UpdateChorusAsync(
            command.Id,
            command.Name,
            command.ChorusText,
            command.Key,
            command.Type,
            command.TimeSignature
        );

        _logger.LogInformation("Chorus updated successfully with ID: {Id}", chorus.Id);
        return chorus;
    }

    public async Task DeleteChorusAsync(string id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            throw new ArgumentException($"Invalid GUID format for chorus ID: {id}", nameof(id));
        }

        _logger.LogInformation("Deleting chorus with ID: {Id}", id);
        await _chorusCommandService.DeleteChorusAsync(guidId);
    }

    public async Task<Chorus?> GetChorusByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            throw new ArgumentException($"Invalid GUID format for chorus ID: {id}", nameof(id));
        }

        return await _chorusQueryService.GetChorusByIdAsync(guidId);
    }

    public async Task<IEnumerable<Chorus>> SearchChorusesAsync(string query, string searchMode, string searchIn)
    {
        if (!Enum.TryParse<SearchMode>(searchMode, out var searchModeEnum))
        {
            _logger.LogWarning("Invalid search mode: {SearchMode}, defaulting to Contains", searchMode);
            searchModeEnum = SearchMode.Contains;
        }

        if (!Enum.TryParse<SearchScope>(searchIn, out var searchScopeEnum))
        {
            _logger.LogWarning("Invalid search scope: {SearchScope}, defaulting to All", searchIn);
            searchScopeEnum = SearchScope.All;
        }

        return await _chorusQueryService.SearchChorusesAsync(query, searchModeEnum, searchScopeEnum);
    }
}
