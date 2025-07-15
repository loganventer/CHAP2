using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services
{
    public class ChorusCommandService : IChorusCommandService
    {
        private readonly IChorusRepository _chorusRepository;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly ILogger<ChorusCommandService> _logger;

        public ChorusCommandService(
            IChorusRepository chorusRepository,
            IDomainEventDispatcher eventDispatcher,
            ILogger<ChorusCommandService> logger)
        {
            _chorusRepository = chorusRepository ?? throw new ArgumentNullException(nameof(chorusRepository));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

            public async Task<Chorus> CreateChorusAsync(string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new chorus with name: {Name}", name);
            
            var chorus = Chorus.Create(name, chorusText, key, type, timeSignature);
            
            await _chorusRepository.AddAsync(chorus, cancellationToken);
            
            // Dispatch domain event
            await _eventDispatcher.DispatchAsync(new ChorusCreatedEvent(chorus.Id, chorus.Name));
            
            _logger.LogInformation("Successfully created chorus with ID: {Id}", chorus.Id);
            return chorus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chorus with name: {Name}", name);
            throw;
        }
    }

            public async Task<Chorus> UpdateChorusAsync(Guid id, string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating chorus with ID: {Id}", id);
            
            var existingChorus = await _chorusRepository.GetByIdAsync(id, cancellationToken);
            if (existingChorus == null)
            {
                _logger.LogWarning("Chorus with ID {Id} not found for update", id);
                throw new InvalidOperationException($"Chorus with ID {id} not found");
            }

            existingChorus.Update(name, chorusText, key, type, timeSignature);
            await _chorusRepository.UpdateAsync(existingChorus, cancellationToken);
            
            _logger.LogInformation("Successfully updated chorus with ID: {Id}", id);
            return existingChorus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chorus with ID: {Id}", id);
            throw;
        }
    }

            public async Task DeleteChorusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting chorus with ID: {Id}", id);
            
            var existingChorus = await _chorusRepository.GetByIdAsync(id, cancellationToken);
            if (existingChorus == null)
            {
                _logger.LogWarning("Chorus with ID {Id} not found for deletion", id);
                throw new InvalidOperationException($"Chorus with ID {id} not found");
            }

            await _chorusRepository.DeleteAsync(id, cancellationToken);
            
            _logger.LogInformation("Successfully deleted chorus with ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chorus with ID: {Id}", id);
            throw;
        }
    }

        
    }
} 