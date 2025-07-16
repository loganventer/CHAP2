// Chorus Display JavaScript
class ChorusDisplay {
    constructor() {
        this.currentChorusIndex = 0;
        this.choruses = [];
        this.currentFontSize = 1;
        this.minFontSize = 0.5;
        this.maxFontSize = 3;
        this.fontSizeStep = 0.1;
        
        this.init();
    }
    
    init() {
        this.loadChoruses();
        this.setupEventListeners();
        this.autoFitText();
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
        document.getElementById('zoomInBtn').addEventListener('click', () => this.zoomIn());
        document.getElementById('zoomOutBtn').addEventListener('click', () => this.zoomOut());
        document.getElementById('resetBtn').addEventListener('click', () => this.resetZoom());
        document.getElementById('printBtn').addEventListener('click', () => this.print());
        document.getElementById('closeBtn').addEventListener('click', () => this.close());
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => this.handleKeyboard(e));
        
        // Window resize
        window.addEventListener('resize', () => this.autoFitText());
        
        // Mouse wheel for zoom
        document.addEventListener('wheel', (e) => {
            if (e.ctrlKey || e.metaKey) {
                e.preventDefault();
                if (e.deltaY < 0) {
                    this.zoomIn();
                } else {
                    this.zoomOut();
                }
            }
        });
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
            case '+':
            case '=':
                e.preventDefault();
                this.zoomIn();
                break;
            case '-':
                e.preventDefault();
                this.zoomOut();
                break;
            case '0':
                e.preventDefault();
                this.resetZoom();
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
        
        let newIndex = this.currentChorusIndex + direction;
        
        // Loop around
        if (newIndex < 0) {
            newIndex = this.choruses.length - 1;
        } else if (newIndex >= this.choruses.length) {
            newIndex = 0;
        }
        
        const chorus = this.choruses[newIndex];
        if (chorus) {
            this.currentChorusIndex = newIndex;
            this.showLoading();
            await this.loadChorus(chorus.id);
            this.autoFitText();
            this.updateNavigationButtons();
            this.hideLoading();
            this.showNotification(`Now viewing: ${chorus.name}`, 'info');
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
        
        const chorusText = document.getElementById('chorusText');
        const lines = chorusData.text.split('\n');
        
        chorusText.innerHTML = lines.map(line => {
            if (line.trim()) {
                return `<div class="text-line">${line}</div>`;
            } else {
                return '<div class="text-line empty-line"></div>';
            }
        }).join('');
    }
    
    updateNavigationButtons() {
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');
        
        if (this.choruses.length <= 1) {
            prevBtn.style.display = 'none';
            nextBtn.style.display = 'none';
        } else {
            prevBtn.style.display = 'flex';
            nextBtn.style.display = 'flex';
        }
    }
    
    autoFitText() {
        const container = document.querySelector('.chorus-content');
        const text = document.querySelector('.chorus-text');
        
        if (!container || !text) return;
        
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        
        // Reset font size to find the optimal size
        text.style.fontSize = '1rem';
        
        let fontSize = 1;
        const maxFontSize = Math.min(containerWidth / 20, containerHeight / 30); // Rough estimate
        
        // Binary search for optimal font size
        let min = 0.5;
        let max = maxFontSize;
        
        while (min <= max) {
            fontSize = (min + max) / 2;
            text.style.fontSize = `${fontSize}rem`;
            
            if (text.scrollHeight <= containerHeight && text.scrollWidth <= containerWidth) {
                min = fontSize + 0.1;
            } else {
                max = fontSize - 0.1;
            }
        }
        
        // Apply the found font size
        this.currentFontSize = Math.max(this.minFontSize, Math.min(this.maxFontSize, fontSize));
        text.style.fontSize = `${this.currentFontSize}rem`;
    }
    
    zoomIn() {
        this.currentFontSize = Math.min(this.maxFontSize, this.currentFontSize + this.fontSizeStep);
        this.applyFontSize();
    }
    
    zoomOut() {
        this.currentFontSize = Math.max(this.minFontSize, this.currentFontSize - this.fontSizeStep);
        this.applyFontSize();
    }
    
    resetZoom() {
        this.currentFontSize = 1;
        this.applyFontSize();
        this.autoFitText();
    }
    
    applyFontSize() {
        const text = document.querySelector('.chorus-text');
        if (text) {
            text.style.fontSize = `${this.currentFontSize}rem`;
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