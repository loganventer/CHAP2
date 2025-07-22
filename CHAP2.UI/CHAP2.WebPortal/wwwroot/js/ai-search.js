// AI Search JavaScript
class AiSearch {
    constructor() {
        this.searchInput = document.getElementById('aiSearchInput');
        this.searchButton = document.getElementById('aiSearchButton');
        this.resultsContainer = document.getElementById('aiSearchResults');
        this.currentQuery = '';
        
        // Use the correct enum mappings that match the C# enums
        this.MusicalKeys = {
            0: 'Not Set',
            1: 'C', 2: 'C#', 3: 'D', 4: 'D#', 5: 'E', 6: 'F', 7: 'F#', 8: 'G', 9: 'G#', 10: 'A', 11: 'A#', 12: 'B',
            13: 'C♭', 14: 'D♭', 15: 'E♭', 16: 'F♭', 17: 'G♭', 18: 'A♭', 19: 'B♭'
        };
        
        this.ChorusTypes = {
            0: 'Not Set',
            1: 'Praise',
            2: 'Worship'
        };
        
        this.TimeSignatures = {
            0: 'Not Set',
            1: '4/4', 2: '3/4', 3: '6/8', 4: '2/4', 5: '4/8', 6: '3/8', 7: '2/2',
            8: '5/4', 9: '6/4', 10: '9/8', 11: '12/8', 12: '7/4', 13: '8/4',
            14: '5/8', 15: '7/8', 16: '8/8', 17: '2/16', 18: '3/16', 19: '4/16',
            20: '5/16', 21: '6/16', 22: '7/16', 23: '8/16', 24: '9/16', 25: '12/16'
        };
        
        this.init();
    }
    
    init() {
        this.setupEventListeners();
    }
    
    setupEventListeners() {
        // Search input events
        this.searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.performSearch();
            }
        });
        
        // Search button
        this.searchButton.addEventListener('click', () => {
            this.performSearch();
        });
        
        // Clear search
        const clearButton = document.getElementById('aiClearButton');
        if (clearButton) {
            clearButton.addEventListener('click', () => {
                this.clearSearch();
            });
        }
    }
    

    
    async performSearch() {
        const query = this.searchInput.value.trim();
        if (!query) {
            this.showNotification('Please enter a search term', 'warning');
            return;
        }
        
        this.currentQuery = query;
        this.showLoading();
        this.clearResults();
        
        try {
            // Use LLM to search and find relevant choruses
            await this.performLlmSearch(query);
            
        } catch (error) {
            console.error('Search error:', error);
            this.showNotification('Search failed. Please try again.', 'error');
        } finally {
            this.hideLoading();
        }
    }
    
    async performLlmSearch(query) {
        // Show progress in the results area
        this.resultsContainer.innerHTML = `
            <div class="ai-progress-container">
                <div class="ai-progress-header">
                    <h2><i class="fas fa-robot"></i> AI Search in Progress</h2>
                    <p>Analyzing your query and searching through choruses...</p>
                </div>
                <div class="progress-bar">
                    <div class="progress-fill"></div>
                </div>
                <div id="streamingContent" class="streaming-content"></div>
            </div>
        `;
        
        const streamingContent = document.getElementById('streamingContent');
        let currentState = '';
        let stateCount = 0;
        
        try {
            // Use LLM to search for choruses
            const response = await fetch('/Home/LlmSearch', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ query: query })
            });
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';
            
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                
                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop() || '';
                
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        try {
                            const data = JSON.parse(line.slice(6));
                            
                            if (data.type === 'choruses') {
                                // Display the actual choruses found by LLM
                                this.displayLlmResults(data.data, query);
                            } else if (data.type === 'chunk') {
                                // Show current state with animation and keep previous steps
                                const newState = data.data;
                                
                                // Update state if it's new
                                if (newState !== currentState) {
                                    currentState = newState;
                                    stateCount++;
                                    
                                    // Add new state to the list
                                    const newStateElement = document.createElement('div');
                                    newStateElement.className = 'ai-state current';
                                    newStateElement.innerHTML = `
                                        <span class="state-icon">${this.getStateIcon(stateCount)}</span>
                                        <span class="state-text">${newState}</span>
                                        <span class="state-animation">${this.getStateAnimation(stateCount)}</span>
                                    `;
                                    
                                    // Remove 'current' class from all previous states
                                    const allStates = streamingContent.querySelectorAll('.ai-state');
                                    allStates.forEach(state => {
                                        state.classList.remove('current');
                                    });
                                    
                                    // Add the new state
                                    streamingContent.appendChild(newStateElement);
                                    streamingContent.scrollTop = streamingContent.scrollHeight;
                                    
                                    // Add a small delay for visual effect
                                    await new Promise(resolve => setTimeout(resolve, 100));
                                }
                                
                            } else if (data.type === 'done') {
                                // Search complete
                                console.log('LLM search completed');
                                
                                // Remove 'current' class from all states
                                const allStates = streamingContent.querySelectorAll('.ai-state');
                                allStates.forEach(state => {
                                    state.classList.remove('current');
                                });
                                
                                // Add completion message
                                const completionElement = document.createElement('div');
                                completionElement.className = 'ai-state completed';
                                completionElement.innerHTML = `
                                    <span class="state-icon">✅</span>
                                    <span class="state-text">Search completed successfully!</span>
                                `;
                                streamingContent.appendChild(completionElement);
                                streamingContent.scrollTop = streamingContent.scrollHeight;
                            }
                        } catch (e) {
                            console.error('Error parsing streaming data:', e);
                        }
                    }
                }
            }
            
        } catch (error) {
            console.error('Error performing LLM search:', error);
            this.resultsContainer.innerHTML = `
                <div class="error-message">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>Sorry, I encountered an error while searching. Please try again.</p>
                    <p class="error-details">${error.message}</p>
                </div>
            `;
        }
    }
    
    getStateIcon(stateNumber) {
        const icons = ['🔍', '📊', '🤖', '💭', '📝', '✨'];
        return icons[stateNumber % icons.length] || '⚡';
    }
    
    getStateAnimation(stateNumber) {
        // Return empty string to remove spinners
        return '';
    }
    
    displayLlmResults(choruses, query) {
        if (!choruses || choruses.length === 0) {
            this.resultsContainer.innerHTML = `
                <div class="no-results">
                    <i class="fas fa-search"></i>
                    <h3>No Results Found</h3>
                    <p>The AI couldn't find any choruses matching your query: "${query}"</p>
                </div>
            `;
            return;
        }
        
        // Create table structure for LLM results with explanations
        const tableHtml = `
            <div class="results-header">
                <h2>AI Search Results (${choruses.length})</h2>
                <p>Found ${choruses.length} choruses using AI analysis</p>
                <div class="explanation-controls">
                    <button class="btn-secondary btn-sm" id="toggleAllExplanations">
                        <i class="fas fa-chevron-down"></i> Show All Explanations
                    </button>
                    <span class="explanation-count">${choruses.length} explanations available</span>
                </div>
            </div>
            <div class="results-table-container">
                <table class="results-table">
                    <thead>
                        <tr>
                            <th class="col-number">#</th>
                            <th class="col-title">Title</th>
                            <th class="col-key">Key</th>
                            <th class="col-type">Type</th>
                            <th class="col-time">Time</th>
                            <th class="col-context">Context</th>
                            <th class="col-actions">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${choruses.map((chorus, index) => this.createAiResultRow(chorus, index + 1)).join('')}
                    </tbody>
                </table>
            </div>
        `;
        
        this.resultsContainer.innerHTML = tableHtml;
        
        // Add click handlers for rows
        this.setupRowClickHandlers();
    }
    
    createRegularResultRow(chorus, index) {
        return `
            <tr class="result-row" data-id="${chorus.id}">
                <td class="col-number">${index}</td>
                <td class="col-title">
                    <strong>${this.highlightSearchTerm(chorus.name, this.currentQuery)}</strong>
                </td>
                <td class="col-key">${this.getKeyDisplay(chorus.key)}</td>
                <td class="col-type">${this.getTypeDisplay(chorus.type)}</td>
                <td class="col-time">${this.getTimeSignatureDisplay(chorus.timeSignature)}</td>
                <td class="col-context">${this.truncateText(chorus.chorusText, 100)}</td>
                <td class="col-actions">
                    <div class="action-buttons">
                        <button onclick="viewChorus('${chorus.id}')" class="btn-icon" title="View">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button onclick="editChorus('${chorus.id}')" class="btn-icon" title="Edit">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button onclick="shareChorus('${chorus.id}')" class="btn-icon" title="Share">
                            <i class="fas fa-share"></i>
                        </button>
                        <button onclick="deleteChorus('${chorus.id}')" class="btn-icon" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }
    
    showAiExplanation(explanation) {
        // Add AI explanation to the results
        const explanationHtml = `
            <div class="ai-explanation">
                <h3><i class="fas fa-robot"></i> AI Search Analysis</h3>
                <p>${explanation}</p>
            </div>
        `;
        
        // Insert at the top of results
        const resultsHeader = this.resultsContainer.querySelector('.results-header');
        if (resultsHeader) {
            resultsHeader.insertAdjacentHTML('afterend', explanationHtml);
        }
    }
    

    
    displaySearchResults(results) {
        if (!results || results.length === 0) {
            this.resultsContainer.innerHTML = `
                <div class="no-results">
                    <i class="fas fa-search"></i>
                    <h3>No Results Found</h3>
                    <p>Try a different search term or check your spelling.</p>
                </div>
            `;
            return;
        }
        
        // Create table structure like regular search
        const tableHtml = `
            <div class="results-header">
                <h2>AI Search Results (${results.length})</h2>
                <p>Found ${results.length} choruses matching your query using semantic search</p>
                <div class="explanation-controls">
                    <button class="btn-secondary btn-sm" id="toggleAllExplanations">
                        <i class="fas fa-chevron-down"></i> Show All Explanations
                    </button>
                    <span class="explanation-count">${results.length} explanations available</span>
                </div>
            </div>
            <div class="results-table-container">
                <table class="results-table">
                    <thead>
                        <tr>
                            <th class="col-number">#</th>
                            <th class="col-title">Title</th>
                            <th class="col-key">Key</th>
                            <th class="col-type">Type</th>
                            <th class="col-time">Time</th>
                            <th class="col-context">Context</th>
                            <th class="col-actions">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${results.map((result, index) => this.createAiResultRow(result, index + 1)).join('')}
                    </tbody>
                </table>
            </div>
        `;
        
        this.resultsContainer.innerHTML = tableHtml;
        
        // Add click handlers for rows
        this.setupRowClickHandlers();
    }
    
    createAiResultRow(chorus, index) {
        // Generate explanation for why it matched
        const explanation = this.generateMatchExplanation(chorus);
        
        // Create main result row
        const mainRow = `
            <tr class="result-row" data-id="${chorus.id}">
                <td class="col-number">${index}</td>
                <td class="col-title">
                    <strong>${this.highlightSearchTerm(chorus.name, this.currentQuery)}</strong>
                </td>
                <td class="col-key">${this.getKeyDisplay(chorus.key)}</td>
                <td class="col-type">${this.getTypeDisplay(chorus.type)}</td>
                <td class="col-time">${this.getTimeSignatureDisplay(chorus.timeSignature)}</td>
                <td class="col-context">${this.truncateText(chorus.chorusText, 100)}</td>
                <td class="col-actions">
                    <div class="action-buttons">
                        <button onclick="viewChorus('${chorus.id}')" class="btn-icon" title="View">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button onclick="editChorus('${chorus.id}')" class="btn-icon" title="Edit">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button onclick="shareChorus('${chorus.id}')" class="btn-icon" title="Share">
                            <i class="fas fa-share"></i>
                        </button>
                        <button onclick="deleteChorus('${chorus.id}')" class="btn-icon" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
            <tr class="explanation-row collapsed" data-explanation-for="${chorus.id}">
                <td colspan="7" class="explanation-cell">
                    <div class="explanation-content">
                        <i class="fas fa-info-circle explanation-icon"></i>
                        <div class="explanation-text">
                            <strong>Why this matched:</strong> ${explanation}
                        </div>
                    </div>
                </td>
            </tr>
        `;
        
        return mainRow;
    }
    
    generateMatchExplanation(chorus) {
        let explanation = '';
        
        // Use AI explanation if available, otherwise generate one
        if (chorus.aiExplanation) {
            explanation = `<span class="match-high">AI Analysis</span><br><small class="match-reasons">${chorus.aiExplanation}</small>`;
        } else {
            // Fallback to generated explanation
            explanation = `<span class="match-high">AI Selected</span>`;
            
            // Add specific reasons based on content
            const reasons = [];
            const query = this.currentQuery.toLowerCase();
            const text = chorus.chorusText.toLowerCase();
            const name = chorus.name.toLowerCase();
            
            // Check for semantic keyword matches
            const keywordGroups = {
                'god': ['god', 'lord', 'jesus', 'christ', 'savior', 'redeemer', 'almighty', 'heavenly', 'divine'],
                'praise': ['praise', 'worship', 'glory', 'hallelujah', 'amen', 'blessed', 'holy'],
                'love': ['love', 'loving', 'beloved', 'heart', 'care', 'kindness', 'mercy'],
                'grace': ['grace', 'amazing', 'wonderful', 'marvelous', 'blessing', 'gift'],
                'faith': ['faith', 'trust', 'believe', 'hope', 'faithful', 'trusting'],
                'music': ['sing', 'song', 'chorus', 'melody', 'music', 'voice', 'singing'],
                'prayer': ['pray', 'prayer', 'praying', 'supplication', 'intercession'],
                'greatness': ['great', 'greatness', 'mighty', 'powerful', 'strong', 'magnificent', 'awesome']
            };
            
            // Check query keywords against content
            for (const [category, keywords] of Object.entries(keywordGroups)) {
                const queryHasCategory = keywords.some(keyword => query.includes(keyword));
                const contentHasCategory = keywords.some(keyword => 
                    text.includes(keyword) || name.includes(keyword)
                );
                
                if (queryHasCategory && contentHasCategory) {
                    reasons.push(`matches ${category} theme`);
                }
            }
            
            // Check for direct word matches
            const queryWords = query.split(/\s+/);
            const contentWords = [...text.split(/\s+/), ...name.split(/\s+/)];
            
            const directMatches = queryWords.filter(queryWord => 
                contentWords.some(contentWord => 
                    contentWord.includes(queryWord) || queryWord.includes(contentWord)
                )
            );
            
            if (directMatches.length > 0) {
                reasons.push(`contains words: "${directMatches.slice(0, 3).join(', ')}"`);
            }
            
            // Add language detection for Afrikaans/English
            const afrikaansWords = ['vir', 'my', 'oor', 'se', 'grootheid', 'koortjies', 'vind'];
            const hasAfrikaans = afrikaansWords.some(word => query.includes(word));
            if (hasAfrikaans) {
                reasons.push('Afrikaans query detected');
            }
            
            if (reasons.length > 0) {
                explanation += `<br><small class="match-reasons">${reasons.join(', ')}</small>`;
            } else {
                explanation += `<br><small class="match-reasons">AI determined relevance based on content analysis</small>`;
            }
        }
        
        return explanation;
    }
    
    highlightSearchTerm(text, searchTerm) {
        if (!searchTerm || !text) return text;
        
        const regex = new RegExp(`(${searchTerm.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
        return text.replace(regex, '<mark>$1</mark>');
    }
    
    setupRowClickHandlers() {
        // Add click handlers for result rows
        const resultRows = document.querySelectorAll('#aiSearchResults .result-row');
        resultRows.forEach(row => {
            row.addEventListener('click', function(e) {
                if (!e.target.closest('.action-btn')) {
                    const chorusId = this.getAttribute('data-id');
                    const url = `/Home/ChorusDisplay/${chorusId}`;
                    const windowFeatures = 'width=1200,height=800,scrollbars=no,resizable=yes,menubar=no,toolbar=no,location=no,status=no';
                    window.open(url, '_blank', windowFeatures);
                }
            });
        });
        
        // Add click handlers for individual explanation toggles
        const explanationToggles = document.querySelectorAll('#aiSearchResults .explanation-toggle');
        explanationToggles.forEach(toggle => {
            toggle.addEventListener('click', (e) => {
                e.stopPropagation();
                const targetId = toggle.getAttribute('data-target');
                this.toggleExplanation(targetId);
            });
        });
        
        // Add click handler for main toggle all button
        const toggleAllBtn = document.getElementById('toggleAllExplanations');
        if (toggleAllBtn) {
            toggleAllBtn.addEventListener('click', () => {
                this.toggleAllExplanations();
            });
        }
    }
    
    toggleExplanation(chorusId) {
        const explanationRow = document.querySelector(`[data-explanation-for="${chorusId}"]`);
        const toggleBtn = document.querySelector(`[data-target="${chorusId}"]`);
        
        if (explanationRow && toggleBtn) {
            const isCollapsed = explanationRow.classList.contains('collapsed');
            
            if (isCollapsed) {
                explanationRow.classList.remove('collapsed');
                toggleBtn.innerHTML = '<i class="fas fa-chevron-up"></i>';
                toggleBtn.setAttribute('data-tooltip', 'Hide Explanation');
            } else {
                explanationRow.classList.add('collapsed');
                toggleBtn.innerHTML = '<i class="fas fa-chevron-down"></i>';
                toggleBtn.setAttribute('data-tooltip', 'Show Explanation');
            }
        }
    }
    
    toggleAllExplanations() {
        const explanationRows = document.querySelectorAll('#aiSearchResults .explanation-row');
        const toggleBtns = document.querySelectorAll('#aiSearchResults .explanation-toggle');
        const toggleAllBtn = document.getElementById('toggleAllExplanations');
        
        if (explanationRows.length === 0) return;
        
        // Check if all are collapsed
        const allCollapsed = Array.from(explanationRows).every(row => 
            row.classList.contains('collapsed')
        );
        
        if (allCollapsed) {
            // Expand all
            explanationRows.forEach(row => row.classList.remove('collapsed'));
            toggleBtns.forEach(btn => {
                btn.innerHTML = '<i class="fas fa-chevron-up"></i>';
                btn.setAttribute('data-tooltip', 'Hide Explanation');
            });
            toggleAllBtn.innerHTML = '<i class="fas fa-chevron-up"></i> Hide All Explanations';
        } else {
            // Collapse all
            explanationRows.forEach(row => row.classList.add('collapsed'));
            toggleBtns.forEach(btn => {
                btn.innerHTML = '<i class="fas fa-chevron-down"></i>';
                btn.setAttribute('data-tooltip', 'Show Explanation');
            });
            toggleAllBtn.innerHTML = '<i class="fas fa-chevron-down"></i> Show All Explanations';
        }
    }
    

    

    

    
    clearSearch() {
        this.searchInput.value = '';
        this.clearResults();
    }
    
    clearResults() {
        this.resultsContainer.innerHTML = '';
    }
    
    showLoading() {
        this.searchButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Searching...';
        this.searchButton.disabled = true;
    }
    
    hideLoading() {
        this.searchButton.innerHTML = '<i class="fas fa-robot"></i> AI Search';
        this.searchButton.disabled = false;
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
    
    getKeyDisplay(keyValue) {
        return this.MusicalKeys[keyValue] || 'Unknown';
    }
    
    getTypeDisplay(typeValue) {
        return this.ChorusTypes[typeValue] || 'Unknown';
    }
    
    getTimeSignatureDisplay(timeValue) {
        return this.TimeSignatures[timeValue] || 'Unknown';
    }
    
    truncateText(text, maxLength) {
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    }
}

// Global functions for chorus actions
function viewChorus(id) {
    window.open(`/Home/ChorusDisplay/${id}`, '_blank');
}

function editChorus(id) {
    window.open(`/Home/Edit/${id}`, '_blank');
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new AiSearch();
}); 