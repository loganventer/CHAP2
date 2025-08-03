// Chorus Display JavaScript
class ChorusDisplay {
    constructor() {
        this.currentChorusIndex = 0;
        this.choruses = [];
        
        // Dynamic display settings
        this.currentFontSize = 24; // Start with 24px
        this.minFontSize = 12;
        this.maxFontSize = 72;
        this.fontSizeStep = 2;
        this.currentPage = 0;
        this.totalPages = 0;
        this.currentChorusLines = [];
        this.linesPerPage = 0;
        
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
        
        // Navigate between pages of the current chorus
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
        
        console.log(`Chorus: ${chorusData.name}`);
        console.log(`Total lines: ${this.currentChorusLines.length}`);
        
        // Auto-fit the text to fill the screen
        this.autoFitText();
    }
    
    // Auto-fit text to fill the screen optimally
    autoFitText() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        // Get available space
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        
        console.log(`Container size: ${containerWidth}x${containerHeight}`);
        
        // Start with a much larger font size to fill screen more aggressively
        this.currentFontSize = Math.min(containerHeight / 12, containerWidth / 20); // More aggressive starting point
        this.currentFontSize = Math.max(this.minFontSize, Math.min(this.maxFontSize, this.currentFontSize));
        
        // Apply font size and calculate lines per page
        this.applyFontSize();
        this.calculateLinesPerPage();
        
        // Adjust font size to fill screen optimally
        this.optimizeFontSize();
        
        // Display the first page
        this.currentPage = 0;
        this.displayCurrentPage();
        this.updateNavigationButtons();
        
        console.log(`Auto-fit complete. Font size: ${this.currentFontSize}px, Pages: ${this.totalPages}`);
    }
    
    // Calculate how many lines can fit on one page
    calculateLinesPerPage() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        const lineHeight = this.currentFontSize * 1.5; // 1.5 line height
        const padding = 40; // Account for padding
        
        // Calculate how many lines can fit vertically
        const maxLinesVertically = Math.floor((containerHeight - padding) / lineHeight);
        this.linesPerPage = Math.max(1, maxLinesVertically); // At least 1 line
        
        // Now calculate how many actual text lines will fit after wrapping
        this.calculateWrappedLines();
        
        console.log(`Font size: ${this.currentFontSize}px, Line height: ${lineHeight}px`);
        console.log(`Container width: ${containerWidth}px, Container height: ${containerHeight}px`);
        console.log(`Lines per page: ${this.linesPerPage}, Total pages: ${this.totalPages}`);
    }
    
    // Calculate how many actual lines will be displayed after wrapping
    calculateWrappedLines() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        const containerWidth = container.clientWidth - 40; // Account for padding
        const fontSize = this.currentFontSize;
        const lineHeight = fontSize * 1.5;
        
        // Create a temporary element to measure text wrapping
        const tempElement = document.createElement('div');
        tempElement.style.cssText = `
            position: absolute;
            top: -9999px;
            left: -9999px;
            width: ${containerWidth}px;
            font-size: ${fontSize}px;
            line-height: ${lineHeight}px;
            word-wrap: break-word;
            word-break: break-word;
            overflow-wrap: break-word;
            white-space: pre-wrap;
            font-family: 'Inter', sans-serif;
        `;
        document.body.appendChild(tempElement);
        
        let totalWrappedLines = 0;
        const wrappedLinesPerOriginalLine = [];
        
        // Calculate wrapped lines for each original line
        for (let i = 0; i < this.currentChorusLines.length; i++) {
            const line = this.currentChorusLines[i];
            tempElement.textContent = line;
            
            // Get the actual height of the wrapped text
            const wrappedHeight = tempElement.scrollHeight;
            const wrappedLines = Math.ceil(wrappedHeight / lineHeight);
            
            wrappedLinesPerOriginalLine.push(wrappedLines);
            totalWrappedLines += wrappedLines;
        }
        
        // Clean up
        document.body.removeChild(tempElement);
        
        // Store the wrapped lines data for pagination
        this.wrappedLinesPerOriginalLine = wrappedLinesPerOriginalLine;
        this.totalWrappedLines = totalWrappedLines;
        
        // Update total pages based on wrapped lines
        this.totalPages = Math.ceil(totalWrappedLines / this.linesPerPage);
        
        console.log(`Total original lines: ${this.currentChorusLines.length}`);
        console.log(`Total wrapped lines: ${totalWrappedLines}`);
        console.log(`Lines per page: ${this.linesPerPage}, Total pages: ${this.totalPages}`);
    }
    
    // Optimize font size to fill screen better
    optimizeFontSize() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        
        // Calculate how many lines we can fit
        const maxLines = Math.floor((containerHeight - 40) / (this.currentFontSize * 1.5));
        
        // Try to fit all text on one page if possible
        if (this.currentChorusLines.length <= maxLines) {
            // We can fit all text, maximize font size to fill screen
            this.maximizeFontSizeForSinglePage();
        } else {
            // Multiple pages needed, optimize for maximum readability while filling screen
            this.optimizeFontSizeForMultiplePages();
        }
    }
    
    // Maximize font size when all text fits on one page
    maximizeFontSizeForSinglePage() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        
        // Start with a very large font size and work down
        this.currentFontSize = Math.min(containerHeight / 8, containerWidth / 15); // Much larger starting point
        this.currentFontSize = Math.max(this.minFontSize, Math.min(this.maxFontSize, this.currentFontSize));
        
        // Apply and test
        this.applyFontSize();
        this.calculateLinesPerPage();
        
        // If we still fit, keep increasing until we don't
        while (this.currentFontSize < this.maxFontSize && this.totalPages <= 1) {
            this.currentFontSize += this.fontSizeStep;
            this.applyFontSize();
            this.calculateLinesPerPage();
            
            if (this.totalPages > 1) {
                // Too big, revert
                this.currentFontSize -= this.fontSizeStep;
                this.applyFontSize();
                this.calculateLinesPerPage();
                break;
            }
        }
        
        console.log(`Maximized font size for single page: ${this.currentFontSize}px`);
    }
    
    // Optimize font size for multiple pages while maximizing screen usage
    optimizeFontSizeForMultiplePages() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        
        // Calculate optimal lines per page (aim for 6-8 lines for readability)
        const targetLinesPerPage = Math.min(8, Math.max(6, Math.floor(this.currentChorusLines.length / 2)));
        
        // Calculate font size that would give us the target lines per page
        const targetFontSize = (containerHeight - 40) / (targetLinesPerPage * 1.5);
        
        // Start with the target font size
        this.currentFontSize = Math.max(this.minFontSize, Math.min(this.maxFontSize, targetFontSize));
        
        // Apply and test
        this.applyFontSize();
        this.calculateLinesPerPage();
        
        // Fine-tune: try to increase font size while maintaining good page distribution
        while (this.currentFontSize < this.maxFontSize) {
            const testFontSize = this.currentFontSize + this.fontSizeStep;
            const testLineHeight = testFontSize * 1.5;
            const testLinesPerPage = Math.floor((containerHeight - 40) / testLineHeight);
            
            // Only increase if we maintain reasonable page distribution
            if (testLinesPerPage >= 4 && testLinesPerPage <= 10) {
                this.currentFontSize = testFontSize;
                this.applyFontSize();
                this.calculateLinesPerPage();
            } else {
                break;
            }
        }
        
        console.log(`Optimized font size for multiple pages: ${this.currentFontSize}px, Lines per page: ${this.linesPerPage}`);
    }
    
    displayCurrentPage() {
        const chorusText = document.getElementById('chorusText');
        
        // Check if we're on a chorus display page
        if (!chorusText) {
            return; // Not on a chorus display page, exit early
        }
        
        // Calculate which original lines to show based on current page
        const startWrappedLine = this.currentPage * this.linesPerPage;
        const endWrappedLine = startWrappedLine + this.linesPerPage;
        
        // Find which original lines correspond to this page
        const linesToShow = this.getLinesForPage(startWrappedLine, endWrappedLine);
        
        console.log(`Displaying page ${this.currentPage + 1}:`);
        console.log(`Wrapped line range: ${startWrappedLine} to ${endWrappedLine}`);
        console.log(`Original lines to show: ${linesToShow.length}`);
        
        chorusText.innerHTML = linesToShow.map(line => {
            return `<div class="text-line">${line}</div>`;
        }).join('');
        
        // Apply current font size
        this.applyFontSize();
        
        // Update navigation buttons after displaying the page
        this.updateNavigationButtons();
    }
    
    // Get the original lines that should be displayed for a given wrapped line range
    getLinesForPage(startWrappedLine, endWrappedLine) {
        const container = document.querySelector('.chorus-content');
        if (!container) return [];
        
        const containerWidth = container.clientWidth - 40;
        const fontSize = this.currentFontSize;
        const lineHeight = fontSize * 1.5;
        
        // Create a temporary element to measure text wrapping
        const tempElement = document.createElement('div');
        tempElement.style.cssText = `
            position: absolute;
            top: -9999px;
            left: -9999px;
            width: ${containerWidth}px;
            font-size: ${fontSize}px;
            line-height: ${lineHeight}px;
            word-wrap: break-word;
            word-break: break-word;
            overflow-wrap: break-word;
            white-space: pre-wrap;
            font-family: 'Inter', sans-serif;
        `;
        document.body.appendChild(tempElement);
        
        let currentWrappedLine = 0;
        const linesToShow = [];
        
        // Find which original lines correspond to the requested wrapped line range
        for (let i = 0; i < this.currentChorusLines.length; i++) {
            const line = this.currentChorusLines[i];
            tempElement.textContent = line;
            
            const wrappedHeight = tempElement.scrollHeight;
            const wrappedLines = Math.ceil(wrappedHeight / lineHeight);
            
            // Check if this line's wrapped lines overlap with our target range
            const lineStartWrapped = currentWrappedLine;
            const lineEndWrapped = currentWrappedLine + wrappedLines;
            
            if (lineStartWrapped < endWrappedLine && lineEndWrapped > startWrappedLine) {
                linesToShow.push(line);
            }
            
            currentWrappedLine += wrappedLines;
            
            // If we've gone past our target range, we can stop
            if (currentWrappedLine >= endWrappedLine) {
                break;
            }
        }
        
        // Clean up
        document.body.removeChild(tempElement);
        
        return linesToShow;
    }
    
    updateNavigationButtons() {
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');
        const pageIndicator = document.getElementById('pageIndicator');
        
        // If elements don't exist, return early
        if (!prevBtn || !nextBtn) {
            return;
        }
        
        // Show navigation buttons if there are multiple pages
        const shouldShowNav = this.totalPages > 1;
        
        if (shouldShowNav) {
            prevBtn.style.display = 'flex';
            nextBtn.style.display = 'flex';
            
            // Update tooltips
            prevBtn.title = 'Previous Page (←)';
            nextBtn.title = 'Next Page (→)';
        } else {
            // Hide navigation buttons if there's only one page
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
    
    // Font size controls
    increaseFontSize() {
        if (this.currentFontSize < this.maxFontSize) {
            this.currentFontSize += this.fontSizeStep;
            this.applyFontSize();
            this.calculateLinesPerPage();
            
            // If we can fit more text on screen, try to increase further
            if (this.totalPages <= 1 && this.currentChorusLines.length > 1) {
                // Try to maximize font size while keeping everything on one page
                while (this.currentFontSize < this.maxFontSize && this.totalPages <= 1) {
                    this.currentFontSize += this.fontSizeStep;
                    this.applyFontSize();
                    this.calculateLinesPerPage();
                    
                    if (this.totalPages > 1) {
                        // Too big, revert
                        this.currentFontSize -= this.fontSizeStep;
                        this.applyFontSize();
                        this.calculateLinesPerPage();
                        break;
                    }
                }
            }
            
            this.displayCurrentPage();
            this.showNotification(`Font size: ${this.currentFontSize}px`, 'info');
        } else {
            this.showNotification('Maximum font size reached', 'warning');
        }
    }
    
    decreaseFontSize() {
        if (this.currentFontSize > this.minFontSize) {
            this.currentFontSize -= this.fontSizeStep;
            this.applyFontSize();
            this.calculateLinesPerPage();
            this.displayCurrentPage();
            this.showNotification(`Font size: ${this.currentFontSize}px`, 'info');
        } else {
            this.showNotification('Minimum font size reached', 'warning');
        }
    }
    
    // Apply font size to the text
    applyFontSize() {
        const chorusText = document.getElementById('chorusText');
        if (!chorusText) return;
        
        chorusText.style.fontSize = `${this.currentFontSize}px`;
        chorusText.style.lineHeight = `${this.currentFontSize * 1.5}px`;
    }
    
    // Handle window resize
    handleResize() {
        // Debounce resize events
        clearTimeout(this.resizeTimeout);
        this.resizeTimeout = setTimeout(() => {
            if (this.currentChorusLines.length > 0) {
                this.autoFitText();
            }
        }, 250);
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