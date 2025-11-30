// Setlist Management
class SetlistManager {
    constructor() {
        this.setlist = [];
        this.loadFromLocalStorage();
        this.initializeEventListeners();
    }

    // Load setlist from localStorage
    loadFromLocalStorage() {
        try {
            const saved = localStorage.getItem('chap2_setlist');
            if (saved) {
                this.setlist = JSON.parse(saved);
                console.log(`Loaded ${this.setlist.length} choruses from localStorage`);
            }
        } catch (e) {
            console.error('Error loading setlist from localStorage:', e);
            this.setlist = [];
        }
    }

    // Save setlist to localStorage
    saveToLocalStorage() {
        try {
            localStorage.setItem('chap2_setlist', JSON.stringify(this.setlist));
            console.log(`Saved ${this.setlist.length} choruses to localStorage`);
        } catch (e) {
            console.error('Error saving setlist to localStorage:', e);
        }
    }

    // Add chorus to setlist
    addChorus(chorus) {
        // Check if already in setlist
        if (this.setlist.some(c => c.id === chorus.id)) {
            this.showNotification('Chorus already in setlist', 'warning');
            return false;
        }

        this.setlist.push(chorus);
        this.saveToLocalStorage();
        this.refreshDisplay();
        this.showNotification(`Added "${chorus.name}" to setlist`, 'success');
        return true;
    }

    // Remove chorus from setlist
    removeChorus(chorusId) {
        const chorus = this.setlist.find(c => c.id === chorusId);
        this.setlist = this.setlist.filter(c => c.id !== chorusId);
        this.saveToLocalStorage();
        this.refreshDisplay();
        if (chorus) {
            this.showNotification(`Removed "${chorus.name}" from setlist`, 'info');
        }
    }

    // Move chorus up in the list
    moveUp(chorusId) {
        const index = this.setlist.findIndex(c => c.id === chorusId);
        if (index > 0) {
            [this.setlist[index], this.setlist[index - 1]] = [this.setlist[index - 1], this.setlist[index]];
            this.saveToLocalStorage();
            this.refreshDisplay();
        }
    }

    // Move chorus down in the list
    moveDown(chorusId) {
        const index = this.setlist.findIndex(c => c.id === chorusId);
        if (index >= 0 && index < this.setlist.length - 1) {
            [this.setlist[index], this.setlist[index + 1]] = [this.setlist[index + 1], this.setlist[index]];
            this.saveToLocalStorage();
            this.refreshDisplay();
        }
    }

    // Clear all choruses from setlist
    clearAll() {
        if (this.setlist.length === 0) {
            return;
        }

        if (confirm(`Are you sure you want to remove all ${this.setlist.length} choruses from your setlist?`)) {
            this.setlist = [];
            this.saveToLocalStorage();
            this.refreshDisplay();
            this.showNotification('Setlist cleared', 'info');
        }
    }

    // Launch the setlist
    launchSetlist() {
        if (this.setlist.length === 0) {
            this.showNotification('Setlist is empty', 'warning');
            return;
        }

        // Store setlist in sessionStorage for chorus navigation
        sessionStorage.setItem('chorusList', JSON.stringify(this.setlist));
        sessionStorage.setItem('currentChorusId', this.setlist[0].id);

        // Open first chorus in ChorusDisplay view
        window.open(`/Home/ChorusDisplay/${this.setlist[0].id}`, '_blank');
        this.showNotification('Launching setlist...', 'success');
    }

    // Refresh the setlist display
    refreshDisplay() {
        const setlistEmpty = document.getElementById('setlistEmpty');
        const setlistItems = document.getElementById('setlistItems');
        const setlistCount = document.getElementById('setlistCount');
        const launchBtn = document.getElementById('launchSetlistBtn');
        const saveBtn = document.getElementById('saveSetlistBtn');
        const clearBtn = document.getElementById('clearSetlistBtn');

        // Update count
        if (setlistCount) {
            setlistCount.textContent = `(${this.setlist.length} chorus${this.setlist.length !== 1 ? 'es' : ''})`;
        }

        // Show/hide buttons
        if (launchBtn) {
            launchBtn.style.display = this.setlist.length > 0 ? 'inline-block' : 'none';
        }
        if (saveBtn) {
            saveBtn.style.display = this.setlist.length > 0 ? 'inline-block' : 'none';
        }
        if (clearBtn) {
            clearBtn.style.display = this.setlist.length > 0 ? 'inline-block' : 'none';
        }

        // Show empty state or items
        if (this.setlist.length === 0) {
            if (setlistEmpty) setlistEmpty.style.display = 'block';
            if (setlistItems) setlistItems.style.display = 'none';
            return;
        }

        if (setlistEmpty) setlistEmpty.style.display = 'none';
        if (setlistItems) setlistItems.style.display = 'block';

        // Build setlist items HTML
        const html = this.setlist.map((chorus, index) => `
            <div class="setlist-item" data-chorus-id="${chorus.id}" draggable="true">
                <div class="setlist-item-drag">
                    <i class="fas fa-grip-vertical"></i>
                </div>
                <div class="setlist-item-number">${index + 1}</div>
                <div class="setlist-item-content">
                    <div class="setlist-item-title">${chorus.name}</div>
                    <div class="setlist-item-meta">
                        ${chorus.key ? `<span><i class="fas fa-music"></i> ${chorus.key}</span>` : ''}
                        ${chorus.type ? `<span><i class="fas fa-tag"></i> ${chorus.type}</span>` : ''}
                        ${chorus.timeSignature ? `<span><i class="fas fa-clock"></i> ${chorus.timeSignature}</span>` : ''}
                    </div>
                </div>
                <div class="setlist-item-actions">
                    <button class="btn-icon" onclick="window.setlistManager.moveUp('${chorus.id}')" title="Move up" ${index === 0 ? 'disabled' : ''}>
                        <i class="fas fa-arrow-up"></i>
                    </button>
                    <button class="btn-icon" onclick="window.setlistManager.moveDown('${chorus.id}')" title="Move down" ${index === this.setlist.length - 1 ? 'disabled' : ''}>
                        <i class="fas fa-arrow-down"></i>
                    </button>
                    <button class="btn-icon" onclick="viewChorus('${chorus.id}')" title="View">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="btn-icon btn-danger" onclick="window.setlistManager.removeChorus('${chorus.id}')" title="Remove">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');

        if (setlistItems) {
            setlistItems.innerHTML = html;
        }

        // Initialize drag and drop
        this.initializeDragAndDrop();
    }

    // Initialize drag and drop
    initializeDragAndDrop() {
        const items = document.querySelectorAll('.setlist-item');
        let draggedItem = null;

        items.forEach(item => {
            item.addEventListener('dragstart', (e) => {
                draggedItem = item;
                item.classList.add('dragging');
                e.dataTransfer.effectAllowed = 'move';
            });

            item.addEventListener('dragend', (e) => {
                item.classList.remove('dragging');
                draggedItem = null;
            });

            item.addEventListener('dragover', (e) => {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';

                if (!draggedItem || draggedItem === item) return;

                const bounding = item.getBoundingClientRect();
                const offset = bounding.y + bounding.height / 2;

                if (e.clientY - offset > 0) {
                    item.style.borderBottom = '2px solid #2196F3';
                    item.style.borderTop = '';
                } else {
                    item.style.borderTop = '2px solid #2196F3';
                    item.style.borderBottom = '';
                }
            });

            item.addEventListener('dragleave', (e) => {
                item.style.borderTop = '';
                item.style.borderBottom = '';
            });

            item.addEventListener('drop', (e) => {
                e.preventDefault();
                item.style.borderTop = '';
                item.style.borderBottom = '';

                if (!draggedItem || draggedItem === item) return;

                const draggedId = draggedItem.getAttribute('data-chorus-id');
                const targetId = item.getAttribute('data-chorus-id');

                const draggedIndex = this.setlist.findIndex(c => c.id === draggedId);
                const targetIndex = this.setlist.findIndex(c => c.id === targetId);

                if (draggedIndex !== -1 && targetIndex !== -1) {
                    // Remove from old position
                    const [movedItem] = this.setlist.splice(draggedIndex, 1);
                    // Insert at new position
                    this.setlist.splice(targetIndex, 0, movedItem);

                    this.saveToLocalStorage();
                    this.refreshDisplay();
                }
            });
        });
    }

    // Initialize event listeners
    initializeEventListeners() {
        // Launch setlist button
        const launchBtn = document.getElementById('launchSetlistBtn');
        if (launchBtn) {
            launchBtn.addEventListener('click', () => this.launchSetlist());
        }

        // Save setlist button
        const saveBtn = document.getElementById('saveSetlistBtn');
        if (saveBtn) {
            saveBtn.addEventListener('click', () => this.exportSetlist());
        }

        // Load setlist button
        const loadBtn = document.getElementById('loadSetlistBtn');
        if (loadBtn) {
            loadBtn.addEventListener('click', () => this.importSetlist());
        }

        // Clear setlist button
        const clearBtn = document.getElementById('clearSetlistBtn');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => this.clearAll());
        }
    }

    // Export setlist to JSON file
    exportSetlist() {
        if (this.setlist.length === 0) {
            this.showNotification('Setlist is empty', 'warning');
            return;
        }

        const dataStr = JSON.stringify(this.setlist, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });

        const url = URL.createObjectURL(dataBlob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `setlist-${new Date().toISOString().slice(0, 10)}.json`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);

        this.showNotification('Setlist exported successfully', 'success');
    }

    // Import setlist from JSON file
    importSetlist() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';

        input.onchange = (e) => {
            const file = e.target.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = (event) => {
                try {
                    const importedSetlist = JSON.parse(event.target.result);

                    if (!Array.isArray(importedSetlist)) {
                        throw new Error('Invalid setlist format');
                    }

                    // Ask user if they want to replace or append
                    if (this.setlist.length > 0) {
                        if (confirm('Replace current setlist or append to it?\n\nOK = Replace\nCancel = Append')) {
                            this.setlist = importedSetlist;
                        } else {
                            // Append, avoiding duplicates
                            importedSetlist.forEach(chorus => {
                                if (!this.setlist.some(c => c.id === chorus.id)) {
                                    this.setlist.push(chorus);
                                }
                            });
                        }
                    } else {
                        this.setlist = importedSetlist;
                    }

                    this.saveToLocalStorage();
                    this.refreshDisplay();
                    this.showNotification(`Loaded ${importedSetlist.length} choruses`, 'success');
                } catch (error) {
                    console.error('Error importing setlist:', error);
                    this.showNotification('Error loading setlist file', 'error');
                }
            };

            reader.readAsText(file);
        };

        input.click();
    }

    // Show notification
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `setlist-notification setlist-notification-${type}`;
        notification.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'warning' ? 'exclamation-triangle' : type === 'error' ? 'times-circle' : 'info-circle'}"></i>
            <span>${message}</span>
        `;

        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'success' ? '#4CAF50' : type === 'warning' ? '#FF9800' : type === 'error' ? '#f44336' : '#2196F3'};
            color: white;
            padding: 15px 20px;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            z-index: 10000;
            display: flex;
            align-items: center;
            gap: 10px;
            animation: slideIn 0.3s ease-out;
        `;

        document.body.appendChild(notification);

        // Remove after 3 seconds
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => {
                if (notification.parentNode) {
                    document.body.removeChild(notification);
                }
            }, 300);
        }, 3000);
    }
}

// Global refresh function for tab switching
window.refreshSetlistDisplay = function() {
    if (window.setlistManager) {
        window.setlistManager.refreshDisplay();
    }
};

// Global function to add chorus to setlist from search results
window.addToSetlist = function(chorusId) {
    // Find the chorus in the current search results
    const chorus = window.currentChorusList?.find(c => c.id === chorusId);

    if (!chorus) {
        console.error('Chorus not found in current results');
        return;
    }

    if (window.setlistManager) {
        window.setlistManager.addChorus(chorus);
    } else {
        console.error('Setlist manager not initialized');
    }
};

// Initialize setlist manager when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.setlistManager = new SetlistManager();
    window.setlistManager.refreshDisplay();
    console.log('Setlist manager initialized');
});
