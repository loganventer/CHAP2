// Clean Search Service
class SearchService {
    constructor() {
        this.baseUrl = '/api/search';
        this.debounceTimeout = null;
        this.debounceDelay = 300;
        this.isSearching = false;
    }

    /**
     * Perform a regular search with debouncing
     * @param {string} query - Search query
     * @param {Object} options - Search options
     * @returns {Promise<SearchResult>}
     */
    async search(query, options = {}) {
        return this._performSearch('search', query, options);
    }

    /**
     * Perform an AI search
     * @param {string} query - Search query
     * @param {Object} options - Search options
     * @returns {Promise<SearchResult>}
     */
    async aiSearch(query, options = {}) {
        return this._performSearch('ai-search', query, options);
    }

    /**
     * Perform search with debouncing
     * @param {string} query - Search query
     * @param {Function} callback - Callback function
     * @param {Object} options - Search options
     */
    debouncedSearch(query, callback, options = {}) {
        // Clear existing timeout
        if (this.debounceTimeout) {
            clearTimeout(this.debounceTimeout);
        }

        // Set new timeout
        this.debounceTimeout = setTimeout(async () => {
            try {
                const result = await this.search(query, options);
                callback(result);
            } catch (error) {
                console.error('Debounced search error:', error);
                callback({ error: error.message });
            }
        }, this.debounceDelay);
    }

    /**
     * Perform the actual search request
     * @param {string} endpoint - API endpoint
     * @param {string} query - Search query
     * @param {Object} options - Search options
     * @returns {Promise<SearchResult>}
     */
    async _performSearch(endpoint, query, options = {}) {
        if (this.isSearching) {
            throw new Error('Search already in progress');
        }

        if (!query || query.trim().length === 0) {
            return {
                results: [],
                totalCount: 0,
                metadata: { query: query }
            };
        }

        this.isSearching = true;

        try {
            const requestBody = {
                query: query.trim(),
                searchMode: options.searchMode || 'Contains',
                searchScope: options.searchScope || 'All',
                maxResults: options.maxResults || 50
            };

            const response = await fetch(`${this.baseUrl}/${endpoint}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestBody)
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.error || `HTTP ${response.status}: ${response.statusText}`);
            }

            const data = await response.json();
            
            return {
                results: data.results || [],
                totalCount: data.totalCount || 0,
                metadata: data.metadata || {},
                error: null
            };
        } catch (error) {
            console.error('Search error:', error);
            return {
                results: [],
                totalCount: 0,
                error: error.message,
                metadata: { query: query }
            };
        } finally {
            this.isSearching = false;
        }
    }

    /**
     * Cancel any pending search
     */
    cancelSearch() {
        if (this.debounceTimeout) {
            clearTimeout(this.debounceTimeout);
            this.debounceTimeout = null;
        }
        this.isSearching = false;
    }

    /**
     * Set debounce delay
     * @param {number} delay - Delay in milliseconds
     */
    setDebounceDelay(delay) {
        this.debounceDelay = Math.max(0, delay);
    }
}

// Export for use in other modules
window.SearchService = SearchService; 