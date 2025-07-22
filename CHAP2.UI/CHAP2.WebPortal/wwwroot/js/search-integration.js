// Search Integration - Initializes the clean search system
document.addEventListener('DOMContentLoaded', function() {
    console.log('Search Integration: Initializing...');
    
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
        console.log('Search Integration: Regular search UI initialized');
    }

    // Initialize AI search if container exists
    const aiSearchContainer = document.getElementById('aiSearchContainer');
    if (aiSearchContainer) {
        console.log('Search Integration: Initializing AI search...');
        
        // Create AI search interface
        aiSearchContainer.innerHTML = `
            <div class="search-container">
                <div class="search-input-container">
                    <div class="search-box">
                        <i class="fas fa-robot search-icon"></i>
                        <input type="text" 
                               class="search-input" 
                               id="aiSearchInput"
                               placeholder="Search choruses with AI analysis..."
                               maxlength="200"
                               autocomplete="off">
                        <div class="search-actions">
                            <button class="clear-btn" id="aiClearBtn" style="display: none;">
                                <i class="fas fa-times"></i>
                            </button>
                        </div>
                    </div>
                    <div class="search-info">
                        <span class="search-delay-info">AI-powered search with intelligent analysis</span>
                        <span class="min-length-info">Maximum 200 characters</span>
                    </div>
                    
                    <!-- Filter Options -->
                    <div class="row mt-3" style="margin-top: 15px;">
                        <div class="col-md-4">
                            <div class="form-group">
                                <label for="filterKey">Musical Key:</label>
                                <select class="form-control" id="filterKey">
                                    <option value="">Any Key</option>
                                    <option value="1">C</option>
                                    <option value="2">C#</option>
                                    <option value="3">D</option>
                                    <option value="4">D#</option>
                                    <option value="5">E</option>
                                    <option value="6">F</option>
                                    <option value="7">F#</option>
                                    <option value="8">G</option>
                                    <option value="9">G#</option>
                                    <option value="10">A</option>
                                    <option value="11">A#</option>
                                    <option value="12">B</option>
                                    <option value="13">C♭</option>
                                    <option value="14">D♭</option>
                                    <option value="15">E♭</option>
                                    <option value="16">F♭</option>
                                    <option value="17">G♭</option>
                                    <option value="18">A♭</option>
                                    <option value="19">B♭</option>
                                </select>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label for="filterType">Chorus Type:</label>
                                <select class="form-control" id="filterType">
                                    <option value="">Any Type</option>
                                    <option value="1">Praise</option>
                                    <option value="2">Worship</option>
                                </select>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="form-group">
                                <label for="filterTimeSignature">Time Signature:</label>
                                <select class="form-control" id="filterTimeSignature">
                                    <option value="">Any Time Signature</option>
                                    <option value="1">4/4</option>
                                    <option value="2">3/4</option>
                                    <option value="3">6/8</option>
                                    <option value="4">2/4</option>
                                    <option value="5">4/8</option>
                                    <option value="6">3/8</option>
                                    <option value="7">2/2</option>
                                    <option value="8">5/4</option>
                                    <option value="9">6/4</option>
                                    <option value="10">9/8</option>
                                    <option value="11">12/8</option>
                                    <option value="12">7/4</option>
                                    <option value="13">8/4</option>
                                    <option value="14">5/8</option>
                                    <option value="15">7/8</option>
                                    <option value="16">8/8</option>
                                </select>
                            </div>
                        </div>
                    </div>
                    
                    <div class="form-group mt-3" style="margin-top: 15px;">
                        <label for="maxResults">Max Results:</label>
                        <select class="form-control" id="maxResults">
                            <option value="5">5 results</option>
                            <option value="10" selected>10 results</option>
                            <option value="15">15 results</option>
                            <option value="20">20 results</option>
                        </select>
                    </div>
                    
                    <div class="mt-3" style="margin-top: 15px;">
                        <button class="btn btn-primary" id="aiSearchBtn">
                            <span class="btn-text">Search with AI Analysis</span>
                            <span class="btn-loading" style="display: none;">
                                <i class="fas fa-spinner fa-spin"></i> Processing...
                            </span>
                        </button>
                        <button class="btn btn-secondary ml-2" id="aiClearFiltersBtn">
                            <i class="fas fa-times"></i> Clear Filters
                        </button>
                    </div>
                </div>

                <div class="search-results-container">
                    <div id="aiSearchResults" style="display: none;">
                        <div class="card">
                            <div class="card-header">
                                <h5>Search Results</h5>
                            </div>
                            <div class="card-body">
                                <div id="aiResultsList"></div>
                            </div>
                        </div>
                    </div>
                    
                    <div id="aiAnalysis" style="display: none;">
                        <div class="card">
                            <div class="card-header">
                                <h5>AI Analysis</h5>
                            </div>
                            <div class="card-body">
                                <div id="aiAnalysisContent"></div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Initialize the AiSearch class for the AI tab
        window.aiSearch = new AiSearch();
        console.log('Search Integration: AI search initialized');
    }
    
    // Set up tab switching
    setupTabSwitching();
});

function setupTabSwitching() {
    const tabButtons = document.querySelectorAll('.tab-button');
    const searchSections = document.querySelectorAll('.search-section');
    
    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const targetTab = button.getAttribute('data-tab');
            console.log('Tab switched to:', targetTab);
            
            // Clear all search results before switching
            clearAllSearchResults();
            
            // Update active tab button
            tabButtons.forEach(btn => btn.classList.remove('active'));
            button.classList.add('active');
            
            // Update active search section
            searchSections.forEach(section => {
                section.classList.remove('active');
                section.style.display = 'none';
            });
            
            const targetSection = document.getElementById(targetTab + 'Search');
            if (targetSection) {
                targetSection.classList.add('active');
                targetSection.style.display = 'block';
            }
            
            // Re-initialize search if switching to regular search
            if (targetTab === 'regular') {
                console.log('Switching to regular search, re-initializing...');
                setTimeout(() => {
                    if (typeof initializeSearch === 'function') {
                        initializeSearch();
                    }
                }, 100);
            }
            
            console.log('Tab switching completed');
        });
    });
}

// Function to clear all search results
function clearAllSearchResults() {
    console.log('Clearing all search results...');
    
    // Clear traditional search results
    const resultsTable = document.getElementById('resultsTable');
    const resultsBody = document.getElementById('resultsBody');
    const resultsHeader = document.getElementById('resultsHeader');
    const resultsCount = document.getElementById('resultsCount');
    const noResults = document.getElementById('noResults');
    
    if (resultsBody) {
        resultsBody.innerHTML = '';
    }
    if (resultsHeader) {
        resultsHeader.style.display = 'none';
    }
    if (resultsCount) {
        resultsCount.textContent = '';
    }
    if (noResults) {
        noResults.style.display = 'none';
    }
    
    // Clear AI search results
    const aiResultsList = document.getElementById('aiResultsList');
    const aiSearchResults = document.getElementById('aiSearchResults');
    const aiResults = document.getElementById('aiResults');
    const aiAnalysis = document.getElementById('aiAnalysis');
    const aiAnalysisContent = document.getElementById('aiAnalysisContent');
    const queryUnderstandingSection = document.querySelector('.query-understanding-section');
    
    if (aiResultsList) {
        aiResultsList.innerHTML = '';
    }
    if (aiSearchResults) {
        aiSearchResults.innerHTML = '';
        aiSearchResults.style.display = 'none';
    }
    if (aiResults) {
        aiResults.style.display = 'none';
    }
    if (aiAnalysis) {
        aiAnalysis.style.display = 'none';
    }
    if (aiAnalysisContent) {
        aiAnalysisContent.innerHTML = '';
    }
    if (queryUnderstandingSection) {
        queryUnderstandingSection.style.display = 'none';
    }
    
    // Clear AI status
    const aiStatusIndicator = document.getElementById('aiStatusIndicator');
    if (aiStatusIndicator) {
        aiStatusIndicator.style.display = 'none';
    }
    
    // Clear search inputs
    const searchInput = document.getElementById('searchInput');
    const aiSearchInput = document.querySelector('#aiSearch input[type="text"]');
    
    if (searchInput) {
        searchInput.value = '';
    }
    if (aiSearchInput) {
        aiSearchInput.value = '';
    }
    
    console.log('All search results cleared');
}

// Global functions for chorus actions
function viewChorus(id) {
    window.open(`/Home/Detail/${id}`, '_blank');
}

function editChorus(id) {
    window.open(`/Home/Edit/${id}`, '_blank');
} 