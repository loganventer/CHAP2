// AI Search JavaScript with Enhanced Animations and Whizzbang Effects
class AiSearch {
    constructor() {
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
            console.log('AiSearch: No AI search elements found, skipping initialization');
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
            statusText.textContent = message;
        }

        // Start rotating messages
        this.startRotatingMessages(type);
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
        
        this.animateResultRows();
    }

    createAnimatedResultRow(result, index) {
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
            return keys[keyValue] || 'Unknown';
        };
        
        const getTypeDisplay = (typeValue) => {
            const types = ['Not Set', 'Praise', 'Worship'];
            return types[typeValue] || 'Unknown';
        };
        
        const getTimeSignatureDisplay = (timeValue) => {
            const times = ['Not Set', '4/4', '3/4', '6/8', '2/4', '4/8', '3/8', '2/2', '5/4', '6/4', '9/8', '12/8', '7/4', '8/4', '5/8', '7/8', '8/8'];
            return times[timeValue] || 'Unknown';
        };
        
        row.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div style="flex: 1;">
                    <h5 style="margin: 0 0 0.5rem 0; color: #333; font-size: 1.1rem;">
                        ${index + 1}. ${result.name || 'Untitled Chorus'}
                    </h5>
                    <div style="display: flex; gap: 1rem; font-size: 0.9rem; color: #666;">
                        <span><i class="fas fa-music"></i> ${getKeyDisplay(result.key || result.Key)}</span>
                        <span><i class="fas fa-tag"></i> ${getTypeDisplay(result.type || result.Type)}</span>
                        <span><i class="fas fa-clock"></i> ${getTimeSignatureDisplay(result.timeSignature || result.TimeSignature)}</span>
                    </div>
                    <div style="margin-top: 0.5rem; font-style: italic; color: #666; font-size: 0.85rem; line-height: 1.3;">
                        ${result.explanation || 'This chorus was selected based on relevance to your search query.'}
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
                    <button class="action-btn action-btn-danger" onclick="showDeleteConfirmation('${result.id}', '${(result.name || '').replace(/'/g, "\\'")}')" data-tooltip="Delete Chorus" style="padding: 0.5rem; border: none; background: #dc3545; color: white; border-radius: 4px; cursor: pointer;">
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
                    <i class="fas fa-brain"></i> AI Query Understanding
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
        
        // Add sparkle effect
        this.addSparkleEffect(understandingSection);
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
            return keys[keyValue] || 'Unknown';
        };
        
        const getTypeDisplay = (typeValue) => {
            const types = ['Not Set', 'Praise', 'Worship'];
            return types[typeValue] || 'Unknown';
        };
        
        const getTimeSignatureDisplay = (timeValue) => {
            const times = ['Not Set', '4/4', '3/4', '6/8', '2/4', '4/8', '3/8', '2/2', '5/4', '6/4', '9/8', '12/8', '7/4', '8/4', '5/8', '7/8', '8/8'];
            return times[timeValue] || 'Unknown';
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

        try {
            // Get filter values
            const filters = this.getFilterValues();
            console.log('AI Search: Filters:', filters);
            
            // Build enhanced query
            const enhancedQuery = this.buildEnhancedQuery(query, filters);
            console.log('AI Search: Enhanced query:', enhancedQuery);

            // Use streaming endpoint for proper flow
            console.log('AI Search: Using streaming endpoint for proper flow');
            const response = await fetch('/Home/IntelligentSearchStream', {
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
                            const data = JSON.parse(line.slice(6));
                            console.log('AI Search: Received streaming data:', data);
                            
                            switch (data.type) {
                                case 'queryUnderstanding':
                                    console.log('AI Search: Displaying query understanding');
                                    this.displayQueryUnderstanding(data.queryUnderstanding);
                                    this.updateAiStatus('🔍 Understanding your search...', 'thinking');
                                    break;
                                    
                                case 'searchResults':
                                    console.log('AI Search: Displaying search results');
                                    this.displaySearchResultsWithAnimation(data.searchResults);
                                    this.updateAiStatus('📚 Found choruses, analyzing...', 'thinking');
                                    break;
                                    
                                case 'aiAnalysis':
                                    console.log('AI Search: Displaying AI analysis');
                                    this.displayAiAnalysis(data.analysis);
                                    this.updateAiStatus('✅ Analysis complete!', 'success');
                                    break;
                                    
                                case 'complete':
                                    console.log('AI Search: Search completed');
                                    this.celebrateAndFadeStatus();
                                    break;
                                    
                                case 'error':
                                    console.error('AI Search: Error from server:', data.error);
                                    this.updateAiStatus('❌ Search failed: ' + data.error, 'error');
                                    break;
                            }
                        } catch (parseError) {
                            console.error('AI Search: Error parsing streaming data:', parseError);
                        }
                    }
                }
            }

        } catch (error) {
            console.error('AI Search: Error during search:', error);
            
            if (error.name === 'AbortError') {
                console.log('AI Search: Search was cancelled');
                this.updateAiStatus('⏹️ Search cancelled', 'error');
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
        
        console.log('AI Search: All AI containers visibility ensured');
    }
}

// Initialize AI Search when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.aiSearch = new AiSearch();
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