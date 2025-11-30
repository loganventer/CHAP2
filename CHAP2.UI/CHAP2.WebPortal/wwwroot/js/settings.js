// Settings Manager
class SettingsManager {
    constructor() {
        this.themes = {
            default: {
                name: 'Default Purple',
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
            },
            dark: {
                name: 'Dark Mode',
                background: 'linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #1a1a1a 0%, #2d2d2d 100%)'
            },
            navy: {
                name: 'Navy Blue',
                background: 'linear-gradient(135deg, #001f3f 0%, #003d7a 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #001f3f 0%, #003d7a 100%)'
            },
            forest: {
                name: 'Forest Green',
                background: 'linear-gradient(135deg, #1a4d2e 0%, #2e7d55 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #1a4d2e 0%, #2e7d55 100%)'
            },
            sunset: {
                name: 'Sunset Orange',
                background: 'linear-gradient(135deg, #ff6b35 0%, #f7931e 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #ff6b35 0%, #f7931e 100%)'
            },
            ocean: {
                name: 'Ocean Blue',
                background: 'linear-gradient(135deg, #0066cc 0%, #0099ff 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #0066cc 0%, #0099ff 100%)'
            },
            royal: {
                name: 'Royal Purple',
                background: 'linear-gradient(135deg, #4a148c 0%, #7b1fa2 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #4a148c 0%, #7b1fa2 100%)'
            },
            custom: {
                name: 'Custom Theme',
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
            }
        };

        this.currentSettings = this.loadSettings();
        this.init();
    }

    init() {
        this.createSettingsModal();
        this.applyTheme(this.currentSettings);
        this.setupEventListeners();
    }

    createSettingsModal() {
        const modalHTML = `
            <div class="settings-modal" id="settingsModal">
                <div class="settings-content">
                    <div class="settings-header">
                        <h2><i class="fas fa-cog"></i> Settings</h2>
                        <button class="settings-close" id="settingsClose">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>

                    <div class="settings-body">
                        <div class="settings-section">
                            <div class="settings-section-title">
                                <i class="fas fa-palette"></i>
                                Theme Selection
                            </div>

                            <div class="form-group">
                                <label for="themeSelect">Choose Theme</label>
                                <select id="themeSelect" class="form-control">
                                    ${Object.keys(this.themes).map(key => `
                                        <option value="${key}" ${this.currentSettings.theme === key ? 'selected' : ''}>
                                            ${this.themes[key].name}
                                        </option>
                                    `).join('')}
                                </select>
                            </div>

                            <div id="customThemeSection" style="display: ${this.currentSettings.theme === 'custom' ? 'block' : 'none'}">
                                <div class="form-group">
                                    <label for="customBackground">Background Color</label>
                                    <div class="color-input-wrapper">
                                        <input type="color" id="customBackgroundColor" value="#667eea">
                                        <input type="text" id="customBackground" placeholder="e.g., linear-gradient(135deg, #667eea 0%, #764ba2 100%)" value="${this.currentSettings.customBackground || 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'}">
                                    </div>
                                </div>

                                <div class="form-group">
                                    <label for="customTextColor">Text Color</label>
                                    <div class="color-input-wrapper">
                                        <input type="color" id="customTextColor" value="${this.currentSettings.customTextColor || '#ffffff'}">
                                        <input type="text" id="customTextColorHex" value="${this.currentSettings.customTextColor || '#ffffff'}" readonly>
                                    </div>
                                </div>

                                <div class="form-group">
                                    <label for="customChorusBackground">Chorus Display Background</label>
                                    <div class="color-input-wrapper">
                                        <input type="color" id="customChorusBackgroundColor" value="#001f3f">
                                        <input type="text" id="customChorusBackground" placeholder="e.g., linear-gradient(135deg, #001f3f 0%, #003d7a 100%)" value="${this.currentSettings.customChorusBackground || 'linear-gradient(135deg, #001f3f 0%, #003d7a 100%)'}">
                                    </div>
                                </div>
                            </div>

                            <div class="theme-preview" id="themePreview">
                                <div class="theme-preview-title">Preview</div>
                                <div class="theme-preview-text">This is how your theme will look</div>
                            </div>
                        </div>

                        <div class="settings-section">
                            <div class="settings-section-title">
                                <i class="fas fa-edit"></i>
                                Mass Edit
                            </div>
                            <p style="color: #666; font-size: 14px; margin-bottom: 16px;">
                                Edit all choruses one by one with easy navigation between them.
                            </p>
                            <button class="mass-edit-btn" id="massEditBtn" style="width: 100%;">
                                <i class="fas fa-edit"></i> Start Mass Edit
                            </button>
                        </div>

                        <div class="settings-section">
                            <div class="settings-section-title">
                                <i class="fas fa-music"></i>
                                Chorus Display Settings
                            </div>

                            <div class="form-group">
                                <label for="chorusAnimation">Background Animation</label>
                                <select id="chorusAnimation" class="form-control">
                                    <option value="musical-staff">Musical Staff (Wave)</option>
                                    <option value="floating-notes">Floating Notes (Classic)</option>
                                    <option value="particle-flow">Particle Flow</option>
                                    <option value="aurora">Aurora Wave</option>
                                    <option value="aurora-borealis">Aurora Borealis (True)</option>
                                    <option value="color-shift">Celestial</option>
                                    <option value="none">None</option>
                                </select>
                            </div>

                            <div class="form-group">
                                <label for="chorusFont">Font Family</label>
                                <select id="chorusFont" class="form-control">
                                    <option value="Inter">Inter (Default)</option>
                                    <option value="Arial">Arial</option>
                                    <option value="Georgia">Georgia</option>
                                    <option value="Times New Roman">Times New Roman</option>
                                    <option value="Courier New">Courier New</option>
                                    <option value="Verdana">Verdana</option>
                                    <option value="Palatino">Palatino</option>
                                    <option value="Trebuchet MS">Trebuchet MS</option>
                                </select>
                            </div>

                            <div class="form-group">
                                <label for="textOutlineWidth">Text Outline Width</label>
                                <select id="textOutlineWidth" class="form-control">
                                    <option value="0">None</option>
                                    <option value="1">Very Thin (1px)</option>
                                    <option value="2">Thin (2px)</option>
                                    <option value="3">Medium (3px)</option>
                                    <option value="4">Thick (4px)</option>
                                    <option value="5">Very Thick (5px)</option>
                                    <option value="6">Extra Thick (6px)</option>
                                    <option value="7">Ultra Thick (7px)</option>
                                    <option value="8">Maximum (8px)</option>
                                </select>
                            </div>

                            <div class="form-group">
                                <label for="textOutlineColor">Text Outline Color</label>
                                <div class="color-input-wrapper">
                                    <input type="color" id="textOutlineColor" value="${this.currentSettings.textOutlineColor || '#000000'}">
                                    <input type="text" id="textOutlineColorHex" value="${this.currentSettings.textOutlineColor || '#000000'}" readonly>
                                </div>
                            </div>

                            <div class="form-group">
                                <label for="textOutlineFeather">
                                    <input type="checkbox" id="textOutlineFeather" style="margin-right: 8px;">
                                    Feather Text Outline (Frosted Glass Effect)
                                </label>
                                <div class="form-help" style="margin-top: 5px; font-size: 12px; color: #999;">
                                    Applies a blurred, frosted glass effect to the outline while keeping text crisp
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="settings-footer">
                        <button class="settings-btn settings-btn-cancel" id="settingsCancel">
                            <i class="fas fa-times"></i> Cancel
                        </button>
                        <button class="settings-btn settings-btn-save" id="settingsSave">
                            <i class="fas fa-save"></i> Save Changes
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHTML);
    }

    setupEventListeners() {
        // Open settings
        const settingsButton = document.getElementById('settingsButton');
        if (settingsButton) {
            settingsButton.addEventListener('click', () => this.openSettings());
        }

        // Close settings
        const closeBtn = document.getElementById('settingsClose');
        const cancelBtn = document.getElementById('settingsCancel');
        const modal = document.getElementById('settingsModal');

        if (closeBtn) closeBtn.addEventListener('click', () => this.closeSettings());
        if (cancelBtn) cancelBtn.addEventListener('click', () => this.closeSettings());
        if (modal) {
            modal.addEventListener('click', (e) => {
                if (e.target === modal) this.closeSettings();
            });
        }

        // Save settings
        const saveBtn = document.getElementById('settingsSave');
        if (saveBtn) saveBtn.addEventListener('click', () => this.saveSettings());

        // Theme selection
        const themeSelect = document.getElementById('themeSelect');
        if (themeSelect) {
            themeSelect.addEventListener('change', (e) => this.onThemeChange(e.target.value));
        }

        // Custom color inputs
        const customBackgroundColor = document.getElementById('customBackgroundColor');
        const customTextColor = document.getElementById('customTextColor');
        const customChorusBackgroundColor = document.getElementById('customChorusBackgroundColor');
        const textOutlineColor = document.getElementById('textOutlineColor');

        if (customBackgroundColor) {
            customBackgroundColor.addEventListener('input', (e) => {
                document.getElementById('customBackground').value = `linear-gradient(135deg, ${e.target.value} 0%, ${e.target.value} 100%)`;
                this.updatePreview();
            });
        }

        if (customTextColor) {
            customTextColor.addEventListener('input', (e) => {
                document.getElementById('customTextColorHex').value = e.target.value;
                this.updatePreview();
            });
        }

        if (customChorusBackgroundColor) {
            customChorusBackgroundColor.addEventListener('input', (e) => {
                document.getElementById('customChorusBackground').value = `linear-gradient(135deg, ${e.target.value} 0%, ${e.target.value} 100%)`;
                this.updatePreview();
            });
        }

        if (textOutlineColor) {
            textOutlineColor.addEventListener('input', (e) => {
                document.getElementById('textOutlineColorHex').value = e.target.value;
                this.autoSaveSettings();
            });
        }

        // Update preview on text input change
        const customBackground = document.getElementById('customBackground');
        const customChorusBackground = document.getElementById('customChorusBackground');

        if (customBackground) {
            customBackground.addEventListener('input', () => this.updatePreview());
        }

        if (customChorusBackground) {
            customChorusBackground.addEventListener('input', () => this.updatePreview());
        }

        // Auto-save on chorus settings change
        const chorusAnimation = document.getElementById('chorusAnimation');
        const chorusFont = document.getElementById('chorusFont');
        const textOutlineWidth = document.getElementById('textOutlineWidth');
        const textOutlineFeather = document.getElementById('textOutlineFeather');

        if (chorusAnimation) {
            chorusAnimation.addEventListener('change', () => this.autoSaveSettings());
        }

        if (chorusFont) {
            chorusFont.addEventListener('change', () => this.autoSaveSettings());
        }

        if (textOutlineWidth) {
            textOutlineWidth.addEventListener('change', () => this.autoSaveSettings());
        }

        if (textOutlineFeather) {
            textOutlineFeather.addEventListener('change', () => this.autoSaveSettings());
        }

        // Auto-save on theme change
        if (themeSelect) {
            themeSelect.addEventListener('change', () => this.autoSaveSettings());
        }

        // Mass Edit button
        const massEditBtn = document.getElementById('massEditBtn');
        if (massEditBtn) {
            massEditBtn.addEventListener('click', () => this.enterMassEditMode());
        }
    }

    async enterMassEditMode() {
        try {
            const btn = document.getElementById('massEditBtn');
            if (btn) {
                btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading...';
                btn.disabled = true;
            }

            // Fetch all choruses (using dedicated endpoint, not search which has limits)
            const response = await fetch('/Home/GetAllChoruses');
            if (!response.ok) {
                throw new Error('Failed to fetch choruses');
            }

            const data = await response.json();
            let chorusList = data.results || [];
            console.log(`Mass Edit: Fetched ${chorusList.length} total choruses`);

            if (chorusList.length === 0) {
                alert('No choruses found to edit.');
                if (btn) {
                    btn.innerHTML = '<i class="fas fa-edit"></i> Start Mass Edit';
                    btn.disabled = false;
                }
                return;
            }

            // Sort choruses alphabetically by name for consistent ordering
            chorusList.sort((a, b) => (a.name || '').localeCompare(b.name || ''));

            // Store in sessionStorage for navigation
            sessionStorage.setItem('chorusList', JSON.stringify(chorusList));
            sessionStorage.setItem('currentChorusId', chorusList[0].id);
            sessionStorage.setItem('massEditMode', 'true');

            // Close settings modal
            this.closeSettings();

            // Open first chorus in Edit mode
            window.open(`/Home/Edit/${chorusList[0].id}`, '_blank');

            console.log(`Mass Edit: Opened first of ${chorusList.length} choruses for editing (sorted alphabetically)`);

            // Reset button
            if (btn) {
                btn.innerHTML = '<i class="fas fa-edit"></i> Start Mass Edit';
                btn.disabled = false;
            }
        } catch (error) {
            console.error('Mass Edit: Error entering mass edit mode', error);
            alert('Error loading choruses for mass edit. Please try again.');

            const btn = document.getElementById('massEditBtn');
            if (btn) {
                btn.innerHTML = '<i class="fas fa-edit"></i> Start Mass Edit';
                btn.disabled = false;
            }
        }
    }

    onThemeChange(themeKey) {
        const customSection = document.getElementById('customThemeSection');
        if (customSection) {
            customSection.style.display = themeKey === 'custom' ? 'block' : 'none';
        }
        this.updatePreview();
    }

    updatePreview() {
        const themeSelect = document.getElementById('themeSelect');
        const preview = document.getElementById('themePreview');

        if (!themeSelect || !preview) return;

        const selectedTheme = themeSelect.value;
        let background, textColor;

        if (selectedTheme === 'custom') {
            background = document.getElementById('customBackground').value;
            textColor = document.getElementById('customTextColor').value;
        } else {
            background = this.themes[selectedTheme].background;
            textColor = this.themes[selectedTheme].textColor;
        }

        preview.style.background = background;
        preview.style.color = textColor;
    }

    openSettings() {
        const modal = document.getElementById('settingsModal');
        if (modal) {
            modal.classList.add('active');
            this.updatePreview();

            // Set chorus animation dropdown to current value
            const chorusAnimation = document.getElementById('chorusAnimation');
            if (chorusAnimation && this.currentSettings.chorusAnimation) {
                chorusAnimation.value = this.currentSettings.chorusAnimation;
            }

            // Set chorus font dropdown to current value
            const chorusFont = document.getElementById('chorusFont');
            if (chorusFont && this.currentSettings.chorusFont) {
                chorusFont.value = this.currentSettings.chorusFont;
            }

            // Set text outline width dropdown to current value
            const textOutlineWidth = document.getElementById('textOutlineWidth');
            if (textOutlineWidth && this.currentSettings.textOutlineWidth !== undefined) {
                textOutlineWidth.value = this.currentSettings.textOutlineWidth;
            }

            // Set text outline color to current value
            const textOutlineColor = document.getElementById('textOutlineColor');
            const textOutlineColorHex = document.getElementById('textOutlineColorHex');
            if (textOutlineColor && this.currentSettings.textOutlineColor) {
                textOutlineColor.value = this.currentSettings.textOutlineColor;
                if (textOutlineColorHex) {
                    textOutlineColorHex.value = this.currentSettings.textOutlineColor;
                }
            }

            // Set text outline feather checkbox to current value
            const textOutlineFeather = document.getElementById('textOutlineFeather');
            if (textOutlineFeather) {
                textOutlineFeather.checked = this.currentSettings.textOutlineFeather || false;
            }
        }
    }

    closeSettings() {
        const modal = document.getElementById('settingsModal');
        if (modal) {
            modal.classList.remove('active');
        }
    }

    saveSettings() {
        const themeSelect = document.getElementById('themeSelect');
        const theme = themeSelect.value;
        const chorusAnimation = document.getElementById('chorusAnimation').value;
        const chorusFont = document.getElementById('chorusFont').value;
        const textOutlineWidth = document.getElementById('textOutlineWidth').value;
        const textOutlineColor = document.getElementById('textOutlineColor').value;
        const textOutlineFeather = document.getElementById('textOutlineFeather').checked;

        const settings = {
            theme: theme,
            customBackground: document.getElementById('customBackground').value,
            customTextColor: document.getElementById('customTextColor').value,
            customChorusBackground: document.getElementById('customChorusBackground').value,
            chorusAnimation: chorusAnimation,
            chorusFont: chorusFont,
            textOutlineWidth: textOutlineWidth,
            textOutlineColor: textOutlineColor,
            textOutlineFeather: textOutlineFeather
        };

        localStorage.setItem('chap2Settings', JSON.stringify(settings));
        this.currentSettings = settings;
        this.applyTheme(settings);
        this.closeSettings();

        // Show success notification
        this.showNotification('Settings saved successfully!', 'success');

        // Store chorus settings in sessionStorage for immediate access
        sessionStorage.setItem('chorusAnimation', chorusAnimation);
        sessionStorage.setItem('chorusFont', chorusFont);
        sessionStorage.setItem('textOutlineWidth', textOutlineWidth);
        sessionStorage.setItem('textOutlineColor', textOutlineColor);
        sessionStorage.setItem('textOutlineFeather', textOutlineFeather);
    }

    autoSaveSettings() {
        const themeSelect = document.getElementById('themeSelect');
        const theme = themeSelect.value;
        const chorusAnimation = document.getElementById('chorusAnimation').value;
        const chorusFont = document.getElementById('chorusFont').value;
        const textOutlineWidth = document.getElementById('textOutlineWidth').value;
        const textOutlineColor = document.getElementById('textOutlineColor').value;
        const textOutlineFeather = document.getElementById('textOutlineFeather').checked;

        const settings = {
            theme: theme,
            customBackground: document.getElementById('customBackground').value,
            customTextColor: document.getElementById('customTextColor').value,
            customChorusBackground: document.getElementById('customChorusBackground').value,
            chorusAnimation: chorusAnimation,
            chorusFont: chorusFont,
            textOutlineWidth: textOutlineWidth,
            textOutlineColor: textOutlineColor,
            textOutlineFeather: textOutlineFeather
        };

        localStorage.setItem('chap2Settings', JSON.stringify(settings));
        this.currentSettings = settings;
        this.applyTheme(settings);

        // Store chorus settings in sessionStorage for immediate access
        sessionStorage.setItem('chorusAnimation', chorusAnimation);
        sessionStorage.setItem('chorusFont', chorusFont);
        sessionStorage.setItem('textOutlineWidth', textOutlineWidth);
        sessionStorage.setItem('textOutlineColor', textOutlineColor);
        sessionStorage.setItem('textOutlineFeather', textOutlineFeather);

        // Show brief notification
        this.showNotification('Settings auto-saved', 'success');
    }

    loadSettings() {
        const saved = localStorage.getItem('chap2Settings');
        if (saved) {
            const settings = JSON.parse(saved);
            // Set default animation if not present
            if (!settings.chorusAnimation) {
                settings.chorusAnimation = 'musical-staff';
            }
            // Set default font if not present
            if (!settings.chorusFont) {
                settings.chorusFont = 'Inter';
            }
            // Set default text outline width if not present
            if (settings.textOutlineWidth === undefined) {
                settings.textOutlineWidth = '0';
            }
            // Set default text outline color if not present
            if (!settings.textOutlineColor) {
                settings.textOutlineColor = '#000000';
            }
            // Set default text outline feather if not present
            if (settings.textOutlineFeather === undefined) {
                settings.textOutlineFeather = false;
            }
            return settings;
        }
        return {
            theme: 'default',
            customBackground: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            customTextColor: '#ffffff',
            customChorusBackground: 'linear-gradient(135deg, #001f3f 0%, #003d7a 100%)',
            chorusAnimation: 'musical-staff',
            chorusFont: 'Inter',
            textOutlineWidth: '0',
            textOutlineColor: '#000000',
            textOutlineFeather: false
        };
    }

    applyTheme(settings) {
        const theme = settings.theme;
        let background, textColor, chorusBackground;

        if (theme === 'custom') {
            background = settings.customBackground;
            textColor = settings.customTextColor;
            chorusBackground = settings.customChorusBackground;
        } else {
            background = this.themes[theme].background;
            textColor = this.themes[theme].textColor;
            chorusBackground = this.themes[theme].chorusBackground;
        }

        // Apply to body (search page)
        document.body.style.background = background;
        document.body.style.color = textColor;

        // Apply to chorus display pages
        const chorusDisplayPage = document.querySelector('.chorus-display-page');
        if (chorusDisplayPage) {
            // Only apply background if animation is NOT color-shift
            const chorusAnimation = settings.chorusAnimation || sessionStorage.getItem('chorusAnimation') || 'musical-staff';
            if (chorusAnimation !== 'color-shift') {
                chorusDisplayPage.style.background = chorusBackground;
            } else {
                // For color-shift, ensure no background is set (animation handles it)
                chorusDisplayPage.style.background = 'transparent';
            }
        }

        // Store for chorus display navigation
        sessionStorage.setItem('currentTheme', JSON.stringify({
            background: chorusBackground,
            textColor: textColor
        }));
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `settings-notification ${type}`;
        notification.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'info-circle'}"></i>
            <span>${message}</span>
        `;

        // Add styles
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'success' ? '#10b981' : '#6366f1'};
            color: white;
            padding: 16px 24px;
            border-radius: 8px;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
            z-index: 10001;
            display: flex;
            align-items: center;
            gap: 12px;
            font-weight: 500;
            animation: slideInRight 0.3s ease-out;
        `;

        document.body.appendChild(notification);

        // Remove after 3 seconds
        setTimeout(() => {
            notification.style.animation = 'slideOutRight 0.3s ease-out';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }
}

// Add notification animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }

    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);

// Initialize settings manager
document.addEventListener('DOMContentLoaded', () => {
    window.settingsManager = new SettingsManager();
    console.log('Settings manager initialized');
});
