// Settings Manager
class SettingsManager {
    constructor() {
        this.themes = {
            parchment: {
                name: 'Parchment & Ink',
                background: 'linear-gradient(135deg, #f5efe0 0%, #e8dcc4 100%)',
                textColor: '#1a1a1a',
                chorusBackground: 'linear-gradient(135deg, #f5efe0 0%, #e8dcc4 100%)',
                logoFilter: 'none',
                bodyClass: 'theme-paper-grain-dark',
                motionClass: 'motion-gentle-breathe'
            },
            cathedralStone: {
                name: 'Cathedral Stone',
                background: 'linear-gradient(135deg, #2d2a26 0%, #1f1c18 100%)',
                textColor: '#f0ece2',
                chorusBackground: 'linear-gradient(135deg, #2d2a26 0%, #1f1c18 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-damask-tl',
                motionClass: 'motion-warm-flicker'
            },
            doveDusk: {
                name: 'Dove & Dusk',
                background: 'linear-gradient(135deg, #0f1c2e 0%, #162638 100%)',
                textColor: '#f2f0e8',
                chorusBackground: 'linear-gradient(135deg, #0f1c2e 0%, #162638 100%)',
                logoFilter: 'brightness(0) invert(1)',
                motionClass: 'motion-cloud-drift'
            },
            classicSeal: {
                name: 'Classic Seal',
                background: 'linear-gradient(135deg, #000000 0%, #1a1a1a 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #000000 0%, #1a1a1a 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-fleur-diagonal'
            },
            stainedGlass: {
                name: 'Stained Glass',
                background: 'radial-gradient(circle at 18% 22%, rgba(155, 34, 66, 0.35) 0%, transparent 55%), radial-gradient(circle at 82% 28%, rgba(34, 80, 155, 0.35) 0%, transparent 55%), radial-gradient(circle at 70% 82%, rgba(76, 34, 120, 0.35) 0%, transparent 55%), linear-gradient(160deg, #180a2a 0%, #2a123d 55%, #150720 100%)',
                textColor: '#f6ecff',
                chorusBackground: 'radial-gradient(circle at 18% 22%, rgba(155, 34, 66, 0.35) 0%, transparent 55%), radial-gradient(circle at 82% 28%, rgba(34, 80, 155, 0.35) 0%, transparent 55%), radial-gradient(circle at 70% 82%, rgba(76, 34, 120, 0.35) 0%, transparent 55%), linear-gradient(160deg, #180a2a 0%, #2a123d 55%, #150720 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-glass-drift'
            },
            goldenHour: {
                name: 'Golden Hour',
                background: 'radial-gradient(ellipse at 20% 10%, rgba(212, 162, 74, 0.35) 0%, transparent 55%), radial-gradient(ellipse at 85% 85%, rgba(139, 26, 26, 0.3) 0%, transparent 55%), linear-gradient(145deg, #2b1810 0%, #3d2817 50%, #1a0e08 100%)',
                textColor: '#f5e7c8',
                chorusBackground: 'radial-gradient(ellipse at 20% 10%, rgba(212, 162, 74, 0.35) 0%, transparent 55%), radial-gradient(ellipse at 85% 85%, rgba(139, 26, 26, 0.3) 0%, transparent 55%), linear-gradient(145deg, #2b1810 0%, #3d2817 50%, #1a0e08 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-golden-breathe'
            },
            sanctuary: {
                name: 'Sanctuary',
                background: 'radial-gradient(ellipse at 50% 35%, rgba(212, 162, 74, 0.25) 0%, transparent 65%), linear-gradient(180deg, #1a0808 0%, #3a0f12 55%, #180606 100%)',
                textColor: '#f2e4c5',
                chorusBackground: 'radial-gradient(ellipse at 50% 35%, rgba(212, 162, 74, 0.25) 0%, transparent 65%), linear-gradient(180deg, #1a0808 0%, #3a0f12 55%, #180606 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-small-cross-4corners',
                motionClass: 'motion-warm-flicker'
            },
            fresco: {
                name: 'Fresco',
                background: 'radial-gradient(ellipse at 25% 20%, rgba(255, 255, 255, 0.45) 0%, transparent 55%), linear-gradient(135deg, #e8d8b7 0%, #d4b896 50%, #c9a678 100%)',
                textColor: '#3a2410',
                chorusBackground: 'radial-gradient(ellipse at 25% 20%, rgba(255, 255, 255, 0.45) 0%, transparent 55%), linear-gradient(135deg, #e8d8b7 0%, #d4b896 50%, #c9a678 100%)',
                logoFilter: 'none',
                bodyClass: 'theme-paper-grain-dark',
                motionClass: 'motion-gentle-breathe'
            },
            pentecost: {
                name: 'Pentecost',
                background: 'radial-gradient(circle at 30% 70%, rgba(255, 140, 30, 0.45) 0%, transparent 50%), radial-gradient(circle at 75% 35%, rgba(220, 50, 30, 0.35) 0%, transparent 55%), linear-gradient(180deg, #2a0a06 0%, #551408 55%, #2a0503 100%)',
                textColor: '#ffe4c0',
                chorusBackground: 'radial-gradient(circle at 30% 70%, rgba(255, 140, 30, 0.45) 0%, transparent 50%), radial-gradient(circle at 75% 35%, rgba(220, 50, 30, 0.35) 0%, transparent 55%), linear-gradient(180deg, #2a0a06 0%, #551408 55%, #2a0503 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-pentecost-fire'
            },
            advent: {
                name: 'Advent',
                background: 'radial-gradient(ellipse at 80% 20%, rgba(214, 164, 76, 0.25) 0%, transparent 55%), radial-gradient(ellipse at 20% 85%, rgba(178, 48, 64, 0.25) 0%, transparent 55%), linear-gradient(155deg, #0e2a1a 0%, #1a3d2a 55%, #0b1e13 100%)',
                textColor: '#ece4c6',
                chorusBackground: 'radial-gradient(ellipse at 80% 20%, rgba(214, 164, 76, 0.25) 0%, transparent 55%), radial-gradient(ellipse at 20% 85%, rgba(178, 48, 64, 0.25) 0%, transparent 55%), linear-gradient(155deg, #0e2a1a 0%, #1a3d2a 55%, #0b1e13 100%)',
                logoFilter: 'brightness(0) invert(1)',
                motionClass: 'motion-cloud-drift'
            },
            epiphany: {
                name: 'Epiphany',
                background: 'radial-gradient(circle at 25% 25%, rgba(255, 255, 255, 0.18) 0%, transparent 25%), radial-gradient(circle at 70% 60%, rgba(212, 184, 110, 0.22) 0%, transparent 40%), radial-gradient(circle at 80% 18%, rgba(255, 255, 255, 0.12) 0%, transparent 18%), linear-gradient(160deg, #07102a 0%, #0c1a3d 55%, #040820 100%)',
                textColor: '#eaf0ff',
                chorusBackground: 'radial-gradient(circle at 25% 25%, rgba(255, 255, 255, 0.18) 0%, transparent 25%), radial-gradient(circle at 70% 60%, rgba(212, 184, 110, 0.22) 0%, transparent 40%), radial-gradient(circle at 80% 18%, rgba(255, 255, 255, 0.12) 0%, transparent 18%), linear-gradient(160deg, #07102a 0%, #0c1a3d 55%, #040820 100%)',
                logoFilter: 'brightness(0) invert(1)',
                motionClass: 'motion-star-twinkle'
            },
            easterDawn: {
                name: 'Easter Dawn',
                background: 'radial-gradient(ellipse at 30% 15%, rgba(255, 235, 215, 0.6) 0%, transparent 55%), radial-gradient(ellipse at 80% 85%, rgba(200, 150, 180, 0.35) 0%, transparent 55%), linear-gradient(160deg, #fce7e0 0%, #f5c6c0 50%, #e4a8bf 100%)',
                motionClass: 'motion-gentle-breathe',
                textColor: '#3c1020',
                chorusBackground: 'radial-gradient(ellipse at 30% 15%, rgba(255, 235, 215, 0.6) 0%, transparent 55%), radial-gradient(ellipse at 80% 85%, rgba(200, 150, 180, 0.35) 0%, transparent 55%), linear-gradient(160deg, #fce7e0 0%, #f5c6c0 50%, #e4a8bf 100%)',
                logoFilter: 'none'
            },
            jerusalemStone: {
                name: 'Jerusalem Stone',
                background: 'radial-gradient(ellipse at 25% 20%, rgba(255, 240, 215, 0.55) 0%, transparent 55%), radial-gradient(ellipse at 80% 80%, rgba(170, 90, 50, 0.22) 0%, transparent 55%), linear-gradient(135deg, #e7d6b3 0%, #d4b486 50%, #b9945d 100%)',
                textColor: '#2e1a0a',
                chorusBackground: 'radial-gradient(ellipse at 25% 20%, rgba(255, 240, 215, 0.55) 0%, transparent 55%), radial-gradient(ellipse at 80% 80%, rgba(170, 90, 50, 0.22) 0%, transparent 55%), linear-gradient(135deg, #e7d6b3 0%, #d4b486 50%, #b9945d 100%)',
                logoFilter: 'none',
                bodyClass: 'theme-paper-grain-dark',
                motionClass: 'motion-warm-flicker'
            },
            galilee: {
                name: 'Sea of Galilee',
                background: 'radial-gradient(ellipse at 20% 20%, rgba(255, 255, 255, 0.22) 0%, transparent 45%), radial-gradient(ellipse at 75% 75%, rgba(180, 220, 220, 0.28) 0%, transparent 50%), linear-gradient(170deg, #0a2540 0%, #104764 50%, #0b2a3a 100%)',
                textColor: '#e4f2f5',
                chorusBackground: 'radial-gradient(ellipse at 20% 20%, rgba(255, 255, 255, 0.22) 0%, transparent 45%), radial-gradient(ellipse at 75% 75%, rgba(180, 220, 220, 0.28) 0%, transparent 50%), linear-gradient(170deg, #0a2540 0%, #104764 50%, #0b2a3a 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-galilee-caustic'
            },
            chapelMarble: {
                name: 'Chapel Marble',
                background: 'radial-gradient(ellipse at 30% 20%, rgba(255, 255, 255, 0.35) 0%, transparent 55%), linear-gradient(160deg, #ece7df 0%, #d8d1c4 50%, #c9c1b2 100%)',
                textColor: '#2a2420',
                chorusBackground: 'radial-gradient(ellipse at 30% 20%, rgba(255, 255, 255, 0.35) 0%, transparent 55%), linear-gradient(160deg, #ece7df 0%, #d8d1c4 50%, #c9c1b2 100%)',
                logoFilter: 'none',
                bodyClass: 'theme-marble',
                motionClass: 'motion-cloud-drift'
            },
            altarLinen: {
                name: 'Altar Linen',
                background: 'radial-gradient(ellipse at 25% 15%, rgba(255, 255, 255, 0.45) 0%, transparent 55%), linear-gradient(150deg, #f5ede0 0%, #e8dcc4 55%, #d8c8a6 100%)',
                textColor: '#3a2a14',
                chorusBackground: 'radial-gradient(ellipse at 25% 15%, rgba(255, 255, 255, 0.45) 0%, transparent 55%), linear-gradient(150deg, #f5ede0 0%, #e8dcc4 55%, #d8c8a6 100%)',
                logoFilter: 'none',
                bodyClass: 'theme-linen',
                motionClass: 'motion-gentle-breathe'
            },
            naveStonework: {
                name: 'Nave Stonework',
                background: 'radial-gradient(ellipse at 70% 30%, rgba(214, 164, 76, 0.15) 0%, transparent 55%), linear-gradient(165deg, #2a2824 0%, #3a3731 55%, #231f1a 100%)',
                textColor: '#efe8d6',
                chorusBackground: 'radial-gradient(ellipse at 70% 30%, rgba(214, 164, 76, 0.15) 0%, transparent 55%), linear-gradient(165deg, #2a2824 0%, #3a3731 55%, #231f1a 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-stonework',
                motionClass: 'motion-warm-flicker'
            },
            starlight: {
                name: 'Starlight',
                background: 'linear-gradient(170deg, #050920 0%, #0c1638 55%, #050920 100%)',
                textColor: '#eaf0ff',
                chorusBackground: 'linear-gradient(170deg, #050920 0%, #0c1638 55%, #050920 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-starlight'
            },
            candlelight: {
                name: 'Candlelight',
                background: 'linear-gradient(180deg, #140807 0%, #2a1208 55%, #140807 100%)',
                textColor: '#f5e2b8',
                chorusBackground: 'linear-gradient(180deg, #140807 0%, #2a1208 55%, #140807 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-candlelight'
            },
            crossLattice: {
                name: 'Cross Lattice',
                background: 'linear-gradient(160deg, #0c1a2a 0%, #16283e 55%, #0a1522 100%)',
                textColor: '#e8eef5',
                chorusBackground: 'linear-gradient(160deg, #0c1a2a 0%, #16283e 55%, #0a1522 100%)',
                logoFilter: 'brightness(0) invert(1)',
                bodyClass: 'theme-cross-lattice'
            },
            custom: {
                name: 'Custom Theme',
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                textColor: '#ffffff',
                chorusBackground: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                logoFilter: 'brightness(0) invert(1)'
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
                                <label for="showChurchSeal">
                                    <input type="checkbox" id="showChurchSeal" style="margin-right: 8px;">
                                    Show church seal as watermark
                                </label>
                                <div class="form-help" style="margin-top: 5px; font-size: 12px; color: #999;">
                                    Sits behind the lyrics at low contrast; works with any background animation
                                </div>
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

        const showChurchSeal = document.getElementById('showChurchSeal');
        if (showChurchSeal) {
            showChurchSeal.addEventListener('change', () => this.autoSaveSettings());
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
            debug(`Mass Edit: Fetched ${chorusList.length} total choruses`);

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

            debug(`Mass Edit: Opened first of ${chorusList.length} choruses for editing (sorted alphabetically)`);

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

            const showChurchSeal = document.getElementById('showChurchSeal');
            if (showChurchSeal) {
                showChurchSeal.checked = this.currentSettings.showChurchSeal === true;
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
        const settings = this._collectFormSettings();
        this._persist(settings);
        this.applyTheme(settings);
        this.closeSettings();
        this.showNotification('Settings saved successfully!', 'success');
    }

    autoSaveSettings() {
        const settings = this._collectFormSettings();
        this._persist(settings);
        this.applyTheme(settings);
        this.showNotification('Settings auto-saved', 'success');
    }

    /** @private Collect every settings value from the modal form. */
    _collectFormSettings() {
        const showChurchSealEl = document.getElementById('showChurchSeal');
        return {
            theme: document.getElementById('themeSelect').value,
            customBackground: document.getElementById('customBackground').value,
            customTextColor: document.getElementById('customTextColor').value,
            customChorusBackground: document.getElementById('customChorusBackground').value,
            chorusAnimation: document.getElementById('chorusAnimation').value,
            chorusFont: document.getElementById('chorusFont').value,
            textOutlineWidth: document.getElementById('textOutlineWidth').value,
            textOutlineColor: document.getElementById('textOutlineColor').value,
            textOutlineFeather: document.getElementById('textOutlineFeather').checked,
            showChurchSeal: showChurchSealEl ? showChurchSealEl.checked : false
        };
    }

    /** @private Persist settings to localStorage + sessionStorage mirrors. */
    _persist(settings) {
        localStorage.setItem('chap2Settings', JSON.stringify(settings));
        this.currentSettings = settings;
        sessionStorage.setItem('chorusAnimation', settings.chorusAnimation);
        sessionStorage.setItem('chorusFont', settings.chorusFont);
        sessionStorage.setItem('textOutlineWidth', settings.textOutlineWidth);
        sessionStorage.setItem('textOutlineColor', settings.textOutlineColor);
        sessionStorage.setItem('textOutlineFeather', settings.textOutlineFeather);
        sessionStorage.setItem('showChurchSeal', settings.showChurchSeal ? 'true' : 'false');
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
            // Church seal watermark: off by default, independent of animation.
            if (settings.showChurchSeal === undefined) {
                settings.showChurchSeal = false;
            }
            // Mirror into sessionStorage so the chorus display iframe can read it.
            sessionStorage.setItem('showChurchSeal', settings.showChurchSeal ? 'true' : 'false');
            return settings;
        }
        const defaults = {
            theme: 'cathedralStone',
            customBackground: 'linear-gradient(135deg, #2d2a26 0%, #1f1c18 100%)',
            customTextColor: '#f0ece2',
            customChorusBackground: 'linear-gradient(135deg, #2d2a26 0%, #1f1c18 100%)',
            chorusAnimation: 'musical-staff',
            chorusFont: 'Inter',
            textOutlineWidth: '0',
            textOutlineColor: '#000000',
            textOutlineFeather: false,
            showChurchSeal: false
        };
        sessionStorage.setItem('showChurchSeal', 'false');
        return defaults;
    }

    applyTheme(settings) {
        let theme = settings.theme;
        // Safety: if the stored theme was removed in a later build, fall
        // back to a sensible default rather than throwing on undefined.
        if (theme !== 'custom' && !this.themes[theme]) {
            theme = 'cathedralStone';
            settings.theme = theme;
        }
        let background, textColor, chorusBackground, logoFilter, bodyClass, motionClass;

        if (theme === 'custom') {
            background = settings.customBackground;
            textColor = settings.customTextColor;
            chorusBackground = settings.customChorusBackground;
            logoFilter = this.themes.custom.logoFilter;
            bodyClass = this.themes.custom.bodyClass || null;
            motionClass = this.themes.custom.motionClass || null;
        } else {
            background = this.themes[theme].background;
            textColor = this.themes[theme].textColor;
            chorusBackground = this.themes[theme].chorusBackground;
            logoFilter = this.themes[theme].logoFilter || 'brightness(0) invert(1)';
            bodyClass = this.themes[theme].bodyClass || null;
            motionClass = this.themes[theme].motionClass || null;
        }

        // Apply to body (search page)
        document.body.style.background = background;
        document.body.style.color = textColor;
        document.documentElement.style.setProperty('--logo-filter', logoFilter);

        // Swap the body theme + motion classes (optional pattern and
        // optional independent animation overlay).
        this._applyBodyThemeClass(bodyClass, motionClass);

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

        // Store for chorus display navigation (includes logoFilter so the
        // ChorusDisplay iframe can tint the watermark correctly per theme,
        // and bodyClass / motionClass so the iframe applies the same
        // pattern + animation overlays).
        sessionStorage.setItem('currentTheme', JSON.stringify({
            background: chorusBackground,
            textColor: textColor,
            logoFilter: logoFilter,
            bodyClass: bodyClass,
            motionClass: motionClass
        }));
    }

    /** @private Remove any previously-applied theme-* / motion-* class and add the new ones. */
    _applyBodyThemeClass(bodyClass, motionClass) {
        const toRemove = [];
        for (const cls of document.body.classList) {
            if (cls.startsWith('theme-') || cls.startsWith('motion-')) toRemove.push(cls);
        }
        toRemove.forEach(c => document.body.classList.remove(c));
        if (bodyClass) document.body.classList.add(bodyClass);
        if (motionClass) document.body.classList.add(motionClass);
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
    debug('Settings manager initialized');
});
