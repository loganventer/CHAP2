// Search functionality
let searchResults = [];
let isSearching = false;

// Initialize search functionality
function initializeSearch() {
    const searchInput = document.getElementById('searchInput');
    const clearBtn = document.getElementById('clearBtn');
    
    if (searchInput) {
        // Debounced search
        const debouncedSearch = utils.debounce(performSearch, searchDelay);
        
        searchInput.addEventListener('input', function() {
            const value = this.value.trim();
            
            // Show/hide clear button
            if (value.length > 0) {
                clearBtn.style.display = 'flex';
            } else {
                clearBtn.style.display = 'none';
                clearResults();
            }
            
            // Perform search if minimum length
            if (value.length >= 2) {
                debouncedSearch(value);
            } else if (value.length === 0) {
                clearResults();
            }
        });
        
        // Clear button functionality
        clearBtn.addEventListener('click', function() {
            searchInput.value = '';
            searchInput.focus();
            clearResults();
            this.style.display = 'none';
        });
        
        // Focus management
        searchInput.addEventListener('focus', function() {
            this.parentElement.classList.add('focused');
        });
        
        searchInput.addEventListener('blur', function() {
            this.parentElement.classList.remove('focused');
        });
    }
}

// Perform search
async function performSearch(searchTerm) {
    if (isSearching || searchTerm.length < 2) return;
    
    currentSearchTerm = searchTerm;
    isSearching = true;
    
    // Show loading state
    showLoading();
    
    try {
        const response = await fetch(`/Home/Search?q=${encodeURIComponent(searchTerm)}`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        if (data.error) {
            throw new Error(data.error);
        }
        
        searchResults = data.results || [];
        displayResults(searchResults, searchTerm);
        
    } catch (error) {
        console.error('Search error:', error);
        showError('Search failed. Please try again.');
    } finally {
        isSearching = false;
        hideLoading();
    }
}

// Display search results
function displayResults(results, searchTerm) {
    const resultsTable = document.getElementById('resultsTable');
    const resultsBody = document.getElementById('resultsBody');
    const resultsHeader = document.getElementById('resultsHeader');
    const resultsCount = document.getElementById('resultsCount');
    const noResults = document.getElementById('noResults');
    
    // Clear previous results
    resultsBody.innerHTML = '';
    
    if (results.length === 0) {
        // Show no results
        resultsTable.style.display = 'none';
        resultsHeader.style.display = 'none';
        noResults.style.display = 'flex';
        return;
    }
    
    // Show results
    noResults.style.display = 'none';
    resultsTable.style.display = 'table';
    resultsHeader.style.display = 'flex';
    
    // Update count
    resultsCount.textContent = `${results.length} result${results.length !== 1 ? 's' : ''} for "${searchTerm}"`;
    
    // Populate table
    results.forEach((result, index) => {
        const row = createResultRow(result, index + 1);
        resultsBody.appendChild(row);
    });
    
    // Animate results
    animateResults();
}

// Create result row
function createResultRow(result, index) {
    const row = document.createElement('tr');
    row.className = 'result-row';
    row.setAttribute('data-id', result.id);
    
    // Highlight search term in title
    const highlightedTitle = highlightSearchTerm(result.name, currentSearchTerm);
    
    row.innerHTML = `
        <td class="result-number">${index}</td>
        <td class="result-title">${highlightedTitle}</td>
        <td class="result-key">${result.key}</td>
        <td class="result-type">${result.type}</td>
        <td class="result-time">${result.timeSignature}</td>
        <td class="result-context">${utils.truncateText(result.chorusText, 80)}</td>
        <td class="result-actions">
            <button class="action-btn" onclick="showDetail('${result.id}')" data-tooltip="View Details">
                <i class="fas fa-eye"></i>
            </button>
            <button class="action-btn" onclick="openInNewWindow('${result.id}')" data-tooltip="Open in New Window">
                <i class="fas fa-external-link-alt"></i>
            </button>
            <button class="action-btn" onclick="copyChorusText('${result.id}')" data-tooltip="Copy Lyrics">
                <i class="fas fa-copy"></i>
            </button>
        </td>
    `;
    
    // Add click handler for row
    row.addEventListener('click', function(e) {
        if (!e.target.closest('.action-btn')) {
            showDetail(result.id);
        }
    });
    
    return row;
}

// Highlight search term in text
function highlightSearchTerm(text, searchTerm) {
    if (!searchTerm) return utils.escapeHtml(text);
    
    const regex = new RegExp(`(${utils.escapeHtml(searchTerm)})`, 'gi');
    return utils.escapeHtml(text).replace(regex, '<mark>$1</mark>');
}

// Show chorus detail
async function showDetail(chorusId) {
    const modal = document.getElementById('detailModal');
    const modalContent = document.getElementById('modalContent');
    const modalTitle = document.getElementById('modalTitle');
    
    try {
        const response = await fetch(`/Home/DetailPartial/${chorusId}`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const html = await response.text();
        modalContent.innerHTML = html;
        
        // Get chorus name for modal title
        const chorus = searchResults.find(r => r.id === chorusId);
        if (chorus) {
            modalTitle.textContent = chorus.name;
        }
        
        // Show modal
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
        
        // Focus management
        const closeBtn = document.getElementById('modalClose');
        if (closeBtn) {
            closeBtn.focus();
        }
        
    } catch (error) {
        console.error('Error loading chorus detail:', error);
        utils.showNotification('Failed to load chorus details', 'error');
    }
}

// Open chorus in new window
function openInNewWindow(chorusId) {
    const url = `/Home/Detail/${chorusId}`;
    const windowFeatures = 'width=1000,height=800,scrollbars=yes,resizable=yes';
    window.open(url, '_blank', windowFeatures);
}

// Copy chorus text to clipboard
async function copyChorusText(chorusId) {
    const chorus = searchResults.find(r => r.id === chorusId);
    if (chorus) {
        await utils.copyToClipboard(chorus.chorusText);
    }
}

// Close modal
function closeModal() {
    const modal = document.getElementById('detailModal');
    modal.style.display = 'none';
    document.body.style.overflow = '';
    
    // Return focus to search input
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.focus();
    }
}

// Clear results
function clearResults() {
    const resultsTable = document.getElementById('resultsTable');
    const resultsHeader = document.getElementById('resultsHeader');
    const noResults = document.getElementById('noResults');
    
    resultsTable.style.display = 'none';
    resultsHeader.style.display = 'none';
    noResults.style.display = 'none';
    
    searchResults = [];
    currentSearchTerm = '';
}

// Show loading state
function showLoading() {
    const loading = document.getElementById('loading');
    const resultsTable = document.getElementById('resultsTable');
    const resultsHeader = document.getElementById('resultsHeader');
    const noResults = document.getElementById('noResults');
    
    loading.style.display = 'flex';
    resultsTable.style.display = 'none';
    resultsHeader.style.display = 'none';
    noResults.style.display = 'none';
}

// Hide loading state
function hideLoading() {
    const loading = document.getElementById('loading');
    loading.style.display = 'none';
}

// Show error
function showError(message) {
    const noResults = document.getElementById('noResults');
    const noResultsContent = noResults.querySelector('.no-results-content');
    
    noResultsContent.innerHTML = `
        <i class="fas fa-exclamation-triangle no-results-icon"></i>
        <h3>Search Error</h3>
        <p>${message}</p>
    `;
    
    noResults.style.display = 'flex';
}

// Animate results
function animateResults() {
    const rows = document.querySelectorAll('.result-row');
    rows.forEach((row, index) => {
        row.style.animationDelay = `${index * 50}ms`;
    });
}

// Test API connectivity
async function testConnectivity() {
    const statusIndicator = document.getElementById('statusIndicator');
    const statusText = document.getElementById('statusText');
    
    try {
        const response = await fetch('/Home/TestConnectivity');
        const data = await response.json();
        
        if (data.connected) {
            statusIndicator.className = 'fas fa-circle status-indicator connected';
            statusText.textContent = 'Connected';
        } else {
            statusIndicator.className = 'fas fa-circle status-indicator disconnected';
            statusText.textContent = 'Disconnected';
        }
    } catch (error) {
        console.error('Connectivity test failed:', error);
        statusIndicator.className = 'fas fa-circle status-indicator disconnected';
        statusText.textContent = 'Connection Error';
    }
}

// Export results
function exportResults() {
    if (searchResults.length === 0) {
        utils.showNotification('No results to export', 'info');
        return;
    }
    
    const exportData = searchResults.map(result => ({
        Title: result.name,
        Key: result.key,
        Type: result.type,
        TimeSignature: result.timeSignature,
        Lyrics: result.chorusText
    }));
    
    const csv = convertToCSV(exportData);
    const filename = `chorus_search_${new Date().toISOString().split('T')[0]}.csv`;
    
    utils.downloadText(csv, filename);
}

// Convert data to CSV
function convertToCSV(data) {
    if (data.length === 0) return '';
    
    const headers = Object.keys(data[0]);
    const csvRows = [headers.join(',')];
    
    for (const row of data) {
        const values = headers.map(header => {
            const value = row[header];
            const escaped = value.toString().replace(/"/g, '""');
            return `"${escaped}"`;
        });
        csvRows.push(values.join(','));
    }
    
    return csvRows.join('\n');
}

// Event listeners
document.addEventListener('DOMContentLoaded', function() {
    // Modal close button
    const modalClose = document.getElementById('modalClose');
    if (modalClose) {
        modalClose.addEventListener('click', closeModal);
    }
    
    // Modal overlay click
    const modalOverlay = document.getElementById('detailModal');
    if (modalOverlay) {
        modalOverlay.addEventListener('click', function(e) {
            if (e.target === this) {
                closeModal();
            }
        });
    }
    
    // Export button
    const exportBtn = document.getElementById('exportBtn');
    if (exportBtn) {
        exportBtn.addEventListener('click', exportResults);
    }
});

// Global functions
window.showDetail = showDetail;
window.openInNewWindow = openInNewWindow;
window.copyChorusText = copyChorusText;
window.closeModal = closeModal;
window.exportResults = exportResults;
window.testConnectivity = testConnectivity; 