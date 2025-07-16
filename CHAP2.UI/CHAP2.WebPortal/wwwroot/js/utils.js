// Utility functions for the CHAP2 Web Portal

const utils = {
    // Escape HTML to prevent XSS
    escapeHtml: function(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    // Truncate text to specified length
    truncateText: function(text, maxLength) {
        if (!text) return '';
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    },

    // Copy text to clipboard
    copyToClipboard: async function(text) {
        try {
            await navigator.clipboard.writeText(text);
            this.showNotification('Copied to clipboard!', 'success');
        } catch (err) {
            console.error('Failed to copy: ', err);
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
                if (notification.parentNode) {
                    document.body.removeChild(notification);
                }
            }, 300);
        }, 3000);
    },

    // Generate random ID
    generateId: function() {
        return Math.random().toString(36).substr(2, 9);
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

    // Debounce function
    debounce: function(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
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

// Make utils globally available
window.utils = utils; 