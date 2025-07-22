// Clean Search UI Component
class SearchUI {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        if (!this.container) {
            throw new Error(`Container with ID '${containerId}' not found`);
        }

        this.options = {
            debounceDelay: 300,
            minQueryLength: 2,
            showLoadingIndicator: true,
            showErrorMessages: true,
            ...options
        };

        this.searchService = new SearchService();
        this.searchService.setDebounceDelay(this.options.debounceDelay);

        this.currentQuery = '';
        this.isSearching = false;

        this.init();
    }

    init() {
        this.createUI();
        this.setupEventListeners();
    }

    createUI() {
        this.container.innerHTML = `
            <div class="search-container">
                <div class="search-input-container">
                    <div class="search-box">
                        <i class="fas fa-search search-icon"></i>
                        <input type="text" 
                               class="search-input" 
                               placeholder="Search choruses..."
                               autocomplete="off">
                        <div class="search-actions">
                            <button class="clear-btn" style="display: none;">
                                <i class="fas fa-times"></i>
                            </button>
                        </div>
                    </div>
                    <div class="search-info">
                        <span class="search-delay-info">Search triggers after typing stops (${this.options.debounceDelay}ms delay)</span>
                        <span class="min-length-info">Minimum ${this.options.minQueryLength} characters</span>
                    </div>
                </div>

                <div class="search-results-container">
                    <div class="results-header" style="display: none;">
                        <div class="results-count"></div>
                        <div class="results-controls">
                            <button class="btn-secondary export-btn" style="display: none;">
                                <i class="fas fa-download"></i> Export
                            </button>
                        </div>
                    </div>

                    <div class="loading-container" style="display: none;">
                        <div class="loading-spinner"></div>
                        <p>Searching...</p>
                    </div>

                    <div class="error-container" style="display: none;">
                        <i class="fas fa-exclamation-triangle"></i>
                        <p class="error-message"></p>
                    </div>

                    <div class="results-table-container">
                        <table class="results-table" style="display: none;">
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
                            <tbody></tbody>
                        </table>
                    </div>

                    <div class="no-results" style="display: none;">
                        <i class="fas fa-search"></i>
                        <p>No results found</p>
                    </div>
                </div>
            </div>
        `;

        // Cache DOM elements
        this.elements = {
            input: this.container.querySelector('.search-input'),
            clearBtn: this.container.querySelector('.clear-btn'),
            resultsHeader: this.container.querySelector('.results-header'),
            resultsCount: this.container.querySelector('.results-count'),
            loadingContainer: this.container.querySelector('.loading-container'),
            errorContainer: this.container.querySelector('.error-container'),
            errorMessage: this.container.querySelector('.error-message'),
            resultsTable: this.container.querySelector('.results-table'),
            resultsTableBody: this.container.querySelector('.results-table tbody'),
            noResults: this.container.querySelector('.no-results'),
            exportBtn: this.container.querySelector('.export-btn')
        };
    }

    setupEventListeners() {
        // Input events
        this.elements.input.addEventListener('input', (e) => {
            this.handleInputChange(e.target.value);
        });

        this.elements.input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.performSearch();
            }
        });

        // Clear button
        this.elements.clearBtn.addEventListener('click', () => {
            this.clearSearch();
        });

        // Export button
        this.elements.exportBtn.addEventListener('click', () => {
            this.exportResults();
        });
    }

    handleInputChange(value) {
        this.currentQuery = value.trim();
        
        // Show/hide clear button
        this.elements.clearBtn.style.display = this.currentQuery ? 'block' : 'none';

        // Perform debounced search
        if (this.currentQuery.length >= this.options.minQueryLength) {
            this.searchService.debouncedSearch(this.currentQuery, (result) => {
                this.handleSearchResult(result);
            });
        } else if (this.currentQuery.length === 0) {
            this.clearResults();
        }
    }

    async performSearch() {
        if (!this.currentQuery || this.currentQuery.length < this.options.minQueryLength) {
            return;
        }

        this.showLoading();

        try {
            const result = await this.searchService.search(this.currentQuery);
            this.handleSearchResult(result);
        } catch (error) {
            this.showError(error.message);
        } finally {
            this.hideLoading();
        }
    }

    handleSearchResult(result) {
        if (result.error) {
            this.showError(result.error);
            return;
        }

        this.hideError();
        this.hideLoading();

        if (result.results.length === 0) {
            this.showNoResults();
            return;
        }

        this.displayResults(result.results, result.totalCount, result.metadata);
    }

    displayResults(results, totalCount, metadata) {
        // Update results header
        this.elements.resultsCount.textContent = `Found ${totalCount} result${totalCount !== 1 ? 's' : ''}`;
        this.elements.resultsHeader.style.display = 'block';

        // Show export button if there are results
        this.elements.exportBtn.style.display = results.length > 0 ? 'block' : 'none';

        // Display AI context if available
        if (metadata && metadata.searchContext) {
            this.showAiContext(metadata.searchContext);
        }

        // Display AI search terms if available
        if (metadata && metadata.aiSearchTerms) {
            this.showAiSearchTerms(metadata.aiSearchTerms);
        }

        // Build table rows
        const tableRows = results.map((chorus, index) => this.createResultRow(chorus, index + 1));
        
        this.elements.resultsTableBody.innerHTML = tableRows.join('');
        this.elements.resultsTable.style.display = 'table';
        this.elements.noResults.style.display = 'none';

        // Setup row click handlers
        this.setupRowClickHandlers();
    }

    createResultRow(chorus, index) {
        const keyDisplay = this.getKeyDisplay(chorus.key);
        const typeDisplay = this.getTypeDisplay(chorus.type);
        const timeDisplay = this.getTimeSignatureDisplay(chorus.timeSignature);
        const contextText = this.truncateText(chorus.chorusText, 100);

        return `
            <tr class="result-row" data-id="${chorus.id}">
                <td class="col-number">${index}</td>
                <td class="col-title">
                    <div class="chorus-title">${this.highlightSearchTerm(chorus.name, this.currentQuery)}</div>
                </td>
                <td class="col-key">${keyDisplay}</td>
                <td class="col-type">${typeDisplay}</td>
                <td class="col-time">${timeDisplay}</td>
                <td class="col-context">
                    <div class="context-text">${this.highlightSearchTerm(contextText, this.currentQuery)}</div>
                </td>
                <td class="col-actions">
                    <button class="btn-action view-btn" onclick="viewChorus('${chorus.id}')" title="View Details">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="btn-action edit-btn" onclick="editChorus('${chorus.id}')" title="Edit">
                        <i class="fas fa-edit"></i>
                    </button>
                </td>
            </tr>
        `;
    }

    setupRowClickHandlers() {
        const rows = this.elements.resultsTableBody.querySelectorAll('.result-row');
        rows.forEach(row => {
            row.addEventListener('click', (e) => {
                // Don't trigger if clicking on action buttons
                if (e.target.closest('.btn-action')) {
                    return;
                }
                
                const chorusId = row.dataset.id;
                viewChorus(chorusId);
            });
        });
    }

    showLoading() {
        this.isSearching = true;
        this.elements.loadingContainer.style.display = 'block';
        this.elements.resultsTable.style.display = 'none';
        this.elements.noResults.style.display = 'none';
        this.hideError();
    }

    hideLoading() {
        this.isSearching = false;
        this.elements.loadingContainer.style.display = 'none';
    }

    showError(message) {
        if (!this.options.showErrorMessages) return;
        
        this.elements.errorMessage.textContent = message;
        this.elements.errorContainer.style.display = 'block';
        this.elements.resultsTable.style.display = 'none';
        this.elements.noResults.style.display = 'none';
    }

    hideError() {
        this.elements.errorContainer.style.display = 'none';
    }

    showNoResults() {
        this.elements.resultsTable.style.display = 'none';
        this.elements.noResults.style.display = 'block';
        this.elements.resultsHeader.style.display = 'none';
    }

    clearSearch() {
        this.currentQuery = '';
        this.elements.input.value = '';
        this.elements.clearBtn.style.display = 'none';
        this.clearResults();
        this.searchService.cancelSearch();
    }

    clearResults() {
        this.elements.resultsHeader.style.display = 'none';
        this.elements.resultsTable.style.display = 'none';
        this.elements.noResults.style.display = 'none';
        this.hideError();
        this.hideLoading();
    }

    exportResults() {
        // Implementation for exporting results
        console.log('Export functionality to be implemented');
    }

    // Utility methods
    highlightSearchTerm(text, searchTerm) {
        if (!searchTerm || !text) return text;
        
        const regex = new RegExp(`(${searchTerm})`, 'gi');
        return text.replace(regex, '<mark>$1</mark>');
    }

    truncateText(text, maxLength) {
        if (!text) return '';
        return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
    }

    getKeyDisplay(keyValue) {
        const keys = {
            0: 'Not Set', 1: 'C', 2: 'C#', 3: 'D', 4: 'D#', 5: 'E', 6: 'F', 7: 'F#', 8: 'G', 9: 'G#', 10: 'A', 11: 'A#', 12: 'B',
            13: 'C♭', 14: 'D♭', 15: 'E♭', 16: 'F♭', 17: 'G♭', 18: 'A♭', 19: 'B♭'
        };
        return keys[keyValue] || 'Unknown';
    }

    getTypeDisplay(typeValue) {
        const types = { 0: 'Not Set', 1: 'Praise', 2: 'Worship' };
        return types[typeValue] || 'Unknown';
    }

    getTimeSignatureDisplay(timeValue) {
        const times = {
            0: 'Not Set', 1: '4/4', 2: '3/4', 3: '6/8', 4: '2/4', 5: '4/8', 6: '3/8', 7: '2/2',
            8: '5/4', 9: '6/4', 10: '9/8', 11: '12/8', 12: '7/4', 13: '8/4',
            14: '5/8', 15: '7/8', 16: '8/8', 17: '2/16', 18: '3/16', 19: '4/16',
            20: '5/16', 21: '6/16', 22: '7/16', 23: '8/16', 24: '9/16', 25: '12/16'
        };
        return times[timeValue] || 'Unknown';
    }

    showAiContext(context) {
        // Create AI context element if it doesn't exist
        let contextElement = this.container.querySelector('.ai-context');
        if (!contextElement) {
            contextElement = document.createElement('div');
            contextElement.className = 'ai-context';
            contextElement.style.cssText = `
                background: #f8f9fa;
                border: 1px solid #e9ecef;
                border-radius: 4px;
                padding: 8px 12px;
                margin: 8px 0;
                font-size: 0.9em;
                color: #6c757d;
            `;
            this.elements.resultsHeader.appendChild(contextElement);
        }
        
        contextElement.textContent = `AI Analysis: ${context}`;
        contextElement.style.display = 'block';
    }

    showAiSearchTerms(terms) {
        // Create AI search terms element if it doesn't exist
        let termsElement = this.container.querySelector('.ai-search-terms');
        if (!termsElement) {
            termsElement = document.createElement('div');
            termsElement.className = 'ai-search-terms';
            termsElement.style.cssText = `
                background: #e3f2fd;
                border: 1px solid #bbdefb;
                border-radius: 4px;
                padding: 8px 12px;
                margin: 8px 0;
                font-size: 0.9em;
                color: #1976d2;
            `;
            this.elements.resultsHeader.appendChild(termsElement);
        }
        
        if (Array.isArray(terms)) {
            termsElement.innerHTML = `<strong>AI Search Terms:</strong> ${terms.join(', ')}`;
            termsElement.style.display = 'block';
        }
    }
}

// Export for use in other modules
window.SearchUI = SearchUI; 