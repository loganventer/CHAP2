// Search functionality
let searchResults = [];
let isSearching = false;
let isConnected = false;
let currentSearchTerm = '';
let searchTimeout = null;
let searchDelay = 300; // milliseconds - match console app
let minSearchLength = 2; // minimum characters - match console app

// Initialize search functionality
function initializeSearch() {
    console.log('Initializing search functionality...');
    const searchInput = document.getElementById('searchInput');
    const clearBtn = document.getElementById('clearBtn');
    const statusIndicator = document.getElementById('statusIndicator');
    const statusText = document.getElementById('statusText');
    
    if (searchInput) {
        console.log('Search input found, setting up event listeners...');
        // Debounced search function
        const debouncedSearch = debounce(performSearch, searchDelay);
        
        searchInput.addEventListener('input', function() {
            const value = this.value.trim();
            currentSearchTerm = value;
            console.log('Search input event fired, value:', value);
            
            // Show/hide clear button
            if (value.length > 0) {
                clearBtn.style.display = 'flex';
            } else {
                clearBtn.style.display = 'none';
                clearResults();
            }
            
            // Perform search automatically on every keystroke
            if (value.length >= minSearchLength) {
                console.log('Triggering search for:', value);
                debouncedSearch(value);
            } else if (value.length === 0) {
                clearResults();
            } else if (value.length === 1) {
                // Special handling for single character - treat as key search
                console.log('Triggering single character search for:', value);
                debouncedSearch(value);
            }
        });
        
        // Clear button functionality
        clearBtn.addEventListener('click', function() {
            searchInput.value = '';
            currentSearchTerm = '';
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
        
        // Enter key handling (optional - for accessibility)
        searchInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const value = this.value.trim();
                if (value.length >= minSearchLength) {
                    performSearch(value);
                }
            }
        });
    } else {
        console.error('Search input not found!');
    }
    
    // Update status indicator
    function updateStatus(connected) {
        isConnected = connected;
        if (statusIndicator && statusText) {
            if (connected) {
                statusIndicator.className = 'fas fa-circle status-indicator connected';
                statusText.textContent = 'Connected';
            } else {
                statusIndicator.className = 'fas fa-circle status-indicator disconnected';
                statusText.textContent = 'Disconnected';
            }
        }
    }
    
    // Test connectivity on startup
    testConnectivity().then(connected => {
        updateStatus(connected);
    });
}

// Debounce function
function debounce(func, wait) {
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(searchTimeout);
            func(...args);
        };
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(later, wait);
    };
}

// Perform search
async function performSearch(searchTerm) {
    if (isSearching || !searchTerm) return;
    
    currentSearchTerm = searchTerm;
    isSearching = true;
    
    // Show loading state
    showLoading();
    
    try {
        // Determine search type based on term length and content
        let searchUrl = `/Home/Search?q=${encodeURIComponent(searchTerm)}`;
        
        // For single character, treat as potential key search
        if (searchTerm.length === 1) {
            const upperTerm = searchTerm.toUpperCase();
            const validKeys = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'BB', 'AB', 'EB', 'DB', 'GB'];
            if (validKeys.includes(upperTerm)) {
                searchUrl += '&searchIn=key';
            }
        }
        
        const response = await fetch(searchUrl, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        if (data.error) {
            throw new Error(data.error);
        }
        
        searchResults = data.results || [];
        
        // Apply sorting similar to console app
        if (searchResults.length > 0) {
            searchResults = sortSearchResults(searchResults, searchTerm);
        }
        
        displayResults(searchResults, searchTerm);
        
    } catch (error) {
        console.error('Search error:', error);
        showError('Search failed. Please try again.');
    } finally {
        isSearching = false;
        hideLoading();
    }
}

// Sort search results similar to console app
function sortSearchResults(results, searchTerm) {
    const lowerSearch = searchTerm.toLowerCase();
    const upperSearch = searchTerm.toUpperCase();
    
    // Check if search term is a valid musical key
    const validKeys = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'BB', 'AB', 'EB', 'DB', 'GB'];
    const isKeySearch = validKeys.includes(upperSearch);
    
    if (isKeySearch) {
        // For key searches: exact key match first, then alphabetically by title
        return results.sort((a, b) => {
            const aExactMatch = a.key && a.key.toString().toUpperCase() === upperSearch;
            const bExactMatch = b.key && b.key.toString().toUpperCase() === upperSearch;
            
            if (aExactMatch && !bExactMatch) return -1;
            if (!aExactMatch && bExactMatch) return 1;
            
            // Both have same exact match status, sort by title
            return (a.name || '').localeCompare(b.name || '');
        });
    } else {
        // For text searches: title match first, then text match, then alphabetically by title
        return results.sort((a, b) => {
            const aTitleMatch = a.name && a.name.toLowerCase().includes(lowerSearch);
            const bTitleMatch = b.name && b.name.toLowerCase().includes(lowerSearch);
            const aTextMatch = a.chorusText && a.chorusText.toLowerCase().includes(lowerSearch);
            const bTextMatch = b.chorusText && b.chorusText.toLowerCase().includes(lowerSearch);
            
            // Title match priority
            if (aTitleMatch && !bTitleMatch) return -1;
            if (!aTitleMatch && bTitleMatch) return 1;
            
            // Text match priority
            if (aTextMatch && !bTextMatch) return -1;
            if (!aTextMatch && bTextMatch) return 1;
            
            // Alphabetical by title
            return (a.name || '').localeCompare(b.name || '');
        });
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
        const response = await fetch(`/Home/DetailPartial/${chorusId}`, {
            method: 'GET',
            headers: {
                'Accept': 'text/html',
                'Content-Type': 'text/html'
            }
        });
        
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
}

// Clear search results
function clearResults() {
    const resultsTable = document.getElementById('resultsTable');
    const resultsHeader = document.getElementById('resultsHeader');
    const resultsBody = document.getElementById('resultsBody');
    const noResults = document.getElementById('noResults');
    
    resultsTable.style.display = 'none';
    resultsHeader.style.display = 'none';
    noResults.style.display = 'none';
    resultsBody.innerHTML = '';
    searchResults = [];
}

// Show loading state
function showLoading() {
    const loadingIndicator = document.getElementById('loading');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'flex';
    }
}

// Hide loading state
function hideLoading() {
    const loadingIndicator = document.getElementById('loading');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'none';
    }
}

// Show error message
function showError(message) {
    utils.showNotification(message, 'error');
}

// Animate results appearance
function animateResults() {
    const rows = document.querySelectorAll('.result-row');
    rows.forEach((row, index) => {
        row.style.opacity = '0';
        row.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            row.style.transition = 'all 0.3s ease';
            row.style.opacity = '1';
            row.style.transform = 'translateY(0)';
        }, index * 50);
    });
}

// Test API connectivity
async function testConnectivity() {
    try {
        const response = await fetch('/Home/TestConnectivity', {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });
        return response.ok;
    } catch (error) {
        console.error('Connectivity test failed:', error);
        return false;
    }
}

// Export search results
function exportResults() {
    if (searchResults.length === 0) {
        utils.showNotification('No results to export', 'info');
        return;
    }
    
    const csv = convertToCSV(searchResults);
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `chorus_search_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
    
    utils.showNotification('Results exported successfully!', 'success');
}

// Convert results to CSV
function convertToCSV(data) {
    const headers = ['Title', 'Key', 'Type', 'Time Signature', 'Text'];
    const csvContent = [
        headers.join(','),
        ...data.map(item => [
            `"${(item.name || '').replace(/"/g, '""')}"`,
            `"${(item.key || '').replace(/"/g, '""')}"`,
            `"${(item.type || '').replace(/"/g, '""')}"`,
            `"${(item.timeSignature || '').replace(/"/g, '""')}"`,
            `"${(item.chorusText || '').replace(/"/g, '""')}"`
        ].join(','))
    ].join('\n');
    
    return csvContent;
}

// Event listeners
document.addEventListener('DOMContentLoaded', function() {
    // Initialize search functionality
    initializeSearch();
    
    // Test connectivity
    testConnectivity().then(connected => {
        console.log('Connectivity test result:', connected);
    });
    
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
    
    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        // Escape key - clear search or close modal
        if (e.key === 'Escape') {
            const modal = document.getElementById('detailModal');
            if (modal.style.display === 'flex') {
                closeModal();
            } else {
                const searchInput = document.getElementById('searchInput');
                const clearBtn = document.getElementById('clearBtn');
                if (searchInput && searchInput.value) {
                    searchInput.value = '';
                    clearBtn.style.display = 'none';
                    clearResults();
                    searchInput.focus();
                }
            }
        }
        
        // Ctrl/Cmd + K - focus search input
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            const searchInput = document.getElementById('searchInput');
            if (searchInput) {
                searchInput.focus();
            }
        }
        
        // Enter key on search results - open first result
        if (e.key === 'Enter' && document.activeElement.id === 'searchInput') {
            const firstResult = document.querySelector('.result-row');
            if (firstResult) {
                const chorusId = firstResult.getAttribute('data-id');
                showDetail(chorusId);
            }
        }
    });
});

// Global functions
window.showDetail = showDetail;
window.openInNewWindow = openInNewWindow;
window.copyChorusText = copyChorusText;
window.closeModal = closeModal;
window.exportResults = exportResults;
window.testConnectivity = testConnectivity; 