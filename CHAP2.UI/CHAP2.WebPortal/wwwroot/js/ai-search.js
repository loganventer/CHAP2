// AI Search JavaScript with Enhanced Animations and Whizzbang Effects
class AiSearch {
    constructor() {
        // Guard against multiple initializations
        if (window.aiSearchInitialized) {
            console.log('AiSearch: Already initialized, skipping...');
            return;
        }
        
        this.searchInput = null;
        this.searchBtn = null;
        this.resultsContainer = null;
        this.resultsList = null;
        this.aiAnalysisContainer = null;
        this.analysisContent = null;
        this.statusContainer = null;
        this.statusText = null;
        this.searchInProgress = false;
        this.currentSearchController = null; // For cancelling ongoing requests
        this.messageInterval = null;
        this.currentMessageIndex = 0;
        this.initRetryCount = 0;
        this.maxInitRetries = 10;
        this.statusMessages = {
            thinking: [
                '🤖 Analyzing your query...',
                '🔍 Understanding search intent...',
                '💭 Generating search terms...',
                '📚 Searching vector database...',
                '🧠 Processing results...'
            ],
            success: [
                '✅ Search completed!',
                '🎉 Results ready!',
                '✨ Analysis complete!'
            ],
            error: [
                '❌ Search failed',
                '⚠️ Error occurred',
                '🔧 Please try again'
            ]
        };
        this.searchInProgress = false;
        this.currentSearchAbortController = null;
        this.individualResultsStreaming = false; // Track if individual results are being streamed
        this.receivedIndividualResults = new Set(); // Track which individual results we've received
        this.init();
    }
    
    init() {
        // Debug: Check what page we're on
        console.log('AiSearch: Current page URL:', window.location.pathname);
        console.log('AiSearch: Page title:', document.title);
        
        // Try to find elements with AI-specific IDs first, then fall back to generic IDs
        this.searchInput = document.getElementById('aiSearchInput') || document.getElementById('searchQuery');
        this.searchBtn = document.getElementById('aiSearchButton') || document.getElementById('aiSearchBtn') || document.getElementById('searchBtn');
        this.resultsContainer = document.getElementById('aiSearchResults') || document.getElementById('searchResults');
        this.resultsList = document.getElementById('aiResultsList') || document.getElementById('resultsList');
        this.aiAnalysisContainer = document.getElementById('aiAnalysis');
        this.analysisContent = document.getElementById('aiAnalysisContent') || document.getElementById('analysisContent');
        this.statusContainer = document.getElementById('status');
        this.statusText = document.getElementById('statusText');
        
        // Debug logging
        console.log('AiSearch init - Elements found:', {
            searchInput: !!this.searchInput,
            searchBtn: !!this.searchBtn,
            resultsContainer: !!this.resultsContainer,
            resultsList: !!this.resultsList,
            aiAnalysisContainer: !!this.aiAnalysisContainer,
            analysisContent: !!this.analysisContent,
            statusContainer: !!this.statusContainer,
            statusText: !!this.statusText
        });
        
        // Only initialize if we're on a page with AI search elements
        if (!this.searchInput || !this.searchBtn) {
            this.initRetryCount++;
            if (this.initRetryCount >= this.maxInitRetries) {
                console.log('AiSearch: Max retries reached, giving up initialization');
                return;
            }
            console.log(`AiSearch: No AI search elements found, retry ${this.initRetryCount}/${this.maxInitRetries}, waiting for container initialization...`);
            // Wait for the container to be initialized by search-integration.js
            setTimeout(() => this.init(), 500);
            return;
        }
        
        console.log('AiSearch: AI search elements found, setting up event listeners');
        
        // Add click event listener to search button
        if (this.searchBtn) {
            this.searchBtn.addEventListener('click', () => {
                console.log('AI Search button clicked');
                this.performSearch();
            });
        }
        
        // Add Enter key support to search input
        if (this.searchInput) {
            this.searchInput.addEventListener('keypress', (event) => {
                if (event.key === 'Enter') {
                    console.log('AI Search Enter key pressed');
                    event.preventDefault();
                    this.performSearch();
                }
            });
            
            // Add input event listener to cancel current search when new input is detected
            this.searchInput.addEventListener('input', (event) => {
                console.log('AI Search input detected, cancelling current search if any');
                this.cancelCurrentSearch();
                
                // Auto-resize functionality for textarea
                if (this.searchInput.tagName === 'TEXTAREA') {
                    this.autoResizeTextarea(this.searchInput);
                }
            });
            
            // Add keyup event listener for better auto-resize responsiveness
            this.searchInput.addEventListener('keyup', (event) => {
                // Auto-resize functionality for textarea
                if (this.searchInput.tagName === 'TEXTAREA') {
                    this.autoResizeTextarea(this.searchInput);
                }
            });
        }
        
        // Add Clear Filters button event listener
        const clearFiltersBtn = document.getElementById('aiClearFiltersBtn');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', () => {
                console.log('AI Clear Filters button clicked');
                this.clearFilters();
            });
        }
        
        // Add tab switching event listeners
        this.setupTabSwitching();
        
        this.createStatusIndicator();
        
        // Mark as initialized to prevent duplicate initialization
        window.aiSearchInitialized = true;
    }
    
    setupTabSwitching() {
        // Find tab buttons
        const aiTab = document.querySelector('.tab-button[data-tab="ai"]');
        const classicTab = document.querySelector('.tab-button[data-tab="regular"]');
        
        // Add event listeners to both tabs
        if (aiTab) {
            aiTab.addEventListener('click', () => {
                console.log('AI Search: AI tab clicked, cancelling current search');
                this.cancelCurrentSearch();
            });
        }
        
        if (classicTab) {
            classicTab.addEventListener('click', () => {
                console.log('AI Search: Classic tab clicked, cancelling current search');
                this.cancelCurrentSearch();
            });
        }
    }

    createStatusIndicator() {
        // Only create if search input exists
        if (!this.searchInput) {
            console.log('AiSearch: No search input found, skipping status indicator creation');
            return;
        }
        
        // Create status indicator
        const statusIndicator = document.createElement('div');
        statusIndicator.id = 'aiStatusIndicator';
        statusIndicator.className = 'ai-status-indicator ai-status-thinking';
        statusIndicator.innerHTML = `
            <div class="ai-status-content">
                <div class="ai-status-text">
                    <h3>AI is analyzing your query...</h3>
                    <p>Understanding your search intent and generating relevant terms</p>
                </div>
            </div>
        `;
        
        // Insert after the search input container
        const searchSection = this.searchInput.closest('.search-section');
        if (searchSection) {
            searchSection.appendChild(statusIndicator);
        } else {
            // Fallback: insert after the search input's parent
            const parent = this.searchInput.parentElement;
            if (parent) {
                parent.appendChild(statusIndicator);
            }
        }
        
        // Initially hidden
        statusIndicator.style.display = 'none';
    }

    updateAiStatus(message, type = 'thinking') {
        const statusIndicator = document.getElementById('aiStatusIndicator');
        if (!statusIndicator) return;

        statusIndicator.style.display = 'block';
        statusIndicator.className = `ai-status-indicator ai-status-${type}`;
        
        const statusText = statusIndicator.querySelector('.ai-status-text');
        if (statusText) {
            // Add timestamp for better tracking
            const timestamp = new Date().toLocaleTimeString();
            const messageWithTimestamp = `${message} (${timestamp})`;
            
            // Fade out current message
            statusText.style.opacity = '0';
            
            setTimeout(() => {
                statusText.innerHTML = `<h3>${messageWithTimestamp}</h3>`;
                statusText.style.opacity = '1';
                
                // Add subtle animation for thinking state
                if (type === 'thinking') {
                    statusIndicator.style.animation = 'pulse 2s infinite';
                } else {
                    statusIndicator.style.animation = '';
                }
            }, 150);
        }

        // Add retry button for timeout errors
        if (type === 'error' && (message.includes('timeout') || message.includes('timed out'))) {
            this.addRetryButton();
        } else {
            this.removeRetryButton();
        }

        // Start rotating messages only for thinking state
        if (type === 'thinking') {
            this.startRotatingMessages(type);
        } else {
            this.stopRotatingMessages();
        }
        
        console.log(`AI Search: Status updated - ${type}: ${message}`);
    }

    startRotatingMessages(type) {
        // Clear existing interval
        if (this.messageInterval) {
            clearInterval(this.messageInterval);
        }

        this.currentMessageIndex = 0;
        const messages = this.statusMessages[type] || this.statusMessages.thinking;
        
        // Show first message immediately
        this.updateStatusMessage(messages[0], type);
        
        // Rotate messages every 5 seconds
        this.messageInterval = setInterval(() => {
            this.currentMessageIndex = (this.currentMessageIndex + 1) % messages.length;
            this.updateStatusMessage(messages[this.currentMessageIndex], type);
        }, 5000);
    }

    updateStatusMessage(message, type) {
        const statusIndicator = document.getElementById('aiStatusIndicator');
        if (!statusIndicator) return;

        const statusText = statusIndicator.querySelector('.ai-status-text');
        if (statusText) {
            // Fade out
            statusText.style.opacity = '0';
            
            setTimeout(() => {
                // Update the message
                if (typeof message === 'string') {
                    statusText.innerHTML = `<h3>${message}</h3>`;
                } else {
                    statusText.textContent = message;
                }
                statusText.style.opacity = '1';
            }, 200);
        }
    }

    stopRotatingMessages() {
        if (this.messageInterval) {
            clearInterval(this.messageInterval);
            this.messageInterval = null;
        }
    }

    typeText(element, text, speed = 50) {
        return new Promise((resolve) => {
            element.textContent = '';
            let index = 0;
            
            const typeChar = () => {
                if (index < text.length) {
                    element.textContent += text.charAt(index);
                    index++;
                    setTimeout(typeChar, speed);
                } else {
                    resolve();
                }
            };
            
            typeChar();
        });
    }

    celebrateSuccess() {
        const statusIndicator = document.getElementById('aiStatusIndicator');
        if (!statusIndicator) return;

        // Add success animation
        statusIndicator.classList.add('ai-status-success');
        
        // Create ripple effect
        this.createRippleEffect(statusIndicator);
    }

    fadeOutResults() {
        if (this.resultsContainer) {
            this.resultsContainer.style.opacity = '0';
            setTimeout(() => {
                this.resultsContainer.style.display = 'none';
                this.resultsContainer.style.opacity = '1';
            }, 300);
        }
    }

    animateProgressSteps() {
        const steps = document.querySelectorAll('.step');
        steps.forEach((step, index) => {
            setTimeout(() => {
                step.classList.add('active');
            }, index * 500);
        });
    }

    displaySearchResultsWithAnimation(results) {
        console.log('AI Search: displaySearchResultsWithAnimation called with:', results);
        console.log('AI Search: resultsContainer exists:', !!this.resultsContainer);
        
        if (!this.resultsContainer) {
            console.error('AI Search: resultsContainer is null!');
            return;
        }

        // Preserve the query understanding section if it exists
        const queryUnderstandingSection = this.resultsContainer.querySelector('.query-understanding-section');
        console.log('AI Search: Found existing query understanding section:', !!queryUnderstandingSection);
        
        // Clear everything except the query understanding section
        this.resultsContainer.innerHTML = '';
        
        // Restore the query understanding section if it existed
        if (queryUnderstandingSection) {
            this.resultsContainer.appendChild(queryUnderstandingSection);
            console.log('AI Search: Restored query understanding section');
        }
        
        if (!results || results.length === 0) {
            console.log('AI Search: No results to display');
            this.resultsContainer.innerHTML = `
                <div class="no-results" style="text-align: center; padding: 2rem; color: #666;">
                    <i class="fas fa-search" style="font-size: 3rem; margin-bottom: 1rem; opacity: 0.5;"></i>
                    <h3>No Results Found</h3>
                    <p>Try a different search term or check your spelling.</p>
                </div>
            `;
        } else {
            console.log('AI Search: Displaying', results.length, 'results');
            // Create results header
            const header = document.createElement('div');
            header.className = 'results-header';
            header.style.cssText = 'margin-bottom: 1rem; padding: 1rem; background: #f8f9fa; border-radius: 8px; border: 1px solid #e0e0e0;';
            header.innerHTML = `
                <h4 style="margin: 0; color: #333;">
                    <i class="fas fa-robot"></i> AI Search Results (${results.length} found)
                </h4>
            `;
            this.resultsContainer.appendChild(header);
            
            // Create results list
            const resultsList = document.createElement('div');
            resultsList.className = 'results-list';
            resultsList.style.cssText = 'display: flex; flex-direction: column; gap: 0.5rem;';
            
            results.forEach((result, index) => {
                console.log('AI Search: Creating result row for:', result.name || 'Untitled');
                const resultElement = this.createAnimatedResultRow(result, index);
                resultsList.appendChild(resultElement);
            });
            
            this.resultsContainer.appendChild(resultsList);
        }

        this.resultsContainer.style.display = 'block';
        console.log('AI Search: Results container display set to block');
        
        // Also show the parent container if it exists
        const parentContainer = document.getElementById('aiResults');
        if (parentContainer) {
            parentContainer.style.display = 'block';
            console.log('AI Search: Parent container display set to block');
        }
        
        // Ensure the aiResults container is specifically visible
        this.ensureAiResultsVisible();
        
        this.animateResultRows();
    }

    createAnimatedResultRow(result, index) {
        console.log('AI Search: Creating result row with data:', result);
        
        const row = document.createElement('div');
        row.className = 'search-result-item';
        
        // Ensure we have a unique ID for each row
        const chorusId = result.id || result.Id || `unknown_${index}`;
        row.dataset.chorusId = chorusId;
        
        // Check if this ID already exists in the DOM
        const existingRow = document.querySelector(`[data-chorus-id="${chorusId}"]`);
        if (existingRow) {
            console.warn('AI Search: Duplicate chorus ID found:', chorusId, 'Using index-based ID instead');
            row.dataset.chorusId = `duplicate_${index}_${Date.now()}`;
        }
        row.style.cssText = `
            background: white;
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            padding: 1rem;
            margin-bottom: 0.5rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            transition: all 0.3s ease;
            animation: slideIn 0.5s ease-out;
            animation-delay: ${index * 0.1}s;
            animation-fill-mode: both;
        `;
        
        // Helper functions for enum conversion
        const getKeyDisplay = (keyValue) => {
            const keys = ['Not Set', 'C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B', 'C♭', 'D♭', 'E♭', 'F♭', 'G♭', 'A♭', 'B♭'];
            const numValue = parseInt(keyValue);
            return keys[numValue] || 'Unknown';
        };
        
        const getTypeDisplay = (typeValue) => {
            const types = ['Not Set', 'Praise', 'Worship'];
            const numValue = parseInt(typeValue);
            return types[numValue] || 'Unknown';
        };
        
        const getTimeSignatureDisplay = (timeValue) => {
            const times = ['Not Set', '4/4', '3/4', '6/8', '2/4', '4/8', '3/8', '2/2', '5/4', '6/4', '9/8', '12/8', '7/4', '8/4', '5/8', '7/8', '8/8', '2/16', '3/16', '4/16', '5/16', '6/16', '7/16', '8/16', '9/16', '12/16'];
            const numValue = parseInt(timeValue);
            return times[numValue] || 'Unknown';
        };
        
        // Extract metadata properties - handle both direct properties and nested metadata
        const metadata = result.metadata || {};
        const chorusName = result.name || metadata.name || 'Untitled Chorus';
        const chorusKey = result.key || metadata.key || result.Key;
        const chorusType = result.type || metadata.type || result.Type;
        const chorusTimeSignature = result.timeSignature || metadata.timeSignature || result.TimeSignature;
        
        console.log('AI Search: Extracted metadata:', { chorusName, chorusKey, chorusType, chorusTimeSignature });
        
        row.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div style="flex: 1;">
                    <h5 style="margin: 0 0 0.5rem 0; color: #333; font-size: 1.1rem;">
                        ${index + 1}. ${chorusName}
                    </h5>
                    <div style="display: flex; gap: 1rem; font-size: 0.9rem; color: #666;">
                        <span><i class="fas fa-music"></i> ${getKeyDisplay(chorusKey)}</span>
                        <span><i class="fas fa-tag"></i> ${getTypeDisplay(chorusType)}</span>
                        <span><i class="fas fa-clock"></i> ${getTimeSignatureDisplay(chorusTimeSignature)}</span>
                    </div>
                    <div style="margin-top: 0.5rem; font-style: italic; color: #666; font-size: 0.85rem; line-height: 1.3;">
                        ${result.explanation || ''}
                    </div>
                </div>
                <div style="display: flex; gap: 0.5rem; margin-left: 1rem;">
                    <button class="action-btn" onclick="openInNewWindow('${result.id}')" data-tooltip="Open in New Window" style="padding: 0.5rem; border: none; background: #007bff; color: white; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-external-link-alt"></i>
                    </button>
                    <button class="action-btn" onclick="showDetail('${result.id}')" data-tooltip="View Details" style="padding: 0.5rem; border: none; background: #28a745; color: white; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="action-btn" onclick="copyChorusText('${result.id}')" data-tooltip="Copy Lyrics" style="padding: 0.5rem; border: none; background: #ffc107; color: #212529; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-copy"></i>
                    </button>
                    <button class="action-btn action-btn-danger" onclick="showDeleteConfirmation('${result.id}', '${(chorusName).replace(/'/g, "\\'")}')" data-tooltip="Delete Chorus" style="padding: 0.5rem; border: none; background: #dc3545; color: white; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;
        
        // Add hover effect
        row.addEventListener('mouseenter', () => {
            row.style.transform = 'translateY(-2px)';
            row.style.boxShadow = '0 4px 8px rgba(0,0,0,0.15)';
        });
        
        row.addEventListener('mouseleave', () => {
            row.style.transform = 'translateY(0)';
            row.style.boxShadow = '0 2px 4px rgba(0,0,0,0.1)';
        });
        
        // Add click functionality to open chorus display (same as original search)
        row.addEventListener('click', (e) => {
            // Don't trigger row click if clicking on action buttons
            if (!e.target.closest('.action-btn')) {
                const chorusId = result.id || result.Id;
                if (chorusId) {
                    const url = `/Home/ChorusDisplay/${chorusId}`;
                    const windowFeatures = 'width=1200,height=800,scrollbars=no,resizable=yes,menubar=no,toolbar=no,location=no,status=no';
                    window.open(url, '_blank', windowFeatures);
                }
            }
        });
        
        // Add cursor pointer to indicate clickable
        row.style.cursor = 'pointer';
        
        return row;
    }

    displayQueryUnderstanding(queryUnderstanding) {
        console.log('AI Search: displayQueryUnderstanding called with:', queryUnderstanding);
        console.log('AI Search: resultsContainer exists:', !!this.resultsContainer);
        console.log('AI Search: resultsContainer display style:', this.resultsContainer?.style.display);
        
        if (!this.resultsContainer) {
            console.error('AI Search: resultsContainer is null in displayQueryUnderstanding');
            return;
        }
        
        // Create query understanding section
        const understandingSection = document.createElement('div');
        understandingSection.className = 'query-understanding-section';
        
        // Parse the query understanding into individual terms
        const terms = queryUnderstanding.split(',').map(term => term.trim());
        console.log('AI Search: Parsed terms:', terms);
        
        // Format the terms with better styling
        const formattedTerms = terms.map(term => 
            `<span>${term}</span>`
        ).join('');
        
        understandingSection.innerHTML = `
            <div class="query-understanding-header">
                <h3>
                    <i class="fas fa-brain"></i> <span>AI Query Understanding</span>
                </h3>
                <p>
                    Based on your search, I'm looking for choruses that match these terms:
                </p>
            </div>
            <div class="query-understanding-content">
                ${formattedTerms}
            </div>
        `;
        
        // Clear any existing query understanding section
        const existingSection = this.resultsContainer.querySelector('.query-understanding-section');
        if (existingSection) {
            existingSection.remove();
            console.log('AI Search: Removed existing query understanding section');
        }
        
        // Insert at the top of the results container
        this.resultsContainer.insertBefore(understandingSection, this.resultsContainer.firstChild);
        console.log('AI Search: Query understanding section added to DOM');
        
        // Ensure the results container is visible
        this.resultsContainer.style.display = 'block';
        console.log('AI Search: Results container display set to block');
        
        // Also show the parent container if it exists
        const parentContainer = document.getElementById('aiResults');
        if (parentContainer) {
            parentContainer.style.display = 'block';
            console.log('AI Search: Parent container display set to block');
        }
        
        // Ensure all AI search containers are visible
        this.ensureAiContainersVisible();
        
        // Ensure the aiResults container is specifically visible
        this.ensureAiResultsVisible();
        
        // Add sparkle effect
        this.addSparkleEffect(understandingSection);
        
        console.log('AI Search: displayQueryUnderstanding completed successfully');
    }

    updateResultsWithExplanations(results) {
        console.log('Updating results with explanations...');
        
        // Find all result rows and update their explanations
        const resultRows = this.resultsContainer.querySelectorAll('.search-result-item');
        
        resultRows.forEach((row, index) => {
            if (results[index] && results[index].explanation) {
                const explanationElement = row.querySelector('div[style*="font-style: italic"]');
                if (explanationElement) {
                    // Add a subtle animation to show the update
                    explanationElement.style.transition = 'all 0.5s ease';
                    explanationElement.style.background = 'rgba(255, 255, 0, 0.1)';
                    explanationElement.style.padding = '0.5rem';
                    explanationElement.style.borderRadius = '4px';
                    
                    setTimeout(() => {
                        explanationElement.innerHTML = results[index].explanation;
                        explanationElement.style.background = 'transparent';
                    }, 200);
                }
            }
        });
        
        // Update status to show explanations are complete
        this.updateAiStatus('✅ Explanations added to results!', 'success');
    }

    displaySearchResultsWithoutExplanations(results) {
        console.log('AI Search: displaySearchResultsWithoutExplanations called with:', results);
        if (!this.resultsContainer) {
            console.error('AI Search: resultsContainer is null in displaySearchResultsWithoutExplanations');
            return;
        }

        // Preserve the query understanding section if it exists
        const queryUnderstandingSection = this.resultsContainer.querySelector('.query-understanding-section');
        console.log('AI Search: Found existing query understanding section:', !!queryUnderstandingSection);
        
        // Clear everything except the query understanding section
        this.resultsContainer.innerHTML = '';
        
        // Restore the query understanding section if it existed
        if (queryUnderstandingSection) {
            this.resultsContainer.appendChild(queryUnderstandingSection);
            console.log('AI Search: Restored query understanding section');
        }
        
        if (!results || results.length === 0) {
            const noResultsDiv = document.createElement('div');
            noResultsDiv.innerHTML = `
                <div class="no-results" style="text-align: center; padding: 2rem; color: #666;">
                    <i class="fas fa-search" style="font-size: 3rem; margin-bottom: 1rem; opacity: 0.5;"></i>
                    <h3>No Results Found</h3>
                    <p>Try a different search term or check your spelling.</p>
                </div>
            `;
            this.resultsContainer.appendChild(noResultsDiv);
            console.log('AI Search: Added no results message');
        } else {
            console.log('AI Search: Displaying', results.length, 'results (without explanations)');
            // Create results header
            const header = document.createElement('div');
            header.className = 'results-header';
            header.style.cssText = 'margin-bottom: 1rem; padding: 1rem; background: #f8f9fa; border-radius: 8px; border: 1px solid #e0e0e0;';
            header.innerHTML = `
                <h4 style="margin: 0; color: #333;">
                    <i class="fas fa-robot"></i> AI Search Results (${results.length} found)
                </h4>
            `;
            this.resultsContainer.appendChild(header);
            
            // Create results list
            const resultsList = document.createElement('div');
            resultsList.className = 'results-list';
            resultsList.style.cssText = 'display: flex; flex-direction: column; gap: 0.5rem;';
            
            results.forEach((result, index) => {
                const chorusName = result.name || result.Name || 'Untitled Chorus';
                console.log('AI Search: Creating result row for:', chorusName);
                const resultElement = this.createResultRowWithoutExplanation(result, index);
                resultsList.appendChild(resultElement);
            });
            
            this.resultsContainer.appendChild(resultsList);
        }

        this.resultsContainer.style.display = 'block';
        console.log('AI Search: Results container display set to block');
        
        // Also show the parent container if it exists
        const parentContainer = document.getElementById('aiResults');
        if (parentContainer) {
            parentContainer.style.display = 'block';
            console.log('AI Search: Parent container display set to block');
        }
        
        this.animateResultRows();
    }

    createResultRowWithoutExplanation(result, index) {
        const row = document.createElement('div');
        row.className = 'search-result-item';
        row.style.cssText = `
            background: white;
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            padding: 1rem;
            margin-bottom: 0.5rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            transition: all 0.3s ease;
            animation: slideIn 0.5s ease-out;
            animation-delay: ${index * 0.1}s;
            animation-fill-mode: both;
        `;
        
        // Helper functions for enum conversion
        const getKeyDisplay = (keyValue) => {
            const keys = ['Not Set', 'C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B', 'C♭', 'D♭', 'E♭', 'F♭', 'G♭', 'A♭', 'B♭'];
            const numValue = parseInt(keyValue);
            return keys[numValue] || 'Unknown';
        };
        
        const getTypeDisplay = (typeValue) => {
            const types = ['Not Set', 'Praise', 'Worship'];
            const numValue = parseInt(typeValue);
            return types[numValue] || 'Unknown';
        };
        
        const getTimeSignatureDisplay = (timeValue) => {
            const times = ['Not Set', '4/4', '3/4', '6/8', '2/4', '4/8', '3/8', '2/2', '5/4', '6/4', '9/8', '12/8', '7/4', '8/4', '5/8', '7/8', '8/8', '2/16', '3/16', '4/16', '5/16', '6/16', '7/16', '8/16', '9/16', '12/16'];
            const numValue = parseInt(timeValue);
            return times[numValue] || 'Unknown';
        };
        
        row.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div style="flex: 1;">
                    <h5 style="margin: 0 0 0.5rem 0; color: #333; font-size: 1.1rem;">
                        ${index + 1}. ${result.name || result.Name || 'Untitled Chorus'}
                    </h5>
                    <div style="display: flex; gap: 1rem; font-size: 0.9rem; color: #666;">
                        <span><i class="fas fa-music"></i> ${getKeyDisplay(result.key || result.Key)}</span>
                        <span><i class="fas fa-tag"></i> ${getTypeDisplay(result.type || result.Type)}</span>
                        <span><i class="fas fa-clock"></i> ${getTimeSignatureDisplay(result.timeSignature || result.TimeSignature)}</span>
                    </div>
                    <div style="margin-top: 0.5rem; font-style: italic; color: #999; font-size: 0.85rem; line-height: 1.3;">
                        <i class="fas fa-spinner fa-spin"></i> Generating explanation...
                    </div>
                </div>
                <div style="display: flex; gap: 0.5rem; margin-left: 1rem;">
                    <button class="action-btn" onclick="openInNewWindow('${result.id || result.Id}')" data-tooltip="Open in New Window" style="padding: 0.5rem; border: none; background: #007bff; color: white; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-external-link-alt"></i>
                    </button>
                    <button class="action-btn" onclick="showDetail('${result.id || result.Id}')" data-tooltip="View Details" style="padding: 0.5rem; border: none; background: #28a745; color: white; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="action-btn" onclick="copyChorusText('${result.id || result.Id}')" data-tooltip="Copy Lyrics" style="padding: 0.5rem; border: none; background: #ffc107; color: #212529; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-copy"></i>
                    </button>
                    <button class="action-btn action-btn-danger" onclick="showDeleteConfirmation('${result.id || result.Id}', '${(result.name || result.Name || '').replace(/'/g, "\\'")}')" data-tooltip="Delete Chorus" style="padding: 0.5rem; border: none; background: #dc3545; color: white; border-radius: 4px; cursor: pointer;">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;
        
        // Add hover effect
        row.addEventListener('mouseenter', () => {
            row.style.transform = 'translateY(-2px)';
            row.style.boxShadow = '0 4px 8px rgba(0,0,0,0.15)';
        });
        
        row.addEventListener('mouseleave', () => {
            row.style.transform = 'translateY(0)';
            row.style.boxShadow = '0 2px 4px rgba(0,0,0,0.1)';
        });
        
        // Add click functionality to open chorus display (same as original search)
        row.addEventListener('click', (e) => {
            // Don't trigger row click if clicking on action buttons
            if (!e.target.closest('.action-btn')) {
                const chorusId = result.id || result.Id;
                if (chorusId) {
                    const url = `/Home/ChorusDisplay/${chorusId}`;
                    const windowFeatures = 'width=1200,height=800,scrollbars=no,resizable=yes,menubar=no,toolbar=no,location=no,status=no';
                    window.open(url, '_blank', windowFeatures);
                }
            }
        });
        
        // Add cursor pointer to indicate clickable
        row.style.cursor = 'pointer';
        
        return row;
    }

    addExplanationToResult(index, explanation) {
        const resultRows = this.resultsContainer.querySelectorAll('.search-result-item');
        if (resultRows[index]) {
            const explanationElement = resultRows[index].querySelector('div[style*="font-style: italic"]');
            if (explanationElement) {
                // Add a subtle animation to show the update
                explanationElement.style.transition = 'all 0.5s ease';
                explanationElement.style.background = 'rgba(255, 255, 0, 0.1)';
                explanationElement.style.padding = '0.5rem';
                explanationElement.style.borderRadius = '4px';
                explanationElement.style.color = '#666';
                
                setTimeout(() => {
                    explanationElement.innerHTML = explanation;
                    explanationElement.style.background = 'transparent';
                }, 200);
            }
        }
    }



    updateSingleExplanation(index, explanation) {
        const resultRows = this.resultsContainer.querySelectorAll('.search-result-item');
        if (resultRows[index]) {
            const explanationElement = resultRows[index].querySelector('div[style*="font-style: italic"]');
            if (explanationElement) {
                // Add a subtle animation to show the update
                explanationElement.style.transition = 'all 0.3s ease';
                explanationElement.style.background = 'rgba(255, 255, 0, 0.1)';
                explanationElement.style.padding = '0.3rem';
                explanationElement.style.borderRadius = '4px';
                
                setTimeout(() => {
                    explanationElement.innerHTML = explanation;
                    explanationElement.style.background = 'transparent';
                }, 100);
            }
        }
    }

    showChorusDetail(chorusId) {
        // Show loading state in modal
        const modal = document.getElementById('detailModal');
        const modalContent = document.getElementById('modalContent');
        const modalTitle = document.getElementById('modalTitle');
        
        if (!modal || !modalContent || !modalTitle) {
            console.error('Modal elements not found');
            return;
        }
        
        modalTitle.textContent = 'Loading Chorus Details...';
        modalContent.innerHTML = '<div style="text-align: center; padding: 2rem;"><i class="fas fa-spinner fa-spin"></i> Loading...</div>';
        modal.style.display = 'block';
        
        // Fetch chorus details
        fetch(`/Home/DetailPartial/${chorusId}`)
            .then(response => response.text())
            .then(html => {
                modalTitle.textContent = 'Chorus Details';
                modalContent.innerHTML = html;
            })
            .catch(error => {
                console.error('Error loading chorus details:', error);
                modalTitle.textContent = 'Error';
                modalContent.innerHTML = '<div style="text-align: center; padding: 2rem; color: #dc3545;"><i class="fas fa-exclamation-triangle"></i> Error loading chorus details</div>';
            });
    }

    animateResultRows() {
        const rows = document.querySelectorAll('.search-result-item');
        rows.forEach((row, index) => {
            setTimeout(() => {
                row.style.opacity = '1';
                row.style.transform = 'translateY(0)';
            }, index * 100);
        });
    }

    addSparkleEffect(element) {
        const sparkle = document.createElement('div');
        sparkle.className = 'sparkle';
        sparkle.innerHTML = '✨';
        sparkle.style.position = 'absolute';
        sparkle.style.right = '10px';
        sparkle.style.top = '50%';
        sparkle.style.transform = 'translateY(-50%)';
        sparkle.style.animation = 'sparkle 1s ease-in-out';
        
        element.style.position = 'relative';
        element.appendChild(sparkle);
        
        setTimeout(() => {
            sparkle.remove();
        }, 1000);
    }

    addWhizzbangEffects() {
        // Add ripple effect to search input
        this.searchInput.addEventListener('focus', () => {
            this.createRippleEffect(this.searchInput);
        });

        // Add ripple effect to search button
        this.searchBtn.addEventListener('click', () => {
            this.createRippleEffect(this.searchBtn);
        });
    }

    createRippleEffect(element) {
        const ripple = document.createElement('div');
        ripple.style.position = 'absolute';
        ripple.style.borderRadius = '50%';
        ripple.style.background = 'rgba(255, 255, 255, 0.3)';
        ripple.style.transform = 'scale(0)';
        ripple.style.animation = 'ripple 0.6s linear';
        ripple.style.pointerEvents = 'none';
        
        element.style.position = 'relative';
        element.style.overflow = 'hidden';
        element.appendChild(ripple);
        
        setTimeout(() => {
            ripple.remove();
        }, 600);
    }

    async performSearch() {
        console.log('AI Search: performSearch called');
        
        // Clear previous results
        this.fadeOutResults();
        
        // Reset streaming flags
        this.individualResultsStreaming = false;
        this.receivedIndividualResults.clear();
        
        // Show AI containers
        this.ensureAiContainersVisible();
        
        // Check if we're on the AI search tab
        const aiTab = document.querySelector('.tab-button[data-tab="ai"]');
        const regularTab = document.querySelector('.tab-button[data-tab="regular"]');
        
        console.log('AI Search: Tab detection - aiTab:', !!aiTab, 'regularTab:', !!regularTab);
        console.log('AI Search: aiTab active:', aiTab?.classList.contains('active'));
        console.log('AI Search: regularTab active:', regularTab?.classList.contains('active'));
        
        // If we're not on the AI tab, switch to it first
        if (aiTab && !aiTab.classList.contains('active')) {
            console.log('AI Search: Switching to AI tab before search');
            aiTab.click(); // This will trigger the tab switching logic
            // Wait a moment for the tab switch to complete
            await new Promise(resolve => setTimeout(resolve, 150));
        }
        
        // Cancel any existing search
        this.cancelCurrentSearch();
        
        this.searchInProgress = true;
        console.log('AI Search: Search in progress set to true');
        
        // Create new AbortController for this search
        this.currentSearchController = new AbortController();
        
        // Add a small delay to prevent double-triggering
        await new Promise(resolve => setTimeout(resolve, 100));
        
        const query = this.searchInput.value.trim();
        
        console.log('AI Search: Search query:', query);
        
        if (!query) {
            alert('Please enter a search query');
            this.searchInProgress = false;
            return;
        }

        // Validate query length
        if (query.length > 400) {
            alert('Search query is too long. Please keep it under 400 characters.');
            this.searchInProgress = false;
            return;
        }

        console.log('AI Search: Starting search process...');

        // Update button state (handle both complex and simple button structures)
        const btnText = this.searchBtn.querySelector('.btn-text');
        const btnLoading = this.searchBtn.querySelector('.btn-loading');
        
        if (btnText && btnLoading) {
            // Complex button structure (TraditionalSearchWithAi page)
            btnText.style.display = 'none';
            btnLoading.style.display = 'inline-block';
        } else {
            // Simple button structure (Index page) - just disable the button
            this.searchBtn.disabled = true;
            this.searchBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Searching...';
        }

        // Show AI status
        this.updateAiStatus('🤖 Initializing AI search...', 'thinking');
        console.log('AI Search: Status updated to initializing');
        
        // Ensure AI search containers are visible
        this.ensureAiContainersVisible();
        
        // Ensure the aiResults container is specifically visible for new searches
        this.ensureAiResultsVisible();

        try {
            // Get filter values
            const filters = this.getFilterValues();
            console.log('AI Search: Filters:', filters);
            
            // Build enhanced query
            const enhancedQuery = this.buildEnhancedQuery(query, filters);
            console.log('AI Search: Enhanced query:', enhancedQuery);

            // Use streaming endpoint for proper flow
            console.log('AI Search: Using streaming endpoint for proper flow');
            
            // Create a timeout promise (15 minutes)
            const timeoutPromise = new Promise((_, reject) => {
                setTimeout(() => {
                    reject(new Error('Request timed out after 15 minutes'));
                }, 15 * 60 * 1000); // 15 minutes timeout
            });
            
            // Create the fetch promise
            const fetchPromise = fetch('/Home/IntelligentSearchStream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    query: enhancedQuery,
                    maxResults: parseInt(document.getElementById('maxResults')?.value || '10')
                }),
                signal: this.currentSearchController.signal
            });
            
            // Race between fetch and timeout
            const response = await Promise.race([fetchPromise, timeoutPromise]);

            console.log('AI Search: Response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Handle streaming response
            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                
                const chunk = decoder.decode(value);
                const lines = chunk.split('\n');
                
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        try {
                            let jsonData = line.slice(6).trim();
                            if (!jsonData) continue; // Skip empty data lines
                            
                            // Skip lines that are just "data: " without any JSON
                            if (jsonData === '') continue;
                            
                            // Handle double "data: " prefix if present
                            if (jsonData.startsWith('data: ')) {
                                jsonData = jsonData.slice(6).trim();
                            }
                            
                            console.log('AI Search: Processing JSON data:', jsonData.substring(0, 100) + '...');
                            
                            let data;
                            try {
                                data = JSON.parse(jsonData);
                                console.log('AI Search: Received streaming data:', data);
                            } catch (parseError) {
                                console.error('AI Search: Error parsing JSON:', parseError);
                                console.error('AI Search: Raw JSON data:', jsonData);
                                continue; // Skip this line and continue with the next one
                            }
                            
                            // Check for duplicate IDs in search results
                            if (data.type === 'searchResults' && data.searchResults) {
                                const ids = data.searchResults.map(r => r.id || r.Id || '');
                                const uniqueIds = [...new Set(ids)];
                                if (ids.length !== uniqueIds.length) {
                                    console.warn('AI Search: Found duplicate IDs in search results, deduplicating...');
                                    // Remove duplicates by keeping only the first occurrence
                                    const seen = new Set();
                                    data.searchResults = data.searchResults.filter(result => {
                                        const id = result.id || result.Id || '';
                                        if (seen.has(id)) {
                                            return false;
                                        }
                                        seen.add(id);
                                        return true;
                                    });
                                    console.log('AI Search: Deduplicated results, now have', data.searchResults.length, 'unique results');
                                }
                            }
                            
                            try {
                                switch (data.type) {
                                    case 'queryUnderstanding':
                                        console.log('AI Search: Displaying query understanding');
                                        this.displayQueryUnderstanding(data.queryUnderstanding);
                                        this.updateAiStatus('🔍 Understanding your search query...', 'thinking');
                                        break;
                                        
                                    case 'searchResult':
                                        console.log('AI Search: Received individual search result:', data.index);
                                        this.individualResultsStreaming = true;
                                        this.receivedIndividualResults.add(data.index);
                                        this.addSearchResult(data.index, data.searchResult);
                                        this.updateAiStatus(`📚 Found chorus ${data.index + 1}, analyzing...`, 'thinking');
                                        break;
                                        
                                    case 'searchResults':
                                        console.log('AI Search: Received complete search results array');
                                        // Only display if we haven't been streaming individual results
                                        if (!this.individualResultsStreaming) {
                                            this.displaySearchResultsWithAnimation(data.searchResults);
                                        } else {
                                            console.log('AI Search: Skipping searchResults display - individual results already streaming');
                                        }
                                        this.updateAiStatus(`📚 Found ${data.searchResults.length} choruses, analyzing why each matches...`, 'thinking');
                                        break;
                                        
                                    case 'chorusReason':
                                        console.log('AI Search: Received chorus reason:', data.chorusId);
                                        this.updateChorusReason(data.chorusId, data.reason);
                                        this.updateAiStatus('💭 Analyzing why each chorus matches...', 'thinking');
                                        break;
                                        
                                    case 'aiAnalysis':
                                        console.log('AI Search: Displaying AI analysis');
                                        this.displayAiAnalysis(data.analysis);
                                        this.updateAiStatus('✅ AI analysis complete!', 'success');
                                        break;
                                        
                                    case 'progress':
                                        console.log('AI Search: Progress update:', data.message);
                                        this.updateAiStatus(data.message, 'thinking');
                                        break;
                                        
                                    case 'ollamaCall':
                                        console.log('AI Search: Ollama service call:', data.service);
                                        this.updateAiStatus(`🤖 Calling ${data.service} service...`, 'thinking');
                                        break;
                                        
                                    case 'step':
                                        console.log('AI Search: Step update:', data.step, data.total);
                                        const progress = Math.round((data.step / data.total) * 100);
                                        this.updateAiStatus(`⚡ Step ${data.step}/${data.total} (${progress}%) - ${data.message}`, 'thinking');
                                        break;
                                        
                                    case 'complete':
                                        console.log('AI Search: Search completed');
                                        this.celebrateAndFadeStatus();
                                        break;
                                        
                                    case 'error':
                                        console.error('AI Search: Error from server:', data.error || data.message);
                                        const errorMessage = data.error || data.message || 'Unknown error occurred';
                                        if (errorMessage.includes('timeout') || errorMessage.includes('timed out')) {
                                            this.updateAiStatus('⏰ Request timed out. The AI is taking longer than expected. Please try again with a simpler query.', 'error');
                                        } else {
                                            this.updateAiStatus('❌ Search failed: ' + errorMessage, 'error');
                                        }
                                        break;
                                }
                            } catch (switchError) {
                                console.error('AI Search: Error processing streaming data:', switchError);
                                console.error('AI Search: Data type was:', data.type);
                                this.updateAiStatus('❌ Error processing search results. Please try again.', 'error');
                            }
                        } catch (parseError) {
                            console.error('AI Search: Error parsing streaming data:', parseError);
                            console.error('AI Search: Raw line was:', line);
                            console.error('AI Search: Extracted JSON was:', line.slice(6));
                        }
                    }
                }
            }

        } catch (error) {
            console.error('AI Search: Error during search:', error);
            
            if (error.name === 'AbortError') {
                console.log('AI Search: Search was cancelled');
                this.updateAiStatus('⏹️ Search cancelled', 'error');
            } else if (error.message.includes('timeout') || error.message.includes('timed out') || error.name === 'TimeoutError') {
                console.log('AI Search: Request timed out');
                this.updateAiStatus('⏰ Request timed out. The AI is taking longer than expected. Please try again with a simpler query.', 'error');
            } else {
                this.updateAiStatus('❌ Search failed: ' + error.message, 'error');
            }
        } finally {
            this.searchInProgress = false;
            this.resetButtonState();
            console.log('AI Search: Search completed, resetting state');
        }
    }

    getFilterValues() {
        const keyElement = document.getElementById('filterKey');
        const typeElement = document.getElementById('filterType');
        const timeSignatureElement = document.getElementById('filterTimeSignature');
        
        return {
            key: keyElement ? keyElement.value : null,
            type: typeElement ? typeElement.value : null,
            timeSignature: timeSignatureElement ? timeSignatureElement.value : null
        };
    }

    buildEnhancedQuery(query, filters) {
        let enhancedQuery = query;
        
        if (filters.key) {
            const keyNames = {
                '1': 'C', '2': 'C#', '3': 'D', '4': 'D#', '5': 'E', '6': 'F', '7': 'F#', '8': 'G', '9': 'G#', '10': 'A', '11': 'A#', '12': 'B',
                '13': 'C♭', '14': 'D♭', '15': 'E♭', '16': 'F♭', '17': 'G♭', '18': 'A♭', '19': 'B♭'
            };
            enhancedQuery += ` key:${keyNames[filters.key]}`;
        }
        
        if (filters.type) {
            const typeNames = { '1': 'Praise', '2': 'Worship' };
            enhancedQuery += ` type:${typeNames[filters.type]}`;
        }
        
        if (filters.timeSignature) {
            const timeNames = {
                '1': '4/4', '2': '3/4', '3': '6/8', '4': '2/4', '5': '4/8', '6': '3/8', '7': '2/2',
                '8': '5/4', '9': '6/4', '10': '9/8', '11': '12/8', '12': '7/4', '13': '8/4',
                '14': '5/8', '15': '7/8', '16': '8/8'
            };
            enhancedQuery += ` time:${timeNames[filters.timeSignature]}`;
        }
        
        return enhancedQuery;
    }

    clearFilters() {
        document.getElementById('filterKey').value = '';
        document.getElementById('filterType').value = '';
        document.getElementById('filterTimeSignature').value = '';
        document.getElementById('maxResults').value = '10';
    }

    displayAiAnalysis(analysis) {
        if (!this.aiAnalysisContainer || !this.analysisContent) return;

        this.analysisContent.innerHTML = analysis;
        this.aiAnalysisContainer.style.display = 'block';
        
        // Add sparkle effect
        this.addSparkleEffect(this.aiAnalysisContainer);
    }

    celebrateAndFadeStatus() {
        const statusIndicator = document.getElementById('aiStatusIndicator');
        if (statusIndicator) {
            // Add celebration class
            statusIndicator.classList.add('celebrating');
            
            // Update status message
            this.updateAiStatus('🎉 Search completed successfully!', 'success');
            
            // Reset button state when search completes successfully
            this.resetButtonState();
            
            // Remove celebration class after animation
            setTimeout(() => {
                statusIndicator.classList.remove('celebrating');
                
                // Start gradual fade out after celebration
                setTimeout(() => {
                    statusIndicator.classList.add('fading');
                    setTimeout(() => {
                        statusIndicator.style.display = 'none';
                    }, 3000); // Match the CSS animation duration
                }, 3000); // Wait longer before starting fade
            }, 2000);
        }
    }

    cancelCurrentSearch() {
        console.log('AI Search: Cancelling current search...');
        
        // Cancel the current fetch request
        if (this.currentSearchController) {
            this.currentSearchController.abort();
            this.currentSearchController = null;
            console.log('AI Search: Current search aborted.');
        }
        
        // Reset search state
        this.searchInProgress = false;
        
        // Stop rotating messages
        this.stopRotatingMessages();
        
        // Hide status indicator
        const statusIndicator = document.getElementById('aiStatusIndicator');
        if (statusIndicator) {
            statusIndicator.style.display = 'none';
        }
        
        // Clear search results
        if (this.resultsContainer) {
            this.resultsContainer.innerHTML = '';
        }
        
        // Clear query understanding
        const queryUnderstandingContainer = document.getElementById('queryUnderstandingContainer');
        if (queryUnderstandingContainer) {
            queryUnderstandingContainer.innerHTML = '';
            queryUnderstandingContainer.style.display = 'none';
        }
        
        // Clear AI analysis
        if (this.aiAnalysisContainer) {
            this.aiAnalysisContainer.style.display = 'none';
        }
        
        // Reset button state
        this.resetButtonState();
    }
    
    resetButtonState() {
        if (this.searchBtn) {
            const btnText = this.searchBtn.querySelector('.btn-text');
            const btnLoading = this.searchBtn.querySelector('.btn-loading');
            
            if (btnText && btnLoading) {
                // Complex button structure
                btnText.style.display = 'inline-block';
                btnLoading.style.display = 'none';
            } else {
                // Simple button structure
                this.searchBtn.disabled = false;
                this.searchBtn.innerHTML = '<span class="btn-text">Search with AI Analysis</span>';
            }
        }
    }

    autoResizeTextarea(textarea) {
        // Reset height to auto to get the correct scrollHeight
        textarea.style.height = 'auto';
        
        // Set height to match the content height (no max limit)
        textarea.style.height = textarea.scrollHeight + 'px';
        
        // Keep overflow hidden since we're growing to fit content
        textarea.style.overflowY = 'hidden';
    }

    ensureAiContainersVisible() {
        console.log('AI Search: Ensuring AI containers are visible...');
        
        // Ensure the AI search input is visible
        const aiSearchInput = document.getElementById('aiSearchInput');
        if (aiSearchInput) {
            aiSearchInput.style.display = 'block';
            console.log('AI Search: AI search input made visible');
        }

        // Ensure the AI search button is visible
        const aiSearchBtn = document.getElementById('aiSearchButton');
        if (aiSearchBtn) {
            aiSearchBtn.style.display = 'inline-block';
            console.log('AI Search: AI search button made visible');
        }

        // Ensure the AI search results container is visible
        const aiSearchResults = document.getElementById('aiSearchResults');
        if (aiSearchResults) {
            aiSearchResults.style.display = 'block';
            console.log('AI Search: AI search results container made visible');
        }

        // Ensure the AI analysis container is visible
        const aiAnalysis = document.getElementById('aiAnalysis');
        if (aiAnalysis) {
            aiAnalysis.style.display = 'block';
            console.log('AI Search: AI analysis container made visible');
        }

        // Ensure the AI status indicator is visible
        const aiStatusIndicator = document.getElementById('aiStatusIndicator');
        if (aiStatusIndicator) {
            aiStatusIndicator.style.display = 'block';
            console.log('AI Search: AI status indicator made visible');
        }
        
        // Ensure the aiResults container (which contains the chorus list) is visible
        const aiResults = document.getElementById('aiResults');
        if (aiResults) {
            aiResults.style.display = 'block';
            console.log('AI Search: AI results container made visible');
        }
        
        console.log('AI Search: All AI containers visibility ensured');
    }

    ensureAiResultsVisible() {
        console.log('AI Search: Ensuring aiResults container is visible...');
        const aiResults = document.getElementById('aiResults');
        if (aiResults) {
            aiResults.style.display = 'block';
            console.log('AI Search: aiResults container made visible');
        }
    }

    addSearchResult(index, searchResult) {
        console.log('AI Search: Adding search result at index:', index);
        
        // Get the results container
        const resultsContainer = document.getElementById('aiResults');
        if (!resultsContainer) {
            console.error('AI Search: Results container not found');
            return;
        }
        
        // Create the result row
        const resultRow = this.createAnimatedResultRow(searchResult, index);
        
        // Add animation delay based on index
        resultRow.style.animationDelay = `${index * 0.1}s`;
        
        // Add to results container
        resultsContainer.appendChild(resultRow);
        
        // Trigger animation
        setTimeout(() => {
            resultRow.classList.add('animate-in');
        }, 50);
        
        // Add sparkle effect
        this.addSparkleEffect(resultRow);
        
        console.log('AI Search: Added search result for:', searchResult.name);
    }

    updateChorusReason(chorusId, reason) {
        console.log('AI Search: Updating chorus reason for ID:', chorusId, 'Reason:', reason);
        
        try {
            const resultRows = this.resultsContainer.querySelectorAll('.search-result-item');
            let found = false;
            
            resultRows.forEach((row, index) => {
                try {
                    const rowChorusId = row.dataset.chorusId;
                    if (rowChorusId === chorusId) {
                        console.log('AI Search: Found matching row for chorus ID:', chorusId);
                        found = true;
                        
                        // Find or create the reason element
                        let reasonElement = row.querySelector('.chorus-reason');
                        if (!reasonElement) {
                            reasonElement = document.createElement('div');
                            reasonElement.className = 'chorus-reason';
                            reasonElement.style.cssText = `
                                margin-top: 0.5rem;
                                padding: 0.5rem;
                                background: rgba(0, 123, 255, 0.1);
                                border-left: 3px solid #007bff;
                                border-radius: 4px;
                                font-style: italic;
                                color: #495057;
                                font-size: 0.85rem;
                                line-height: 1.4;
                                animation: fadeIn 0.5s ease-in;
                            `;
                            row.appendChild(reasonElement);
                        }
                        
                        // Update the reason text with animation
                        reasonElement.style.opacity = '0';
                        reasonElement.textContent = reason;
                        
                        // Fade in the updated reason
                        setTimeout(() => {
                            reasonElement.style.opacity = '1';
                            reasonElement.style.transition = 'opacity 0.3s ease-in';
                        }, 100);
                        
                        console.log('AI Search: Updated reason for chorus:', chorusId);
                    }
                } catch (rowError) {
                    console.error('AI Search: Error processing row', index, ':', rowError);
                }
            });
            
            if (!found) {
                console.warn('AI Search: No matching row found for chorus ID:', chorusId);
            }
        } catch (error) {
            console.error('AI Search: Error in updateChorusReason:', error);
        }
    }

    addRetryButton() {
        const statusIndicator = document.getElementById('aiStatusIndicator');
        if (!statusIndicator) return;

        // Remove existing retry button
        this.removeRetryButton();

        // Create retry button
        const retryButton = document.createElement('button');
        retryButton.className = 'retry-button';
        retryButton.innerHTML = '<i class="fas fa-redo"></i> Retry';
        retryButton.style.cssText = `
            background: rgba(255, 255, 255, 0.2);
            border: 1px solid rgba(255, 255, 255, 0.3);
            color: white;
            padding: 0.5rem 1rem;
            border-radius: 6px;
            cursor: pointer;
            margin-left: 1rem;
            font-size: 0.9rem;
            transition: all 0.3s ease;
        `;

        retryButton.addEventListener('click', () => {
            this.performSearch();
        });

        statusIndicator.appendChild(retryButton);
    }

    removeRetryButton() {
        const statusIndicator = document.getElementById('aiStatusIndicator');
        if (!statusIndicator) return;

        const retryButton = statusIndicator.querySelector('.retry-button');
        if (retryButton) {
            retryButton.remove();
        }
    }
}

// Initialize AI Search when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    // Only create if not already created
    if (!window.aiSearch) {
        window.aiSearch = new AiSearch();
    }
});

// Global function for button click
function performSearch() {
    if (window.aiSearch) {
        window.aiSearch.performSearch();
    }
}

function clearFilters() {
    if (window.aiSearch) {
        window.aiSearch.clearFilters();
    }
} 