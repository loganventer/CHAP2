// Global variables
let searchTimeout;
let currentSearchTerm = '';
let searchDelay = 300; // milliseconds - match console app
let minSearchLength = 2; // minimum characters - match console app

// Utility functions
const utils = {
    // Debounce function
    debounce: function(func, wait) {
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(searchTimeout);
                func(...args);
            };
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(later, wait);
        };
    },

    // Show notification
    showNotification: function(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
                <span>${message}</span>
            </div>
        `;
        
        document.body.appendChild(notification);
        
        // Animate in
        setTimeout(() => {
            notification.classList.add('show');
        }, 100);
        
        // Remove after 3 seconds
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 300);
        }, 3000);
    },

    // Format date
    formatDate: function(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    },

    // Truncate text
    truncateText: function(text, maxLength = 100) {
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    },

    // Escape HTML
    escapeHtml: function(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    // Generate random ID
    generateId: function() {
        return Math.random().toString(36).substr(2, 9);
    },

    // Check if element is in viewport
    isInViewport: function(element) {
        const rect = element.getBoundingClientRect();
        return (
            rect.top >= 0 &&
            rect.left >= 0 &&
            rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
            rect.right <= (window.innerWidth || document.documentElement.clientWidth)
        );
    },

    // Smooth scroll to element
    scrollToElement: function(element, offset = 0) {
        const elementPosition = element.getBoundingClientRect().top;
        const offsetPosition = elementPosition + window.pageYOffset - offset;
        
        window.scrollTo({
            top: offsetPosition,
            behavior: 'smooth'
        });
    },

    // Copy text to clipboard
    copyToClipboard: async function(text) {
        try {
            await navigator.clipboard.writeText(text);
            this.showNotification('Copied to clipboard!', 'success');
        } catch (err) {
            console.error('Failed to copy text: ', err);
            this.showNotification('Failed to copy text', 'error');
        }
    },

    // Download text as file
    downloadText: function(text, filename) {
        const blob = new Blob([text], { type: 'text/plain' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
    },

    // Validate musical key
    isValidMusicalKey: function(key) {
        const validKeys = [
            'C', 'C#', 'Db', 'D', 'D#', 'Eb', 'E', 'F', 'F#', 'Gb', 
            'G', 'G#', 'Ab', 'A', 'A#', 'Bb', 'B', 'CM', 'Cm', 'DM', 
            'Dm', 'EM', 'Em', 'FM', 'Fm', 'GM', 'Gm', 'AM', 'Am', 
            'BM', 'Bm', 'CSharp', 'DSharp', 'ESharp', 'FSharp', 
            'GSharp', 'ASharp', 'BSharp', 'CFlat', 'DFlat', 'EFlat', 
            'GFlat', 'AFlat', 'BFlat', 'CSharpM', 'DSharpM', 'ESharpM', 
            'FSharpM', 'GSharpM', 'ASharpM', 'BSharpM', 'CFlatM', 
            'DFlatM', 'EFlatM', 'GFlatM', 'AFlatM', 'BFlatM', 'CSharpm', 
            'DSharpm', 'ESharpm', 'FSharpm', 'GSharpm', 'ASharpm', 
            'BSharpm', 'CFlatm', 'DFlatm', 'EFlatm', 'GFlatm', 'AFlatm', 
            'BFlatm', 'NotSet'
        ];
        return validKeys.includes(key);
    },

    // Get key variations
    getKeyVariations: function(key) {
        const variations = {
            'C#': ['CSharp', 'Db'],
            'Db': ['CSharp', 'Db'],
            'D#': ['DSharp', 'Eb'],
            'Eb': ['DSharp', 'Eb'],
            'F#': ['FSharp', 'Gb'],
            'Gb': ['FSharp', 'Gb'],
            'G#': ['GSharp', 'Ab'],
            'Ab': ['GSharp', 'Ab'],
            'A#': ['ASharp', 'Bb'],
            'Bb': ['ASharp', 'Bb']
        };
        return variations[key] || [key];
    }
};

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
                performSearch(searchInput.value.trim());
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
    const resultsTable = document.getElementById('resultsTable');
    if (resultsTable) {
        resultsTable.addEventListener('keydown', function(event) {
            const rows = this.querySelectorAll('.result-row');
            const currentRow = event.target.closest('.result-row');
            
            if (!currentRow) return;
            
            const currentIndex = Array.from(rows).indexOf(currentRow);
            
            switch (event.key) {
                case 'ArrowDown':
                    event.preventDefault();
                    if (currentIndex < rows.length - 1) {
                        rows[currentIndex + 1].focus();
                    }
                    break;
                case 'ArrowUp':
                    event.preventDefault();
                    if (currentIndex > 0) {
                        rows[currentIndex - 1].focus();
                    }
                    break;
                case 'Enter':
                    event.preventDefault();
                    const chorusId = currentRow.getAttribute('data-id');
                    if (chorusId) {
                        showDetail(chorusId);
                    }
                    break;
            }
        });
    }
} 