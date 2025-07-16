// Chorus Display JavaScript
class ChorusDisplay {
    constructor() {
        this.currentChorusIndex = 0;
        this.choruses = [];
        
        // Pagination settings
        this.maxDisplayLines = window.chorusDisplayConfig?.maxDisplayLines || 8; // Configurable maximum lines per page
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
        
        this.updateNavigationButtons();
    }
    
    async loadChoruses() {
        try {
            // Get all choruses for navigation
            const response = await fetch('/Home/Search?q=*');
            const data = await response.json();
            this.choruses = data.results || [];
            
            // Find current chorus index
            this.currentChorusIndex = this.choruses.findIndex(c => c.id === window.chorusData.id);
            if (this.currentChorusIndex === -1) {
                this.currentChorusIndex = 0;
            }
            
            this.updateNavigationButtons();
        } catch (error) {
            console.error('Error loading choruses:', error);
        }
    }
    
    setupEventListeners() {
        // Navigation buttons
        document.getElementById('prevBtn').addEventListener('click', () => this.navigate(-1));
        document.getElementById('nextBtn').addEventListener('click', () => this.navigate(1));
        
        // Control buttons
        document.getElementById('printBtn').addEventListener('click', () => this.print());
        document.getElementById('closeBtn').addEventListener('click', () => this.close());
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => this.handleKeyboard(e));
        
        // Window resize
        window.addEventListener('resize', () => this.updateDisplay());
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
        const keys = ['Not Set', 'C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];
        return keys[keyValue] || 'Not Set';
    }
    
    updateDisplay(chorusData) {
        document.getElementById('chorusTitle').textContent = chorusData.name;
        document.getElementById('chorusKey').textContent = chorusData.key;
        
        // Split text into lines and store for pagination
        this.currentChorusLines = chorusData.text.split('\n').filter(line => line.trim() !== '');
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
        
        // Update navigation buttons after displaying the page
        this.updateNavigationButtons();
    }
    
    updateNavigationButtons() {
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');
        
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
        const pageIndicator = document.getElementById('pageIndicator');
        if (this.totalPages > 1) {
            pageIndicator.style.display = 'block';
            pageIndicator.textContent = `Page ${this.currentPage + 1} of ${this.totalPages}`;
        } else {
            pageIndicator.style.display = 'none';
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
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new ChorusDisplay();
});

// Show loading state while initializing
document.body.classList.add('loading');

// Remove loading state after initialization
window.addEventListener('load', () => {
    document.body.classList.remove('loading');
}); 