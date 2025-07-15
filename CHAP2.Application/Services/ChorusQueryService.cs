using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services
{
    public class ChorusQueryService : IChorusQueryService
    {
        private readonly IChorusRepository _chorusRepository;
        private readonly ILogger<ChorusQueryService> _logger;

        public ChorusQueryService(
            IChorusRepository chorusRepository,
            ILogger<ChorusQueryService> logger)
        {
            _chorusRepository = chorusRepository ?? throw new ArgumentNullException(nameof(chorusRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

            public async Task<IReadOnlyList<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving all choruses");
            var choruses = await _chorusRepository.GetAllAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} choruses", choruses.Count);
            return choruses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all choruses");
            throw;
        }
    }

            public async Task<Chorus?> GetChorusByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving chorus with ID: {Id}", id);
            var chorus = await _chorusRepository.GetByIdAsync(id, cancellationToken);
            
            if (chorus == null)
            {
                _logger.LogWarning("Chorus with ID {Id} not found", id);
            }
            else
            {
                _logger.LogInformation("Retrieved chorus with ID: {Id}", id);
            }
            
            return chorus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chorus with ID: {Id}", id);
            throw;
        }
    }

            public async Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving chorus with name: {Name}", name);
            var chorus = await _chorusRepository.GetByNameAsync(name, cancellationToken);
            
            if (chorus == null)
            {
                _logger.LogWarning("Chorus with name {Name} not found", name);
            }
            else
            {
                _logger.LogInformation("Retrieved chorus with name: {Name}", name);
            }
            
            return chorus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chorus with name: {Name}", name);
            throw;
        }
    }

    public async Task<IReadOnlyList<Chorus>> SearchChorusesAsync(string searchTerm, SearchMode searchMode, SearchScope searchScope, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogWarning("Search term is null or empty");
                return new List<Chorus>();
            }

            _logger.LogInformation("Searching choruses with term: {SearchTerm}, mode: {SearchMode}, scope: {SearchScope}", searchTerm, searchMode, searchScope);
            var results = await _chorusRepository.SearchAsync(searchTerm, cancellationToken);
            _logger.LogInformation("Found {Count} choruses matching search term: {SearchTerm}", results.Count, searchTerm);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching choruses with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

        
    }
} 