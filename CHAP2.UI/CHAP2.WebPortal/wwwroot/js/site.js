// Global variables

// Event handlers
document.addEventListener('DOMContentLoaded', function() {
    // Initialize tooltips
    initializeTooltips();
    
    // Initialize keyboard shortcuts
    initializeKeyboardShortcuts();
    
    // Initialize accessibility features
    initializeAccessibility();
});

// Initialize tooltips
function initializeTooltips() {
    const tooltipElements = document.querySelectorAll('[data-tooltip]');
    
    tooltipElements.forEach(element => {
        element.addEventListener('mouseenter', showTooltip);
        element.addEventListener('mouseleave', hideTooltip);
    });
}

function showTooltip(event) {
    const element = event.target;
    const tooltipText = element.getAttribute('data-tooltip');
    
    const tooltip = document.createElement('div');
    tooltip.className = 'tooltip';
    tooltip.textContent = tooltipText;
    tooltip.id = 'tooltip-' + utils.generateId();
    
    document.body.appendChild(tooltip);
    
    const rect = element.getBoundingClientRect();
    tooltip.style.left = rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2) + 'px';
    tooltip.style.top = rect.top - tooltip.offsetHeight - 8 + 'px';
    
    setTimeout(() => {
        tooltip.classList.add('show');
    }, 100);
}

function hideTooltip(event) {
    const tooltips = document.querySelectorAll('.tooltip');
    tooltips.forEach(tooltip => {
        tooltip.classList.remove('show');
        setTimeout(() => {
            if (tooltip.parentNode) {
                tooltip.parentNode.removeChild(tooltip);
            }
        }, 200);
    });
}

// Initialize keyboard shortcuts
function initializeKeyboardShortcuts() {
    document.addEventListener('keydown', function(event) {
        // Ctrl/Cmd + K to focus search
        if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
            event.preventDefault();
            const searchInput = document.getElementById('searchInput');
            if (searchInput) {
                searchInput.focus();
            }
        }
        
        // Escape to close modals
        if (event.key === 'Escape') {
            const modal = document.getElementById('detailModal');
            if (modal && modal.style.display !== 'none') {
                closeModal();
            }
        }
        
        // Enter to trigger search (optional - search is now automatic)
        if (event.key === 'Enter' && event.target.id === 'searchInput') {
            event.preventDefault();
            const searchInput = event.target;
            if (searchInput.value.trim().length >= minSearchLength) {
                performSearch();
            }
        }
    });
}

// Initialize accessibility features
function initializeAccessibility() {
    // Add ARIA labels
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.setAttribute('aria-label', 'Search choruses by name, text, or musical key');
    }
    
    // Add keyboard navigation for results
    const resultsContainer = document.getElementById('searchResults');
    if (resultsContainer) {
        resultsContainer.setAttribute('role', 'listbox');
        resultsContainer.setAttribute('aria-label', 'Search results');
    }
}

// Modal functions
function openModal(chorusId) {
    const modal = document.getElementById('detailModal');
    if (modal) {
        modal.style.display = 'block';
        loadChorusDetails(chorusId);
    }
}

function closeModal() {
    const modal = document.getElementById('detailModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

async function loadChorusDetails(chorusId) {
    try {
        const response = await fetch(`/Home/GetChorus/${chorusId}`);
        if (response.ok) {
            const chorus = await response.json();
            displayChorusDetails(chorus);
        } else {
            utils.showNotification('Failed to load chorus details', 'error');
        }
    } catch (error) {
        console.error('Error loading chorus details:', error);
        utils.showNotification('Failed to load chorus details', 'error');
    }
}

function displayChorusDetails(chorus) {
    const modalContent = document.getElementById('modalContent');
    if (!modalContent) return;
    
    modalContent.innerHTML = `
        <div class="modal-header">
            <h2>${utils.escapeHtml(chorus.name)}</h2>
            <button onclick="closeModal()" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
            <div class="chorus-info">
                <div class="info-row">
                    <span class="label">Key:</span>
                    <span class="value">${chorus.key || 'Not Set'}</span>
                </div>
                <div class="info-row">
                    <span class="label">Type:</span>
                    <span class="value">${chorus.type || 'Not Set'}</span>
                </div>
                <div class="info-row">
                    <span class="label">Time Signature:</span>
                    <span class="value">${chorus.timeSignature || 'Not Set'}</span>
                </div>
            </div>
            <div class="chorus-text">
                <h3>Chorus Text</h3>
                <pre>${utils.escapeHtml(chorus.chorusText)}</pre>
            </div>
        </div>
        <div class="modal-footer">
            <button onclick="utils.copyToClipboard('${utils.escapeHtml(chorus.chorusText)}')" class="btn btn-secondary">
                <i class="fas fa-copy"></i> Copy Text
            </button>
            <button onclick="utils.downloadText('${utils.escapeHtml(chorus.chorusText)}', '${utils.escapeHtml(chorus.name)}.txt')" class="btn btn-primary">
                <i class="fas fa-download"></i> Download
            </button>
        </div>
    `;
}

// Global functions for external access
window.openModal = openModal;
window.closeModal = closeModal; 