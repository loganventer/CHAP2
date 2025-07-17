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
        if (statusIndicator) {
            if (connected) {
                statusIndicator.className = 'fas fa-circle status-indicator connected';
            } else {
                statusIndicator.className = 'fas fa-circle status-indicator disconnected';
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
    console.log('displayResults called with:', { results: results.length, searchTerm });
    console.log('currentSearchTerm in displayResults:', currentSearchTerm);
    
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

// Enum conversion helpers
const MusicalKeys = {
    0: 'Not Set',
    1: 'C', 2: 'C#', 3: 'D', 4: 'D#', 5: 'E', 6: 'F', 7: 'F#', 8: 'G', 9: 'G#', 10: 'A', 11: 'A#', 12: 'B',
    13: 'C♭', 14: 'D♭', 15: 'E♭', 16: 'F♭', 17: 'G♭', 18: 'A♭', 19: 'B♭'
};

const ChorusTypes = {
    0: 'Not Set',
    1: 'Traditional', 2: 'Contemporary', 3: 'Gospel', 4: 'Hymn', 5: 'Worship',
    6: 'Praise', 7: 'Spiritual', 8: 'Folk', 9: 'Classical', 10: 'Jazz',
    11: 'Pop', 12: 'Rock', 13: 'Country', 14: 'Blues', 15: 'Reggae',
    16: 'Latin', 17: 'African', 18: 'Asian', 19: 'European', 20: 'Other'
};

const TimeSignatures = {
    0: 'Not Set',
    1: '4/4', 2: '3/4', 3: '6/8', 4: '2/4', 5: '4/8', 6: '3/8', 7: '2/2',
    8: '5/4', 9: '6/4', 10: '9/8', 11: '12/8', 12: '7/4', 13: '8/4',
    14: '5/8', 15: '7/8', 16: '8/8', 17: '2/16', 18: '3/16', 19: '4/16',
    20: '5/16', 21: '6/16', 22: '7/16', 23: '8/16', 24: '9/16', 25: '12/16'
};

function getKeyDisplay(keyValue) {
    return MusicalKeys[keyValue] || 'Unknown';
}

function getTypeDisplay(typeValue) {
    return ChorusTypes[typeValue] || 'Unknown';
}

function getTimeSignatureDisplay(timeValue) {
    return TimeSignatures[timeValue] || 'Unknown';
}

// Create result row
function createResultRow(result, index) {
    console.log('Creating row for result:', result);
    console.log('Result name:', result.name);
    console.log('Result keys:', Object.keys(result));
    console.log('currentSearchTerm:', currentSearchTerm);
    
    const row = document.createElement('tr');
    row.className = 'result-row';
    row.setAttribute('data-id', result.id);
    
    // Highlight search term in title
    const highlightedTitle = highlightSearchTerm(result.name, currentSearchTerm);
    console.log('Highlighted title:', highlightedTitle);
    console.log('Type of highlightedTitle:', typeof highlightedTitle);
    console.log('highlightedTitle === undefined:', highlightedTitle === undefined);
    console.log('highlightedTitle === null:', highlightedTitle === null);
    
    const rowHtml = `
        <td class="result-number">${index}</td>
        <td class="result-title">${highlightedTitle || result.name || 'Unknown'}</td>
        <td class="result-key">${getKeyDisplay(result.key)}</td>
        <td class="result-type">${getTypeDisplay(result.type)}</td>
        <td class="result-time">${getTimeSignatureDisplay(result.timeSignature)}</td>
        <td class="result-context">${utils.truncateText(result.chorusText, 80)}</td>
        <td class="result-actions">
            <button class="action-btn" onclick="openInNewWindow('${result.id}')" data-tooltip="Open in New Window">
                <i class="fas fa-external-link-alt"></i>
            </button>
            <button class="action-btn" onclick="showDetail('${result.id}')" data-tooltip="View Details">
                <i class="fas fa-eye"></i>
            </button>
            <button class="action-btn" onclick="copyChorusText('${result.id}')" data-tooltip="Copy Lyrics">
                <i class="fas fa-copy"></i>
            </button>
            <button class="action-btn action-btn-danger" onclick="showDeleteConfirmation('${result.id}', '${result.name.replace(/'/g, "\\'")}')" data-tooltip="Delete Chorus">
                <i class="fas fa-trash"></i>
            </button>
        </td>
    `;
    
    console.log('Row HTML:', rowHtml);
    row.innerHTML = rowHtml;
    
    // Add click handler for row
    row.addEventListener('click', function(e) {
        if (!e.target.closest('.action-btn')) {
            // Always open chorus display in new window
            const url = `/Home/ChorusDisplay/${result.id}`;
            const windowFeatures = 'width=1200,height=800,scrollbars=no,resizable=yes,menubar=no,toolbar=no,location=no,status=no';
            window.open(url, '_blank', windowFeatures);
        }
    });
    
    return row;
}

// Highlight search term in text
function highlightSearchTerm(text, searchTerm) {
    console.log('highlightSearchTerm called with:', { text, searchTerm });
    
    // Simple test first
    if (!text) {
        console.log('text is falsy, returning empty string');
        return '';
    }
    
    // Basic HTML escape
    const escapedText = text.replace(/&/g, '&amp;')
                           .replace(/</g, '&lt;')
                           .replace(/>/g, '&gt;')
                           .replace(/"/g, '&quot;')
                           .replace(/'/g, '&#39;');
    console.log('escapedText:', escapedText);
    
    if (!searchTerm) {
        console.log('searchTerm is falsy, returning escaped text');
        return escapedText;
    }
    
    // Escape regex special characters in the search term
    const escapedSearchTerm = searchTerm.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    console.log('escapedSearchTerm:', escapedSearchTerm);
    
    const regex = new RegExp(`(${escapedSearchTerm})`, 'gi');
    console.log('regex:', regex);
    
    const result = escapedText.replace(regex, '<mark>$1</mark>');
    console.log('final result:', result);
    
    return result;
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

// Open chorus display in new window
function openInNewWindow(chorusId) {
    const url = `/Home/ChorusDisplay/${chorusId}`;
    const windowFeatures = 'scrollbars=no,resizable=yes,menubar=no,toolbar=no,location=no,status=no,fullscreen=yes';
    const newWindow = window.open(url, '_blank', windowFeatures);
    
    // Fallback for browsers that don't support fullscreen in window.open
    if (newWindow) {
        newWindow.addEventListener('load', function() {
            try {
                if (newWindow.screen && newWindow.screen.availWidth) {
                    newWindow.moveTo(0, 0);
                    newWindow.resizeTo(newWindow.screen.availWidth, newWindow.screen.availHeight);
                }
            } catch (e) {
                console.log('Could not maximize window:', e);
            }
        });
    }
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
    
    // Initialize delete modal functionality
    initializeDeleteModal();
    
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

// Delete Modal Functions
let currentChorusId = null;
let currentChorusName = null;

// Show delete confirmation modal
function showDeleteConfirmation(chorusId, chorusName) {
    currentChorusId = chorusId;
    currentChorusName = chorusName;
    
    // Update the modal content
    document.getElementById('deleteChorusName').textContent = chorusName;
    
    // Show the modal
    const modal = document.getElementById('deleteModal');
    modal.classList.add('show');
    
    // Prevent body scroll
    document.body.style.overflow = 'hidden';
    
    // Focus on the cancel button for accessibility
    setTimeout(() => {
        const cancelButton = modal.querySelector('.btn-secondary');
        if (cancelButton) {
            cancelButton.focus();
        }
    }, 100);
}

// Hide delete confirmation modal
function hideDeleteModal() {
    const modal = document.getElementById('deleteModal');
    modal.classList.remove('show');
    
    // Restore body scroll
    document.body.style.overflow = '';
    
    // Clear current values
    currentChorusId = null;
    currentChorusName = null;
}

// Confirm delete action
async function confirmDelete() {
    if (!currentChorusId) {
        console.error('No chorus ID set for deletion');
        return;
    }
    
    try {
        // Show loading state
        const deleteButton = document.querySelector('.delete-modal-actions .btn-danger');
        const originalText = deleteButton.innerHTML;
        deleteButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deleting...';
        deleteButton.disabled = true;
        
        // Make API call
        const response = await fetch(`/Home/Delete/${currentChorusId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Show success message
            utils.showNotification('Chorus deleted successfully', 'success');
            
            // Hide modal
            hideDeleteModal();
            
            // Remove the row from the table
            const row = document.querySelector(`tr[data-id="${currentChorusId}"]`);
            if (row) {
                row.remove();
                
                // Update result count
                const resultsCount = document.getElementById('resultsCount');
                if (resultsCount) {
                    const currentCount = parseInt(resultsCount.textContent.match(/\d+/)[0]);
                    resultsCount.textContent = resultsCount.textContent.replace(/\d+/, currentCount - 1);
                }
                
                // Remove from searchResults array
                searchResults = searchResults.filter(r => r.id !== currentChorusId);
            }
        } else {
            // Show error message
            utils.showNotification(result.message || 'Failed to delete chorus', 'error');
            
            // Reset button
            deleteButton.innerHTML = originalText;
            deleteButton.disabled = false;
        }
    } catch (error) {
        console.error('Error deleting chorus:', error);
        utils.showNotification('An error occurred while deleting the chorus', 'error');
        
        // Reset button
        const deleteButton = document.querySelector('.delete-modal-actions .btn-danger');
        deleteButton.innerHTML = '<i class="fas fa-trash"></i> Delete Chorus';
        deleteButton.disabled = false;
    }
}

// Get anti-forgery token
function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

// Initialize delete modal functionality
function initializeDeleteModal() {
    // Close modal when clicking outside
    const modal = document.getElementById('deleteModal');
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                hideDeleteModal();
            }
        });
    }
    
    // Prevent modal from closing when clicking inside the modal
    const modalContent = document.querySelector('.delete-modal');
    if (modalContent) {
        modalContent.addEventListener('click', function(e) {
            e.stopPropagation();
        });
    }
    
    // Close modal with Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            const modal = document.getElementById('deleteModal');
            if (modal && modal.classList.contains('show')) {
                hideDeleteModal();
            }
        }
    });
}

// Add global delete functions
window.showDeleteConfirmation = showDeleteConfirmation;
window.hideDeleteModal = hideDeleteModal;
window.confirmDelete = confirmDelete; 