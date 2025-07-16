// Detail page functionality
document.addEventListener('DOMContentLoaded', function() {
    initializeDetailPage();
});

function initializeDetailPage() {
    // Add print functionality
    addPrintFunctionality();
    
    // Add copy functionality
    addCopyFunctionality();
    
    // Add keyboard shortcuts
    addKeyboardShortcuts();
    
    // Add animations
    addAnimations();
    
    // Add responsive behavior
    addResponsiveBehavior();
}

// Add print functionality
function addPrintFunctionality() {
    const printBtn = document.querySelector('button[onclick="window.print()"]');
    if (printBtn) {
        printBtn.addEventListener('click', function(e) {
            e.preventDefault();
            printChorus();
        });
    }
}

// Print chorus
function printChorus() {
    // Create a print-friendly version
    const printWindow = window.open('', '_blank');
    const chorusTitle = document.querySelector('.chorus-title')?.textContent || 'Chorus Details';
    const chorusText = document.querySelector('.lyrics-container')?.innerHTML || '';
    const metaItems = document.querySelectorAll('.meta-item');
    
    let metaHtml = '';
    metaItems.forEach(item => {
        const icon = item.querySelector('i')?.className || '';
        const text = item.textContent.trim();
        metaHtml += `<div class="meta-item"><i class="${icon}"></i> ${text}</div>`;
    });
    
    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>${chorusTitle} - CHAP2</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; }
                .header { text-align: center; margin-bottom: 30px; }
                .title { font-size: 24px; font-weight: bold; margin-bottom: 10px; }
                .meta { margin-bottom: 20px; }
                .meta-item { margin: 5px 0; }
                .lyrics { line-height: 1.8; }
                .lyrics-line { margin-bottom: 8px; }
                @media print {
                    body { margin: 0; }
                    .no-print { display: none; }
                }
            </style>
        </head>
        <body>
            <div class="header">
                <div class="title">${chorusTitle}</div>
                <div class="meta">${metaHtml}</div>
            </div>
            <div class="lyrics">${chorusText}</div>
        </body>
        </html>
    `);
    
    printWindow.document.close();
    printWindow.focus();
    
    // Wait for content to load then print
    setTimeout(() => {
        printWindow.print();
        printWindow.close();
    }, 500);
}

// Add copy functionality
function addCopyFunctionality() {
    // Add copy button to lyrics section
    const lyricsSection = document.querySelector('.lyrics-section');
    if (lyricsSection) {
        const copyBtn = document.createElement('button');
        copyBtn.className = 'btn-secondary copy-lyrics-btn';
        copyBtn.innerHTML = '<i class="fas fa-copy"></i> Copy Lyrics';
        copyBtn.onclick = copyLyrics;
        
        const header = lyricsSection.querySelector('h3');
        if (header) {
            header.appendChild(copyBtn);
        }
    }
}

// Copy lyrics to clipboard
async function copyLyrics() {
    const lyricsContainer = document.querySelector('.lyrics-container');
    if (lyricsContainer) {
        const lyricsText = lyricsContainer.textContent || lyricsContainer.innerText;
        await utils.copyToClipboard(lyricsText);
    }
}

// Add keyboard shortcuts
function addKeyboardShortcuts() {
    document.addEventListener('keydown', function(event) {
        // Ctrl/Cmd + P to print
        if ((event.ctrlKey || event.metaKey) && event.key === 'p') {
            event.preventDefault();
            printChorus();
        }
        
        // Ctrl/Cmd + C to copy lyrics
        if ((event.ctrlKey || event.metaKey) && event.key === 'c') {
            event.preventDefault();
            copyLyrics();
        }
        
        // Escape to close window
        if (event.key === 'Escape') {
            window.close();
        }
        
        // F11 for fullscreen
        if (event.key === 'F11') {
            event.preventDefault();
            toggleFullscreen();
        }
    });
}

// Toggle fullscreen
function toggleFullscreen() {
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen().catch(err => {
            console.log('Error attempting to enable fullscreen:', err);
        });
    } else {
        document.exitFullscreen();
    }
}

// Add animations
function addAnimations() {
    // Animate lyrics lines
    const lyricsLines = document.querySelectorAll('.lyrics-line');
    lyricsLines.forEach((line, index) => {
        line.style.animationDelay = `${index * 100}ms`;
        line.classList.add('fade-in');
    });
    
    // Animate meta items
    const metaItems = document.querySelectorAll('.meta-item');
    metaItems.forEach((item, index) => {
        item.style.animationDelay = `${index * 150}ms`;
        item.classList.add('slide-in');
    });
    
    // Animate floating notes
    const notes = document.querySelectorAll('.note');
    notes.forEach((note, index) => {
        note.style.animationDelay = `${index * 0.5}s`;
    });
}

// Add responsive behavior
function addResponsiveBehavior() {
    // Handle window resize
    window.addEventListener('resize', function() {
        adjustLayout();
    });
    
    // Initial layout adjustment
    adjustLayout();
}

// Adjust layout based on screen size
function adjustLayout() {
    const container = document.querySelector('.detail-container');
    const content = document.querySelector('.detail-content');
    
    if (window.innerWidth < 768) {
        // Mobile layout
        if (content) {
            content.style.gridTemplateColumns = '1fr';
        }
        
        // Adjust font sizes
        const title = document.querySelector('.chorus-title');
        if (title) {
            title.style.fontSize = 'var(--font-size-2xl)';
        }
    } else {
        // Desktop layout
        if (content) {
            content.style.gridTemplateColumns = '2fr 1fr';
        }
        
        // Reset font sizes
        const title = document.querySelector('.chorus-title');
        if (title) {
            title.style.fontSize = 'var(--font-size-4xl)';
        }
    }
}

// Add smooth scrolling
function addSmoothScrolling() {
    const lyricsContainer = document.querySelector('.lyrics-container');
    if (lyricsContainer) {
        lyricsContainer.style.scrollBehavior = 'smooth';
    }
}

// Add search highlight functionality
function highlightSearchTerm() {
    const urlParams = new URLSearchParams(window.location.search);
    const searchTerm = urlParams.get('highlight');
    
    if (searchTerm) {
        const lyricsLines = document.querySelectorAll('.lyrics-line');
        lyricsLines.forEach(line => {
            const text = line.textContent;
            const highlightedText = text.replace(
                new RegExp(searchTerm, 'gi'),
                match => `<mark>${match}</mark>`
            );
            line.innerHTML = highlightedText;
        });
    }
}

// Add zoom functionality
function addZoomFunctionality() {
    const lyricsContainer = document.querySelector('.lyrics-container');
    if (lyricsContainer) {
        let currentZoom = 1;
        
        // Add zoom controls
        const zoomControls = document.createElement('div');
        zoomControls.className = 'zoom-controls';
        zoomControls.innerHTML = `
            <button class="zoom-btn" onclick="zoomIn()" data-tooltip="Zoom In">
                <i class="fas fa-search-plus"></i>
            </button>
            <button class="zoom-btn" onclick="zoomOut()" data-tooltip="Zoom Out">
                <i class="fas fa-search-minus"></i>
            </button>
            <button class="zoom-btn" onclick="resetZoom()" data-tooltip="Reset Zoom">
                <i class="fas fa-undo"></i>
            </button>
        `;
        
        const cardHeader = document.querySelector('.card-header');
        if (cardHeader) {
            cardHeader.appendChild(zoomControls);
        }
    }
}

// Zoom functions
function zoomIn() {
    const lyricsContainer = document.querySelector('.lyrics-container');
    if (lyricsContainer) {
        const currentZoom = parseFloat(lyricsContainer.style.fontSize) || 1;
        const newZoom = Math.min(currentZoom + 0.1, 2);
        lyricsContainer.style.fontSize = `${newZoom}em`;
    }
}

function zoomOut() {
    const lyricsContainer = document.querySelector('.lyrics-container');
    if (lyricsContainer) {
        const currentZoom = parseFloat(lyricsContainer.style.fontSize) || 1;
        const newZoom = Math.max(currentZoom - 0.1, 0.5);
        lyricsContainer.style.fontSize = `${newZoom}em`;
    }
}

function resetZoom() {
    const lyricsContainer = document.querySelector('.lyrics-container');
    if (lyricsContainer) {
        lyricsContainer.style.fontSize = '1em';
    }
}

// Add theme toggle
function addThemeToggle() {
    const themeToggle = document.createElement('button');
    themeToggle.className = 'btn-secondary theme-toggle';
    themeToggle.innerHTML = '<i class="fas fa-moon"></i>';
    themeToggle.onclick = toggleTheme;
    
    const headerActions = document.querySelector('.header-actions');
    if (headerActions) {
        headerActions.appendChild(themeToggle);
    }
}

// Toggle theme
function toggleTheme() {
    const body = document.body;
    const isDark = body.classList.contains('dark-theme');
    
    if (isDark) {
        body.classList.remove('dark-theme');
        localStorage.setItem('theme', 'light');
    } else {
        body.classList.add('dark-theme');
        localStorage.setItem('theme', 'dark');
    }
}

// Load saved theme
function loadSavedTheme() {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
        document.body.classList.add('dark-theme');
    }
}

// Initialize additional features
function initializeAdditionalFeatures() {
    addSmoothScrolling();
    highlightSearchTerm();
    addZoomFunctionality();
    addThemeToggle();
    loadSavedTheme();
}

// Call additional initialization
document.addEventListener('DOMContentLoaded', function() {
    initializeAdditionalFeatures();
});

// Global functions
window.printChorus = printChorus;
window.copyLyrics = copyLyrics;
window.toggleFullscreen = toggleFullscreen;
window.zoomIn = zoomIn;
window.zoomOut = zoomOut;
window.resetZoom = resetZoom;
window.toggleTheme = toggleTheme; 