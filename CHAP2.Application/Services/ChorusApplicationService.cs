using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class ChorusApplicationService
{
    private readonly IChorusRepository _chorusRepository;
    private readonly ISearchService _searchService;
    private readonly ILogger<ChorusApplicationService> _logger;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ChorusApplicationService(
        IChorusRepository chorusRepository,
        ISearchService searchService,
        ILogger<ChorusApplicationService> logger,
        IDomainEventDispatcher eventDispatcher)
    {
        _chorusRepository = chorusRepository;
        _searchService = searchService;
        _logger = logger;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Chorus> CreateChorusAsync(
        string name, 
        string chorusText, 
        MusicalKey key, 
        ChorusType type, 
        TimeSignature timeSignature,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating chorus with name: {Name}", name);

        // Check if chorus already exists
        if (await _chorusRepository.ExistsAsync(name, cancellationToken))
        {
            throw new ChorusAlreadyExistsException(name);
        }

        // Create domain entity
        var chorus = Chorus.Create(name, chorusText, key, type, timeSignature);

        // Persist to repository
        var createdChorus = await _chorusRepository.AddAsync(chorus, cancellationToken);

        // Dispatch domain events
        foreach (var domainEvent in chorus.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        chorus.DomainEvents.Clear();

        // Invalidate search cache
        _searchService.InvalidateCache();

        _logger.LogInformation("Successfully created chorus with ID: {Id}", createdChorus.Id);
        return createdChorus;
    }

    public async Task<Chorus> UpdateChorusAsync(
        Guid id,
        string name,
        string chorusText,
        MusicalKey key,
        ChorusType type,
        TimeSignature timeSignature,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating chorus with ID: {Id}", id);

        // Get existing chorus
        var existingChorus = await _chorusRepository.GetByIdAsync(id, cancellationToken);
        if (existingChorus == null)
        {
            throw new ChorusNotFoundException(id);
        }

        // Check for name conflicts (excluding current chorus)
        var allChoruses = await _chorusRepository.GetAllAsync(cancellationToken);
        var nameConflict = allChoruses.Any(c => 
            c.Id != id && 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            
        if (nameConflict)
        {
            throw new ChorusAlreadyExistsException(name);
        }

        // Update domain entity
        existingChorus.Update(name, chorusText, key, type, timeSignature);

        // Persist changes
        var updatedChorus = await _chorusRepository.UpdateAsync(existingChorus, cancellationToken);

        // Invalidate search cache
        _searchService.InvalidateCache();

        _logger.LogInformation("Successfully updated chorus with ID: {Id}", updatedChorus.Id);
        return updatedChorus;
    }

    public async Task<Chorus> GetChorusByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting chorus by ID: {Id}", id);

        var chorus = await _chorusRepository.GetByIdAsync(id, cancellationToken);
        if (chorus == null)
        {
            throw new ChorusNotFoundException(id);
        }

        return chorus;
    }

    public async Task<Chorus> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting chorus by name: {Name}", name);

        var chorus = await _chorusRepository.GetByNameAsync(name, cancellationToken);
        if (chorus == null)
        {
            throw new ChorusNotFoundException(name);
        }

        return chorus;
    }

    public async Task<IReadOnlyList<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all choruses");

        return await _chorusRepository.GetAllAsync(cancellationToken);
    }

    public async Task DeleteChorusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting chorus with ID: {Id}", id);

        var existingChorus = await _chorusRepository.GetByIdAsync(id, cancellationToken);
        if (existingChorus == null)
        {
            throw new ChorusNotFoundException(id);
        }

        await _chorusRepository.DeleteAsync(id, cancellationToken);

        // Invalidate search cache
        _searchService.InvalidateCache();

        _logger.LogInformation("Successfully deleted chorus with ID: {Id}", id);
    }

    public async Task<IReadOnlyList<Chorus>> SearchChorusesAsync(
        string searchTerm,
        SearchMode searchMode = SearchMode.Contains,
        SearchScope searchScope = SearchScope.All,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching choruses with term: {SearchTerm}, mode: {SearchMode}, scope: {SearchScope}", 
            searchTerm, searchMode, searchScope);

        return searchScope switch
        {
            SearchScope.Name => await _searchService.SearchByNameAsync(searchTerm, searchMode, cancellationToken),
            SearchScope.Text => await _searchService.SearchByTextAsync(searchTerm, searchMode, cancellationToken),
            SearchScope.Key => await _searchService.SearchByKeyAsync(searchTerm, searchMode, cancellationToken),
            SearchScope.All => await _searchService.SearchAllAsync(searchTerm, searchMode, cancellationToken),
            _ => throw new ArgumentException($"Invalid search scope: {searchScope}")
        };
    }
} 