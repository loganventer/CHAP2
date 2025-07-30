// Chorus Display JavaScript
class ChorusDisplay {
    constructor() {
        this.currentChorusIndex = 0;
        this.choruses = [];
        
        // Dynamic display settings
        this.currentFontSize = 'medium'; // small, medium, large, xlarge, xxlarge
        this.fontSizes = ['small', 'medium', 'large', 'xlarge', 'xxlarge'];
        this.maxDisplayLines = this.calculateMaxLines(); // Dynamic based on screen size
        this.currentPage = 0;
        this.totalPages = 0;
        this.currentChorusLines = [];
        
        this.init();
    }
    
    init() {
        this.loadChoruses();
        this.setupEventListeners();
        
        // Process initial chorus data if available
        if (window.chorusData) {
            this.updateDisplay(window.chorusData);
        }
        
        // Only update navigation buttons if we're on a chorus display page
        if (window.chorusData || window.location.pathname.includes('/Detail/')) {
            this.updateNavigationButtons();
        }
    }
    
    async loadChoruses() {
        try {
            // Check if we're on a chorus display page
            if (!window.chorusData) {
                console.log('Not on chorus display page, skipping chorus loading');
                return; // Not on a chorus display page, exit early
            }
            
            console.log('Loading choruses for display page');
            
            // Get all choruses for navigation
            const response = await fetch('/Home/Search?q=*');
            const data = await response.json();
            this.choruses = data.results || [];
            
            // Find current chorus index - add null check for window.chorusData
            if (window.chorusData && window.chorusData.id) {
                this.currentChorusIndex = this.choruses.findIndex(c => c && c.id === window.chorusData.id);
                if (this.currentChorusIndex === -1) {
                    this.currentChorusIndex = 0;
                }
            } else {
                this.currentChorusIndex = 0;
            }
            
            this.updateNavigationButtons();
        } catch (error) {
            console.error('Error loading choruses:', error);
        }
    }
    
    setupEventListeners() {
        // Navigation buttons
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');
        const printBtn = document.getElementById('printBtn');
        const closeBtn = document.getElementById('closeBtn');
        const increaseFontBtn = document.getElementById('increaseFontBtn');
        const decreaseFontBtn = document.getElementById('decreaseFontBtn');
        
        // Only add event listeners if the elements exist (they won't on the search page)
        if (prevBtn) prevBtn.addEventListener('click', () => this.navigate(-1));
        if (nextBtn) nextBtn.addEventListener('click', () => this.navigate(1));
        if (printBtn) printBtn.addEventListener('click', () => this.print());
        if (closeBtn) closeBtn.addEventListener('click', () => this.close());
        if (increaseFontBtn) increaseFontBtn.addEventListener('click', () => this.increaseFontSize());
        if (decreaseFontBtn) decreaseFontBtn.addEventListener('click', () => this.decreaseFontSize());
        
        // Keyboard shortcuts (only if we're on a chorus display page)
        if ((window.chorusData || window.location.pathname.includes('/Detail/')) && 
            (prevBtn || nextBtn || printBtn || closeBtn)) {
            document.addEventListener('keydown', (e) => this.handleKeyboard(e));
            
            // Window resize
            window.addEventListener('resize', () => this.handleResize());
        }
    }
    
    handleKeyboard(e) {
        switch (e.key) {
            case 'ArrowLeft':
                e.preventDefault();
                this.navigate(-1);
                break;
            case 'ArrowRight':
                e.preventDefault();
                this.navigate(1);
                break;
            case 'p':
                if (e.ctrlKey || e.metaKey) {
                    e.preventDefault();
                    this.print();
                }
                break;
            case 'Escape':
                this.close();
                break;
            case '+':
            case '=':
                e.preventDefault();
                this.increaseFontSize();
                break;
            case '-':
                e.preventDefault();
                this.decreaseFontSize();
                break;
        }
    }
    
    async navigate(direction) {
        if (this.choruses.length === 0) return;
        
        // Only navigate between pages of the current chorus
        if (this.totalPages > 1) {
            let newPage = this.currentPage + direction;
            
            // Loop around pages within the current chorus
            if (newPage < 0) {
                newPage = this.totalPages - 1;
            } else if (newPage >= this.totalPages) {
                newPage = 0;
            }
            
            this.currentPage = newPage;
            this.displayCurrentPage();
            this.updateNavigationButtons();
            this.showNotification(`Page ${this.currentPage + 1} of ${this.totalPages}`, 'info');
        } else {
            // If there's only one page, show a notification that there are no more pages
            this.showNotification('This chorus has only one page', 'info');
        }
    }
    
    async loadChorus(chorusId) {
        try {
            // First try to get the chorus data directly from the API
            const response = await fetch(`/Home/Search?q=*`);
            if (response.ok) {
                const data = await response.json();
                const chorus = data.results.find(c => c.id === chorusId);
                if (chorus) {
                    this.updateDisplay({
                        id: chorus.id,
                        name: chorus.name,
                        key: this.getKeyDisplay(chorus.key),
                        text: chorus.chorusText
                    });
                    return;
                }
            }
            
            // Fallback to loading the detail page
            const detailResponse = await fetch(`/Home/Detail/${chorusId}`);
            if (detailResponse.ok) {
                const html = await detailResponse.text();
                
                // Create a temporary div to parse the HTML
                const tempDiv = document.createElement('div');
                tempDiv.innerHTML = html;
                
                // Extract the chorus data
                const scriptTag = tempDiv.querySelector('script');
                if (scriptTag) {
                    const scriptContent = scriptTag.textContent;
                    const chorusDataMatch = scriptContent.match(/window\.chorusData\s*=\s*({[^}]+})/);
                    if (chorusDataMatch) {
                        try {
                            const chorusData = JSON.parse(chorusDataMatch[1].replace(/'/g, '"'));
                            this.updateDisplay(chorusData);
                        } catch (e) {
                            console.error('Error parsing chorus data:', e);
                        }
                    }
                }
            }
        } catch (error) {
            console.error('Error loading chorus:', error);
        }
    }
    
    getKeyDisplay(keyValue) {
        const keys = ['Not Set', 'C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B', 'C♭', 'D♭', 'E♭', 'F♭', 'G♭', 'A♭', 'B♭'];
        const numValue = parseInt(keyValue);
        return keys[numValue] || 'Not Set';
    }
    
    updateDisplay(chorusData) {
        const chorusTitle = document.getElementById('chorusTitle');
        const chorusKey = document.getElementById('chorusKey');
        
        // Check if we're on a chorus display page
        if (!chorusTitle || !chorusKey) {
            return; // Not on a chorus display page, exit early
        }
        
        chorusTitle.textContent = chorusData.name;
        chorusKey.textContent = chorusData.key;
        
        // Split text into lines and store for pagination
        this.currentChorusLines = chorusData.text.split('\n').filter(line => line.trim() !== '');
        this.maxDisplayLines = this.calculateMaxLines(); // Recalculate for current screen size
        this.totalPages = Math.ceil(this.currentChorusLines.length / this.maxDisplayLines);
        this.currentPage = 0;
        
        console.log(`Chorus: ${chorusData.name}`);
        console.log(`Total lines: ${this.currentChorusLines.length}`);
        console.log(`Max display lines: ${this.maxDisplayLines}`);
        console.log(`Total pages: ${this.totalPages}`);
        
        this.displayCurrentPage();
    }
    
    displayCurrentPage() {
        const chorusText = document.getElementById('chorusText');
        
        // Check if we're on a chorus display page
        if (!chorusText) {
            return; // Not on a chorus display page, exit early
        }
        
        const startIndex = this.currentPage * this.maxDisplayLines;
        const endIndex = Math.min(startIndex + this.maxDisplayLines, this.currentChorusLines.length);
        const pageLines = this.currentChorusLines.slice(startIndex, endIndex);
        
        console.log(`Displaying page ${this.currentPage + 1}:`);
        console.log(`Start index: ${startIndex}, End index: ${endIndex}`);
        console.log(`Lines to display: ${pageLines.length}`);
        console.log(`Page lines:`, pageLines);
        
        chorusText.innerHTML = pageLines.map(line => {
            return `<div class="text-line">${line}</div>`;
        }).join('');
        
        // Apply current font size
        this.applyFontSize();
        
        // Update navigation buttons after displaying the page
        this.updateNavigationButtons();
    }
    
    updateNavigationButtons() {
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');
        const pageIndicator = document.getElementById('pageIndicator');
        
        // If elements don't exist, return early
        if (!prevBtn || !nextBtn) {
            return;
        }
        
        // Show navigation buttons if there are multiple choruses or multiple pages
        const shouldShowNav = this.choruses.length > 1 || this.totalPages > 1;
        
        if (shouldShowNav) {
            prevBtn.style.display = 'flex';
            nextBtn.style.display = 'flex';
            
            // Update tooltips based on navigation state
            if (this.totalPages > 1) {
                prevBtn.title = 'Previous Page';
                nextBtn.title = 'Next Page';
            } else {
                // Hide navigation buttons if there's only one page
                prevBtn.style.display = 'none';
                nextBtn.style.display = 'none';
                return;
            }
        } else {
            prevBtn.style.display = 'none';
            nextBtn.style.display = 'none';
        }
        
        // Update page indicator
        if (pageIndicator) {
            if (this.totalPages > 1) {
                pageIndicator.style.display = 'block';
                pageIndicator.textContent = `Page ${this.currentPage + 1} of ${this.totalPages}`;
            } else {
                pageIndicator.style.display = 'none';
            }
        }
    }
    
    // Removed autoFitText method
    
    // Removed zoom-related methods
    
    print() {
        window.print();
    }
    
    showLoading() {
        document.body.classList.add('loading');
    }
    
    hideLoading() {
        document.body.classList.remove('loading');
    }
    
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-message">${message}</span>
                <button class="notification-close">&times;</button>
            </div>
        `;
        
        // Add to page
        document.body.appendChild(notification);
        
        // Auto-remove after 3 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 3000);
        
        // Close button
        const closeBtn = notification.querySelector('.notification-close');
        closeBtn.addEventListener('click', () => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        });
    }
    
    close() {
        // Try to close the window, fallback to going back
        if (window.opener) {
            window.close();
        } else {
            window.history.back();
        }
    }
    
    // Dynamic line calculation based on screen size
    calculateMaxLines() {
        const viewportHeight = window.innerHeight;
        const viewportWidth = window.innerWidth;
        
        // Calculate available space for text (subtract header, padding, controls)
        const headerHeight = 120; // Approximate header height
        const controlsHeight = 80; // Approximate controls height
        const padding = 80; // Total padding
        const availableHeight = viewportHeight - headerHeight - controlsHeight - padding;
        
        // Calculate line height based on current font size
        const lineHeights = {
            'small': 1.6,
            'medium': 1.8,
            'large': 2.0,
            'xlarge': 2.2,
            'xxlarge': 2.4
        };
        
        const fontSize = this.getFontSizeInPixels();
        const lineHeight = fontSize * lineHeights[this.currentFontSize];
        
        // Calculate max lines that can fit
        const maxLines = Math.floor(availableHeight / lineHeight);
        
        // Ensure minimum and maximum bounds
        const minLines = 3;
        const maxLinesBound = Math.max(minLines, Math.min(maxLines, 15));
        
        console.log(`Screen: ${viewportWidth}x${viewportHeight}, Available height: ${availableHeight}px`);
        console.log(`Font size: ${fontSize}px, Line height: ${lineHeight}px, Max lines: ${maxLinesBound}`);
        
        return maxLinesBound;
    }
    
    // Get current font size in pixels
    getFontSizeInPixels() {
        const fontSizeMap = {
            'small': 19.2, // 1.2rem
            'medium': 24,  // 1.5rem
            'large': 28.8, // 1.8rem
            'xlarge': 33.6, // 2.1rem
            'xxlarge': 38.4 // 2.4rem
        };
        return fontSizeMap[this.currentFontSize] || 24;
    }
    
    // Font size controls
    increaseFontSize() {
        const currentIndex = this.fontSizes.indexOf(this.currentFontSize);
        if (currentIndex < this.fontSizes.length - 1) {
            this.currentFontSize = this.fontSizes[currentIndex + 1];
            this.applyFontSize();
            this.recalculateAndRedisplay();
            this.showNotification(`Font size: ${this.currentFontSize}`, 'info');
        } else {
            this.showNotification('Maximum font size reached', 'warning');
        }
    }
    
    decreaseFontSize() {
        const currentIndex = this.fontSizes.indexOf(this.currentFontSize);
        if (currentIndex > 0) {
            this.currentFontSize = this.fontSizes[currentIndex - 1];
            this.applyFontSize();
            this.recalculateAndRedisplay();
            this.showNotification(`Font size: ${this.currentFontSize}`, 'info');
        } else {
            this.showNotification('Minimum font size reached', 'warning');
        }
    }
    
    // Apply font size to the text
    applyFontSize() {
        const chorusText = document.getElementById('chorusText');
        if (!chorusText) return;
        
        // Remove all font size classes
        chorusText.classList.remove('font-small', 'font-medium', 'font-large', 'font-xlarge', 'font-xxlarge');
        
        // Add current font size class
        chorusText.classList.add(`font-${this.currentFontSize}`);
    }
    
    // Handle window resize
    handleResize() {
        // Debounce resize events
        clearTimeout(this.resizeTimeout);
        this.resizeTimeout = setTimeout(() => {
            this.recalculateAndRedisplay();
        }, 250);
    }
    
    // Recalculate lines and redisplay
    recalculateAndRedisplay() {
        if (this.currentChorusLines.length > 0) {
            this.maxDisplayLines = this.calculateMaxLines();
            this.totalPages = Math.ceil(this.currentChorusLines.length / this.maxDisplayLines);
            
            // Adjust current page if it's now out of bounds
            if (this.currentPage >= this.totalPages) {
                this.currentPage = Math.max(0, this.totalPages - 1);
            }
            
            this.displayCurrentPage();
            this.updateNavigationButtons();
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize ChorusDisplay if we're on a chorus display page
    // Check if we have chorus data or if we're on a detail page
    if (window.chorusData || window.location.pathname.includes('/Detail/')) {
        new ChorusDisplay();
    }
});

// Show loading state while initializing (only on chorus display pages)
if (window.chorusData || window.location.pathname.includes('/Detail/')) {
    document.body.classList.add('loading');
}

// Remove loading state after initialization
window.addEventListener('load', () => {
    document.body.classList.remove('loading');
}); 