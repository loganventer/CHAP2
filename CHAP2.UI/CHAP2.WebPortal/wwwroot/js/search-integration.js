// Search Integration - Initializes the clean search system
document.addEventListener('DOMContentLoaded', function() {
    // Initialize search UI
    const searchContainer = document.getElementById('searchContainer');
    if (searchContainer) {
        const searchUI = new SearchUI('searchContainer', {
            debounceDelay: 300,
            minQueryLength: 2,
            showLoadingIndicator: true,
            showErrorMessages: true
        });

        // Store reference for potential future use
        window.searchUI = searchUI;
    }

    // Initialize AI search if container exists
    const aiSearchContainer = document.getElementById('aiSearchContainer');
    if (aiSearchContainer) {
        const aiSearchUI = new SearchUI('aiSearchContainer', {
            debounceDelay: 500, // Longer delay for AI search
            minQueryLength: 3,
            showLoadingIndicator: true,
            showErrorMessages: true
        });

        // Override the search method to use AI search
        aiSearchUI.searchService.search = async function(query, options = {}) {
            return this._performSearch('ai-search', query, options);
        };

        // Store reference for potential future use
        window.aiSearchUI = aiSearchUI;
    }
});

// Global functions for chorus actions
function viewChorus(id) {
    window.open(`/Home/Detail/${id}`, '_blank');
}

function editChorus(id) {
    window.open(`/Home/Edit/${id}`, '_blank');
} 