// AI Search JavaScript
class AiSearch {
    constructor() {
        this.searchInput = document.getElementById('aiSearchInput');
        this.searchButton = document.getElementById('aiSearchButton');
        this.resultsContainer = document.getElementById('aiSearchResults');
        this.currentQuery = '';
        
        // Use the correct enum mappings that match the C# enums
        this.MusicalKeys = {
            0: 'Not Set',
            1: 'C', 2: 'C#', 3: 'D', 4: 'D#', 5: 'E', 6: 'F', 7: 'F#', 8: 'G', 9: 'G#', 10: 'A', 11: 'A#', 12: 'B',
            13: 'C♭', 14: 'D♭', 15: 'E♭', 16: 'F♭', 17: 'G♭', 18: 'A♭', 19: 'B♭'
        };
        
        this.ChorusTypes = {
            0: 'Not Set',
            1: 'Praise',
            2: 'Worship'
        };
        
        this.TimeSignatures = {
            0: 'Not Set',
            1: '4/4', 2: '3/4', 3: '6/8', 4: '2/4', 5: '4/8', 6: '3/8', 7: '2/2',
            8: '5/4', 9: '6/4', 10: '9/8', 11: '12/8', 12: '7/4', 13: '8/4',
            14: '5/8', 15: '7/8', 16: '8/8', 17: '2/16', 18: '3/16', 19: '4/16',
            20: '5/16', 21: '6/16', 22: '7/16', 23: '8/16', 24: '9/16', 25: '12/16'
        };
        
        this.init();
    }
    
    init() {
        this.setupEventListeners();
    }
    
    setupEventListeners() {
        // Search input events
        this.searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.performSearch();
            }
        });
        
        // Search button
        this.searchButton.addEventListener('click', () => {
            this.performSearch();
        });
        
        // Clear search
        const clearButton = document.getElementById('aiClearButton');
        if (clearButton) {
            clearButton.addEventListener('click', () => {
                this.clearSearch();
            });
        }
    }
    

    
    async performSearch() {
        const query = this.searchInput.value.trim();
        if (!query) {
            this.showNotification('Please enter a search term', 'warning');
            return;
        }
        
        this.currentQuery = query;
        this.showLoading();
        this.clearResults();
        
        try {
            // Use intelligent search with LLM + vector database
            await this.performIntelligentSearch(query);
            
        } catch (error) {
            console.error('Search error:', error);
            this.showNotification('Search failed. Please try again.', 'error');
        } finally {
            this.hideLoading();
        }
    }
    
    async performIntelligentSearch(query) {
        // Step 1: Show initial progress
        this.resultsContainer.innerHTML = `
            <div class="ai-progress-container">
                <div class="ai-progress-header">
                    <h2><i class="fas fa-robot"></i> Intelligent Search</h2>
                    <p>Understanding your query and searching the vector database...</p>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill"></div>
                </div>
            </div>
        `;
        
        try {
            // Step 2: Use intelligent search with LLM + vector database
            const searchResponse = await fetch('/Home/IntelligentSearch', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ query: query, maxResults: 10 })
            });
            
            if (!searchResponse.ok) {
                throw new Error(`HTTP ${searchResponse.status}: ${searchResponse.statusText}`);
            }
            
            const result = await searchResponse.json();
            
            // Step 3: Display search results first
            this.displaySearchResultsFirst(result.searchResults, query);
            
            // Step 4: Show query understanding
            if (result.queryUnderstanding) {
                this.displayQueryUnderstanding(result.queryUnderstanding);
            }
            
            // Step 5: Show AI analysis if available
            if (result.hasAiAnalysis && result.aiAnalysis) {
                this.displayAiAnalysis(result.aiAnalysis);
            }
            
        } catch (error) {
            console.error('Error performing intelligent search:', error);
            this.resultsContainer.innerHTML = `
                <div class="error-message">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>Sorry, I encountered an error while searching. Please try again.</p>
                    <p class="error-details">${error.message}</p>
                </div>
            `;
        }
    }
    
    displaySearchResultsFirst(results, query) {
        if (!results || results.length === 0) {
            this.resultsContainer.innerHTML = `
                <div class="no-results">
                    <i class="fas fa-search"></i>
                    <h3>No Results Found</h3>
                    <p>No choruses found matching your query: "${query}"</p>
                </div>
            `;
            return;
        }
        
        // Create table structure like regular search
        const tableHtml = `
            <div class="results-header">
                <h2><i class="fas fa-search"></i> Search Results (${results.length})</h2>
                <p>Found ${results.length} choruses matching your query</p>
            </div>
            <div class="results-table-container">
                <table class="results-table">
                    <thead>
                        <tr>
                            <th class="col-number">#</th>
                            <th class="col-title">Title</th>
                            <th class="col-key">Key</th>
                            <th class="col-type">Type</th>
                            <th class="col-time">Time</th>
                            <th class="col-context">Context</th>
                            <th class="col-actions">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${results.map((chorus, index) => this.createRegularResultRow(chorus, index + 1)).join('')}
                    </tbody>
                </table>
            </div>
        `;
        
        this.resultsContainer.innerHTML = tableHtml;
        
        // Add click handlers for rows
        this.setupRowClickHandlers();
    }
    
    displayQueryUnderstanding(understanding) {
        // Add query understanding above the search results
        const understandingHtml = `
            <div class="query-understanding-section">
                <div class="query-understanding-header">
                    <h3><i class="fas fa-lightbulb"></i> Query Understanding</h3>
                    <p>How the AI interpreted your search</p>
                </div>
                <div class="query-understanding-content">
                    <strong>Search terms generated:</strong> ${understanding}
                </div>
            </div>
        `;
        
        // Insert before the search results
        const resultsHeader = this.resultsContainer.querySelector('.results-header');
        if (resultsHeader) {
            resultsHeader.insertAdjacentHTML('beforebegin', understandingHtml);
        }
    }
    
    displayAiAnalysis(analysis) {
        // Add AI analysis below the search results
        const analysisHtml = `
            <div class="ai-analysis-section">
                <div class="ai-analysis-header">
                    <h3><i class="fas fa-robot"></i> AI Analysis</h3>
                    <p>Analysis of the search results</p>
                </div>
                <div class="ai-analysis-content">
                    ${analysis.replace(/\n/g, '<br>')}
                </div>
            </div>
        `;
        
        // Append to existing results
        this.resultsContainer.insertAdjacentHTML('beforeend', analysisHtml);
    }
    

    

    
    createRegularResultRow(chorus, index) {
        return `
            <tr class="result-row" data-id="${chorus.id}">
                <td class="col-number">${index}</td>
                <td class="col-title">
                    <strong>${this.highlightSearchTerm(chorus.name, this.currentQuery)}</strong>
                </td>
                <td class="col-key">${this.getKeyDisplay(chorus.key)}</td>
                <td class="col-type">${this.getTypeDisplay(chorus.type)}</td>
                <td class="col-time">${this.getTimeSignatureDisplay(chorus.timeSignature)}</td>
                <td class="col-context">${this.truncateText(chorus.chorusText, 100)}</td>
                <td class="col-actions">
                    <div class="action-buttons">
                        <button onclick="viewChorus('${chorus.id}')" class="btn-icon" title="View">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button onclick="editChorus('${chorus.id}')" class="btn-icon" title="Edit">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button onclick="shareChorus('${chorus.id}')" class="btn-icon" title="Share">
                            <i class="fas fa-share"></i>
                        </button>
                        <button onclick="deleteChorus('${chorus.id}')" class="btn-icon" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }
    

    

    

    

    

    
    highlightSearchTerm(text, searchTerm) {
        if (!searchTerm || !text) return text;
        
        const regex = new RegExp(`(${searchTerm.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
        return text.replace(regex, '<mark>$1</mark>');
    }
    
    setupRowClickHandlers() {
        // Add click handlers for result rows
        const resultRows = document.querySelectorAll('#aiSearchResults .result-row');
        resultRows.forEach(row => {
            row.addEventListener('click', function(e) {
                if (!e.target.closest('.action-btn')) {
                    const chorusId = this.getAttribute('data-id');
                    const url = `/Home/ChorusDisplay/${chorusId}`;
                    const windowFeatures = 'width=1200,height=800,scrollbars=no,resizable=yes,menubar=no,toolbar=no,location=no,status=no';
                    window.open(url, '_blank', windowFeatures);
                }
            });
        });
    }
    

    

    

    
    clearSearch() {
        this.searchInput.value = '';
        this.clearResults();
    }
    
    clearResults() {
        this.resultsContainer.innerHTML = '';
    }
    
    showLoading() {
        this.searchButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Searching...';
        this.searchButton.disabled = true;
    }
    
    hideLoading() {
        this.searchButton.innerHTML = '<i class="fas fa-robot"></i> AI Search';
        this.searchButton.disabled = false;
    }
    
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-message">${message}</span>
                <button class="notification-close">&times;</button>
            </div>
        `;
        
        // Add to page
        document.body.appendChild(notification);
        
        // Auto-remove after 3 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 3000);
        
        // Close button
        const closeBtn = notification.querySelector('.notification-close');
        closeBtn.addEventListener('click', () => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        });
    }
    
    getKeyDisplay(keyValue) {
        return this.MusicalKeys[keyValue] || 'Unknown';
    }
    
    getTypeDisplay(typeValue) {
        return this.ChorusTypes[typeValue] || 'Unknown';
    }
    
    getTimeSignatureDisplay(timeValue) {
        return this.TimeSignatures[timeValue] || 'Unknown';
    }
    
    truncateText(text, maxLength) {
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    }
}

// Global functions for chorus actions
function viewChorus(id) {
    window.open(`/Home/ChorusDisplay/${id}`, '_blank');
}

function editChorus(id) {
    window.open(`/Home/Edit/${id}`, '_blank');
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new AiSearch();
}); 