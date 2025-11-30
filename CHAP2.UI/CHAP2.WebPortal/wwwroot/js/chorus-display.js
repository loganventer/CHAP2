// Chorus Display JavaScript
/**
 * ChorusDisplay - Advanced chorus display and navigation system
 *
 * CHORUS LIST NAVIGATION FEATURE:
 * -------------------------------
 * This system allows users to navigate between multiple choruses without returning to the search page.
 *
 * HOW IT WORKS:
 * 1. When a user views a chorus from the search results, the current search results are stored in sessionStorage
 * 2. The detail view loads the chorus list from sessionStorage
 * 3. Users can navigate between choruses using:
 *    - Arrow Up/Down keys (navigate between choruses)
 *    - Ctrl/Cmd + Arrow Left/Right keys (navigate between choruses)
 *    - Arrow Left/Right keys alone (navigate between pages of the current chorus)
 *    - Navigation buttons in the UI (if available)
 *
 * USAGE FROM THE UI:
 * ------------------
 * Option 1: Automatic (from search results)
 * - The search-integration.js automatically stores the chorus list when viewChorus() is called
 *
 * Option 2: Manual (from custom code)
 * - Call window.setChorusList(chorusList, currentChorusId) to set the navigation list
 * - Example:
 *   ```javascript
 *   const searchResults = [...]; // Array of chorus objects
 *   const currentId = '403b775e-9e53-40dd-8b4d-90a357be8fb6';
 *   window.setChorusList(searchResults, currentId);
 *   ```
 *
 * Option 3: Via sessionStorage
 * - Store chorus list: sessionStorage.setItem('chorusList', JSON.stringify(chorusList));
 * - Store current ID: sessionStorage.setItem('currentChorusId', chorusId);
 * - The detail view will automatically load it on initialization
 *
 * KEYBOARD SHORTCUTS:
 * -------------------
 * - Arrow Up/Down: Navigate to previous/next chorus
 * - Ctrl/Cmd + Arrow Left/Right: Navigate to previous/next chorus
 * - Arrow Left/Right: Navigate between pages (if chorus has multiple pages)
 * - +/=: Increase font size
 * - -: Decrease font size
 * - Escape: Close detail view
 * - Ctrl/Cmd + P: Print
 */
class ChorusDisplay {
    constructor() {
        this.currentChorusIndex = 0;
        this.choruses = [];
        this.currentFontSize = 86; // Changed from 96 to 86
        this.minFontSize = 12;
        this.maxFontSize = 96; // Increased from 72 to 96
        this.fontSizeStep = 2;
        this.currentPage = 0;
        this.totalPages = 1;
        this.linesPerPage = 10;
        this.currentChorusLines = [];
        this.wrappedLinesPerOriginalLine = [];
        this.totalWrappedLines = 0;

        // Auto-hide buttons settings
        this.hideButtonsTimeout = null;
        this.hideDelay = 2000; // 2000ms of inactivity
        this.buttonsVisible = true;

        // Initialize the display
        this.init();
    }
    
    init() {
        this.loadChoruses();
        this.setupEventListeners();

        // Apply saved font from settings
        this.applyChorusFontFromSettings();

        // Apply saved animation from settings
        this.applyAnimationFromSettings();

        // Process initial chorus data if available
        if (window.chorusData) {
            this.updateDisplay(window.chorusData);
        }

        // Only update navigation buttons if we're on a chorus display page
        if (window.chorusData || window.location.pathname.includes('/ChorusDisplay/')) {
            this.updateNavigationButtons();
        }

        // Initialize auto-hide buttons
        this.setupAutoHideButtons();
    }

    applyChorusFontFromSettings() {
        // Get font from sessionStorage or use default
        const chorusFont = sessionStorage.getItem('chorusFont') || 'Inter';

        // Apply font to the entire chorus display page
        const chorusPage = document.querySelector('.chorus-display-page');
        if (chorusPage) {
            chorusPage.style.fontFamily = `'${chorusFont}', sans-serif`;
        }

        // Apply to chorus text specifically
        const chorusText = document.querySelector('.chorus-text');
        if (chorusText) {
            chorusText.style.fontFamily = `'${chorusFont}', sans-serif`;
        }

        // Apply to title and key
        const chorusTitle = document.getElementById('chorusTitle');
        const chorusKey = document.getElementById('chorusKey');
        if (chorusTitle) chorusTitle.style.fontFamily = `'${chorusFont}', sans-serif`;
        if (chorusKey) chorusKey.style.fontFamily = `'${chorusFont}', sans-serif`;

        console.log(`Applied chorus font: ${chorusFont}`);
    }

    applyAnimationFromSettings() {
        // Get animation from sessionStorage or use default
        const chorusAnimation = sessionStorage.getItem('chorusAnimation') || 'musical-staff';

        console.log(`Applying chorus animation: ${chorusAnimation}`);

        // Get animation elements
        const musicalStaff = document.querySelector('.musical-staff-background');
        const flowingNotesContainer = document.getElementById('flowingNotesContainer');

        // Hide all animations first
        if (musicalStaff) musicalStaff.style.display = 'none';
        if (flowingNotesContainer) flowingNotesContainer.style.display = 'none';

        // Apply background color for all animations except color-shift
        const chorusDisplayPage = document.querySelector('.chorus-display-page');
        if (chorusDisplayPage && chorusAnimation !== 'color-shift') {
            // Get theme background from sessionStorage
            const currentTheme = sessionStorage.getItem('currentTheme');
            if (currentTheme) {
                try {
                    const theme = JSON.parse(currentTheme);
                    chorusDisplayPage.style.background = theme.background;
                } catch (e) {
                    console.warn('Failed to parse theme:', e);
                }
            }
        } else if (chorusDisplayPage && chorusAnimation === 'color-shift') {
            // For color-shift, clear background
            chorusDisplayPage.style.background = 'transparent';
        }

        // Apply selected animation
        switch (chorusAnimation) {
            case 'musical-staff':
                // Show musical staff with flowing notes
                if (musicalStaff) musicalStaff.style.display = 'block';
                if (flowingNotesContainer) flowingNotesContainer.style.display = 'block';
                this.initFlowingNotes();
                break;

            case 'floating-notes':
                // Show only floating notes (classic)
                if (flowingNotesContainer) flowingNotesContainer.style.display = 'block';
                this.initFloatingNotes();
                break;

            case 'particle-flow':
                // Show particle flow animation
                this.initParticleFlow();
                break;

            case 'aurora':
                // Show aurora wave animation
                this.initAuroraWave();
                break;

            case 'aurora-borealis':
                // Show true aurora borealis animation
                this.initAuroraBorealis();
                break;

            case 'color-shift':
                // Show color shifting animation
                this.initColorShift();
                break;

            case 'none':
                // No animation - all already hidden
                console.log('No animation selected');
                break;

            default:
                // Default to musical staff
                if (musicalStaff) musicalStaff.style.display = 'block';
                if (flowingNotesContainer) flowingNotesContainer.style.display = 'block';
                this.initFlowingNotes();
                break;
        }
    }

    initFlowingNotes() {
        const container = document.getElementById('flowingNotesContainer');
        if (!container) return;
        if (this.flowingNotesInterval) return; // Already initialized

        const musicalNotes = ['♪', '♫', '♬', '♩', '♭', '♯', '𝄞'];
        // Staff line positions matching the SVG (35%, 41%, 47%, 53%, 59% for centered staff)
        const staffPositions = [
            35, 41, 47, 53, 59    // Single centered staff with wider spacing
        ];

        // Create notes at intervals
        this.flowingNotesInterval = setInterval(() => {
            const note = document.createElement('div');
            note.className = 'flowing-note';
            note.textContent = musicalNotes[Math.floor(Math.random() * musicalNotes.length)];

            // Position note on one of the staff lines
            const staffY = staffPositions[Math.floor(Math.random() * staffPositions.length)];
            note.style.top = staffY + '%';
            note.style.left = '100vw';

            // Random duration between 15-25 seconds
            const duration = 15 + Math.random() * 10;
            note.style.animationDuration = duration + 's';

            // Random delay
            note.style.animationDelay = Math.random() * 2 + 's';

            container.appendChild(note);

            // Remove note after animation completes
            setTimeout(() => {
                note.remove();
            }, (duration + 2) * 1000);
        }, 2000); // Create new note every 2 seconds
    }

    initFloatingNotes() {
        // Classic floating notes animation (without staff lines)
        const container = document.getElementById('flowingNotesContainer');
        if (!container) return;
        if (this.floatingNotesInterval) return; // Already initialized

        container.innerHTML = ''; // Clear existing content
        const musicalNotes = ['♪', '♫', '♬', '♩', '♭', '♯', '𝄞'];

        // Create floating notes at random positions
        this.floatingNotesInterval = setInterval(() => {
            const note = document.createElement('div');
            note.className = 'floating-note-classic';
            note.textContent = musicalNotes[Math.floor(Math.random() * musicalNotes.length)];

            // Random position
            note.style.top = Math.random() * 100 + '%';
            note.style.left = Math.random() * 100 + '%';
            note.style.fontSize = (20 + Math.random() * 40) + 'px';
            note.style.position = 'absolute';
            note.style.color = 'rgba(255, 255, 255, 0.3)';
            note.style.animation = 'floatClassic 8s ease-in-out infinite';
            note.style.animationDelay = Math.random() * 4 + 's';

            container.appendChild(note);

            // Remove after animation
            setTimeout(() => {
                note.remove();
            }, 8000);
        }, 1000);
    }

    initParticleFlow() {
        // Particle flow animation
        const animatedBg = document.querySelector('.animated-background');
        if (!animatedBg) return;

        const canvas = document.createElement('canvas');
        canvas.id = 'particleCanvas';
        canvas.style.position = 'fixed';
        canvas.style.top = '0';
        canvas.style.left = '0';
        canvas.style.width = '100%';
        canvas.style.height = '100%';
        canvas.style.zIndex = '0';
        canvas.style.pointerEvents = 'none';
        animatedBg.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;

        const particles = [];
        const particleCount = 50;

        for (let i = 0; i < particleCount; i++) {
            particles.push({
                x: Math.random() * canvas.width,
                y: Math.random() * canvas.height,
                vx: (Math.random() - 0.5) * 2,
                vy: (Math.random() - 0.5) * 2,
                size: Math.random() * 3 + 1
            });
        }

        const animate = () => {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.fillStyle = 'rgba(255, 255, 255, 0.5)';

            particles.forEach(p => {
                p.x += p.vx;
                p.y += p.vy;

                if (p.x < 0 || p.x > canvas.width) p.vx *= -1;
                if (p.y < 0 || p.y > canvas.height) p.vy *= -1;

                ctx.beginPath();
                ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
                ctx.fill();
            });

            this.particleAnimationFrame = requestAnimationFrame(animate);
        };

        animate();
    }

    initAuroraWave() {
        // Aurora Borealis effect with multiple colored waves
        const animatedBg = document.querySelector('.animated-background');
        if (!animatedBg) return;
        if (this.auroraAnimationFrame) return; // Already initialized

        const canvas = document.createElement('canvas');
        canvas.id = 'auroraCanvas';
        canvas.style.position = 'fixed';
        canvas.style.top = '0';
        canvas.style.left = '0';
        canvas.style.width = '100%';
        canvas.style.height = '100%';
        canvas.style.zIndex = '0';
        canvas.style.pointerEvents = 'none';
        animatedBg.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;

        let time = 0;

        // Aurora wave layers with different colors
        const waves = [
            { color: 'rgba(0, 255, 136, 0.15)', speed: 0.02, amplitude: 80, frequency: 0.008 },
            { color: 'rgba(57, 255, 20, 0.12)', speed: 0.025, amplitude: 60, frequency: 0.01 },
            { color: 'rgba(0, 191, 255, 0.1)', speed: 0.018, amplitude: 70, frequency: 0.012 },
            { color: 'rgba(138, 43, 226, 0.08)', speed: 0.022, amplitude: 50, frequency: 0.015 },
            { color: 'rgba(255, 0, 255, 0.06)', speed: 0.015, amplitude: 90, frequency: 0.007 }
        ];

        const drawWave = () => {
            // Fade out previous frame instead of clearing
            ctx.fillStyle = 'rgba(0, 0, 0, 0.05)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            // Draw multiple aurora waves
            waves.forEach((wave, index) => {
                ctx.beginPath();

                for (let x = 0; x < canvas.width; x += 2) {
                    // Create flowing wave pattern
                    const y1 = canvas.height * 0.3 +
                               Math.sin(x * wave.frequency + time * wave.speed) * wave.amplitude +
                               Math.sin(x * wave.frequency * 2 + time * wave.speed * 1.5) * (wave.amplitude * 0.5);

                    if (x === 0) {
                        ctx.moveTo(x, y1);
                    } else {
                        ctx.lineTo(x, y1);
                    }
                }

                // Complete the shape
                ctx.lineTo(canvas.width, canvas.height);
                ctx.lineTo(0, canvas.height);
                ctx.closePath();

                // Create gradient for each wave
                const gradient = ctx.createLinearGradient(0, 0, 0, canvas.height);
                gradient.addColorStop(0, wave.color);
                gradient.addColorStop(0.5, wave.color.replace(/[\d.]+\)/, '0.05)'));
                gradient.addColorStop(1, 'rgba(0, 0, 0, 0)');

                ctx.fillStyle = gradient;
                ctx.fill();

                // Add glow effect
                ctx.strokeStyle = wave.color.replace(/[\d.]+\)/, '0.3)');
                ctx.lineWidth = 2;
                ctx.stroke();
            });

            time += 0.5;
            this.auroraAnimationFrame = requestAnimationFrame(drawWave);
        };

        // Handle window resize
        const resizeCanvas = () => {
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
        };
        window.addEventListener('resize', resizeCanvas);

        drawWave();
    }

    initAuroraBorealis() {
        // True Aurora Borealis effect with frosted glass blur
        const animatedBg = document.querySelector('.animated-background');
        if (!animatedBg) return;
        if (this.auroraBorealisAnimationFrame) return; // Already initialized

        const canvas = document.createElement('canvas');
        canvas.id = 'auroraBorealisCanvas';
        canvas.style.position = 'fixed';
        canvas.style.top = '0';
        canvas.style.left = '0';
        canvas.style.width = '100%';
        canvas.style.height = '100%';
        canvas.style.zIndex = '0';
        canvas.style.pointerEvents = 'none';
        // Apply frosted glass effect using CSS backdrop-filter
        canvas.style.backdropFilter = 'blur(80px) saturate(180%)';
        canvas.style.webkitBackdropFilter = 'blur(80px) saturate(180%)';
        animatedBg.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;

        let time = 0;

        // Aurora wave layers with realistic colors and frosted glass blur
        const waves = [
            { color: 'rgba(0, 255, 127, 0.03)', speed: 0.015, amplitude: 100, frequency: 0.006, blur: 40 },
            { color: 'rgba(173, 255, 47, 0.025)', speed: 0.012, amplitude: 80, frequency: 0.008, blur: 35 },
            { color: 'rgba(0, 255, 255, 0.022)', speed: 0.018, amplitude: 90, frequency: 0.007, blur: 38 },
            { color: 'rgba(138, 43, 226, 0.02)', speed: 0.01, amplitude: 70, frequency: 0.009, blur: 32 }
        ];

        const drawAurora = () => {
            // Dark starry background
            ctx.fillStyle = 'rgba(5, 10, 20, 0.15)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            // Draw multiple blurred aurora waves with frosted glass effect
            waves.forEach((wave, index) => {
                ctx.save();

                // Apply moderate blur (frosted glass effect is applied via CSS)
                ctx.filter = `blur(${wave.blur}px) saturate(150%)`;

                // Use additive blending for realistic aurora glow
                ctx.globalCompositeOperation = 'lighter';

                ctx.beginPath();

                // Draw smooth flowing wave
                for (let x = 0; x < canvas.width; x += 3) {
                    // Create gentle, flowing wave pattern
                    const y1 = canvas.height * 0.35 +
                               Math.sin(x * wave.frequency + time * wave.speed) * wave.amplitude +
                               Math.sin(x * wave.frequency * 1.5 + time * wave.speed * 0.8) * (wave.amplitude * 0.4) +
                               Math.sin(time * 0.003 + index) * 30; // Slow vertical drift

                    if (x === 0) {
                        ctx.moveTo(x, y1);
                    } else {
                        ctx.lineTo(x, y1);
                    }
                }

                // Complete the shape
                ctx.lineTo(canvas.width, canvas.height);
                ctx.lineTo(0, canvas.height);
                ctx.closePath();

                // Create soft gradient
                const gradient = ctx.createLinearGradient(0, canvas.height * 0.2, 0, canvas.height);
                gradient.addColorStop(0, wave.color);
                gradient.addColorStop(0.4, wave.color.replace(/[\d.]+\)/, '0.06)'));
                gradient.addColorStop(0.7, wave.color.replace(/[\d.]+\)/, '0.02)'));
                gradient.addColorStop(1, 'rgba(0, 0, 0, 0)');

                ctx.fillStyle = gradient;
                ctx.fill();

                ctx.restore();
            });

            time += 0.5;
            this.auroraBorealisAnimationFrame = requestAnimationFrame(drawAurora);
        };

        // Handle window resize
        const resizeCanvas = () => {
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
        };
        window.addEventListener('resize', resizeCanvas);

        drawAurora();
    }

    initColorShift() {
        // Color shifting background that lerps between warm and cold colors with gradients
        const animatedBg = document.querySelector('.animated-background');
        if (!animatedBg) return;
        if (this.colorShiftAnimationFrame) return; // Already initialized

        const canvas = document.createElement('canvas');
        canvas.id = 'colorShiftCanvas';
        canvas.style.position = 'fixed';
        canvas.style.top = '0';
        canvas.style.left = '0';
        canvas.style.width = '100%';
        canvas.style.height = '100%';
        canvas.style.zIndex = '0';
        canvas.style.pointerEvents = 'none';
        animatedBg.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;

        let time = 0;

        // Dusk colors (deep purples, oranges, pinks)
        const duskColors = [
            { r: 255, g: 94, b: 77 },   // Sunset Red
            { r: 253, g: 121, b: 168 }, // Pink
            { r: 142, g: 68, b: 173 },  // Purple
            { r: 241, g: 90, b: 36 }    // Deep Orange
        ];

        // Dawn colors (soft yellows, light blues, pastels)
        const dawnColors = [
            { r: 255, g: 223, b: 186 }, // Soft Peach
            { r: 179, g: 229, b: 252 }, // Light Blue
            { r: 255, g: 250, b: 205 }, // Lemon Chiffon
            { r: 173, g: 216, b: 230 }  // Light Sky Blue
        ];

        // Interpolate between two colors
        const lerpColor = (color1, color2, t) => {
            return {
                r: Math.round(color1.r + (color2.r - color1.r) * t),
                g: Math.round(color1.g + (color2.g - color1.g) * t),
                b: Math.round(color1.b + (color2.b - color1.b) * t)
            };
        };

        const drawColorShift = () => {
            // Create multiple cycles for more dynamic color transitions
            const mainCycle = (Math.sin(time * 0.0004) + 1) / 2; // Primary dusk/dawn cycle
            const secondaryCycle = (Math.sin(time * 0.0003 + Math.PI / 4) + 1) / 2; // Secondary offset cycle
            const tertiaryCycle = (Math.sin(time * 0.0002 + Math.PI / 2) + 1) / 2; // Tertiary cycle

            // Determine which phase we're in and create color combinations
            const colors = [];

            // Create 4 different color stops with complex interpolation
            for (let i = 0; i < 4; i++) {
                const duskIndex1 = i % duskColors.length;
                const duskIndex2 = (i + 1) % duskColors.length;
                const dawnIndex1 = i % dawnColors.length;
                const dawnIndex2 = (i + 1) % dawnColors.length;

                // First interpolate within color sets
                const duskMix = lerpColor(duskColors[duskIndex1], duskColors[duskIndex2], secondaryCycle);
                const dawnMix = lerpColor(dawnColors[dawnIndex1], dawnColors[dawnIndex2], tertiaryCycle);

                // Then interpolate between dusk and dawn
                colors.push(lerpColor(duskMix, dawnMix, mainCycle));
            }

            // Clear canvas
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // Create animated radial gradient
            const gradientOffsetX = Math.cos(time * 0.0003) * (canvas.width * 0.1);
            const gradientOffsetY = Math.sin(time * 0.0003) * (canvas.height * 0.1);

            const gradient = ctx.createRadialGradient(
                canvas.width / 2 + gradientOffsetX,
                canvas.height / 2 + gradientOffsetY,
                0,
                canvas.width / 2 + gradientOffsetX,
                canvas.height / 2 + gradientOffsetY,
                Math.max(canvas.width, canvas.height) * 0.9
            );

            gradient.addColorStop(0, `rgba(${colors[0].r}, ${colors[0].g}, ${colors[0].b}, 0.6)`);
            gradient.addColorStop(0.33, `rgba(${colors[1].r}, ${colors[1].g}, ${colors[1].b}, 0.5)`);
            gradient.addColorStop(0.66, `rgba(${colors[2].r}, ${colors[2].g}, ${colors[2].b}, 0.4)`);
            gradient.addColorStop(1, `rgba(${colors[3].r}, ${colors[3].g}, ${colors[3].b}, 0.35)`);

            ctx.fillStyle = gradient;
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            // Add multiple rotating linear gradient overlays for complex effects
            for (let layer = 0; layer < 2; layer++) {
                const angle = time * 0.0002 * (layer + 1) + (layer * Math.PI / 3);
                const distance = Math.max(canvas.width, canvas.height) * 0.7;

                const x1 = canvas.width / 2 + Math.cos(angle) * distance;
                const y1 = canvas.height / 2 + Math.sin(angle) * distance;
                const x2 = canvas.width / 2 - Math.cos(angle) * distance;
                const y2 = canvas.height / 2 - Math.sin(angle) * distance;

                const linearGradient = ctx.createLinearGradient(x1, y1, x2, y2);

                const colorIndex1 = (layer * 2) % 4;
                const colorIndex2 = (layer * 2 + 1) % 4;
                const colorIndex3 = (layer * 2 + 2) % 4;

                linearGradient.addColorStop(0, `rgba(${colors[colorIndex1].r}, ${colors[colorIndex1].g}, ${colors[colorIndex1].b}, 0.25)`);
                linearGradient.addColorStop(0.5, `rgba(${colors[colorIndex2].r}, ${colors[colorIndex2].g}, ${colors[colorIndex2].b}, 0.15)`);
                linearGradient.addColorStop(1, `rgba(${colors[colorIndex3].r}, ${colors[colorIndex3].g}, ${colors[colorIndex3].b}, 0.25)`);

                ctx.fillStyle = linearGradient;
                ctx.fillRect(0, 0, canvas.width, canvas.height);
            }

            // Adapt font color based on background brightness
            // Calculate average color brightness (center color for simplicity)
            const avgColor = colors[0]; // Use center color
            const brightness = (avgColor.r * 0.299 + avgColor.g * 0.587 + avgColor.b * 0.114);

            // If background is bright (dawn colors), use dark text; if dark (dusk colors), use light text
            const textColor = brightness > 180 ? '#000000' : '#ffffff';

            // Apply text color to chorus text elements
            const textLines = document.querySelectorAll('.text-line');
            textLines.forEach(line => {
                line.style.color = textColor;
            });

            const chorusTitle = document.getElementById('chorusTitle');
            const chorusKey = document.getElementById('chorusKey');
            if (chorusTitle) chorusTitle.style.color = textColor;
            if (chorusKey) chorusKey.style.color = textColor;

            time += 1;
            this.colorShiftAnimationFrame = requestAnimationFrame(drawColorShift);
        };

        // Handle window resize
        const resizeCanvas = () => {
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
        };
        window.addEventListener('resize', resizeCanvas);

        drawColorShift();
    }

    async loadChoruses() {
        try {
            // Check if we're on a chorus display page
            if (!window.chorusData) {
                console.log('Not on chorus display page, skipping chorus loading');
                return; // Not on a chorus display page, exit early
            }

            console.log('Loading choruses for display page');

            // First, try to load from sessionStorage
            const storedChorusList = sessionStorage.getItem('chorusList');
            const storedCurrentChorusId = sessionStorage.getItem('currentChorusId');

            if (storedChorusList) {
                try {
                    this.choruses = JSON.parse(storedChorusList);
                    console.log(`Loaded ${this.choruses.length} choruses from sessionStorage`);

                    // Find current chorus index
                    const currentId = window.chorusData?.id || storedCurrentChorusId;
                    if (currentId) {
                        this.currentChorusIndex = this.choruses.findIndex(c => c && c.id === currentId);
                        if (this.currentChorusIndex === -1) {
                            this.currentChorusIndex = 0;
                        }
                    }

                    this.updateNavigationButtons();
                    return;
                } catch (e) {
                    console.error('Error parsing stored chorus list:', e);
                    // Fall through to fetch from server
                }
            }

            // If no stored list, get all choruses for navigation
            const response = await fetch('/Home/Search?q=*');
            const data = await response.json();
            this.choruses = data.results || [];
            console.log(`Loaded ${this.choruses.length} choruses from server`);

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
        console.log('Setting up event listeners...');
        console.log('window.chorusData:', window.chorusData);
        console.log('window.location.pathname:', window.location.pathname);
        console.log('Includes /ChorusDisplay/:', window.location.pathname.includes('/ChorusDisplay/'));

        // Navigation buttons for pages
        const prevPageBtn = document.getElementById('prevPageBtn');
        const nextPageBtn = document.getElementById('nextPageBtn');

        // Backward compatibility: check for old button IDs
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');

        const printBtn = document.getElementById('printBtn');
        const closeBtn = document.getElementById('closeBtn');
        const increaseFontBtn = document.getElementById('increaseFontBtn');
        const decreaseFontBtn = document.getElementById('decreaseFontBtn');

        // Navigation buttons for choruses
        const prevChorusBtn = document.getElementById('prevChorusBtn');
        const nextChorusBtn = document.getElementById('nextChorusBtn');

        console.log('Navigation buttons found:', { prevPageBtn, nextPageBtn, prevBtn, nextBtn, prevChorusBtn, nextChorusBtn, printBtn, closeBtn, increaseFontBtn, decreaseFontBtn });

        // Only add event listeners if the elements exist (they won't on the search page)
        // Page navigation (if separate page buttons exist)
        if (prevPageBtn) prevPageBtn.addEventListener('click', () => this.navigate(-1));
        if (nextPageBtn) nextPageBtn.addEventListener('click', () => this.navigate(1));

        // Main navigation buttons (prevBtn/nextBtn) - use for page navigation
        // These are the primary left/right arrow buttons in ChorusDisplay
        if (prevBtn) prevBtn.addEventListener('click', () => this.navigate(-1));
        if (nextBtn) nextBtn.addEventListener('click', () => this.navigate(1));

        // Chorus navigation (separate chorus buttons if they exist)
        if (prevChorusBtn) prevChorusBtn.addEventListener('click', () => this.navigateChorus(-1));
        if (nextChorusBtn) nextChorusBtn.addEventListener('click', () => this.navigateChorus(1));
        if (printBtn) printBtn.addEventListener('click', () => this.print());
        if (closeBtn) closeBtn.addEventListener('click', () => this.close());
        if (increaseFontBtn) increaseFontBtn.addEventListener('click', () => this.increaseFontSize());
        if (decreaseFontBtn) decreaseFontBtn.addEventListener('click', () => this.decreaseFontSize());

        // Always add resize listener if we're on a chorus display page
        if (window.chorusData || window.location.pathname.includes('/ChorusDisplay/')) {
            console.log('Setting up resize listener for chorus display page');
            window.addEventListener('resize', () => {
                console.log('Resize event fired!');
                this.handleResize();
            });

            // Also add keyboard shortcuts
            console.log('Setting up keyboard listener for chorus display page');
            document.addEventListener('keydown', (e) => {
                console.log('Keyboard event captured:', e.key);
                this.handleKeyboard(e);
            });
        } else {
            console.log('Not on chorus display page, skipping resize listener');
        }
    }
    
    handleKeyboard(e) {
        console.log('Keyboard event:', e.key);
        switch (e.key) {
            case 'ArrowLeft':
                e.preventDefault();
                // If holding Ctrl/Cmd, navigate between choruses, otherwise between pages
                if (e.ctrlKey || e.metaKey) {
                    this.navigateChorus(-1);
                } else {
                    this.navigate(-1);
                }
                break;
            case 'ArrowRight':
                e.preventDefault();
                // If holding Ctrl/Cmd, navigate between choruses, otherwise between pages
                if (e.ctrlKey || e.metaKey) {
                    this.navigateChorus(1);
                } else {
                    this.navigate(1);
                }
                break;
            case 'ArrowUp':
                e.preventDefault();
                this.navigateChorus(-1);
                break;
            case 'ArrowDown':
                e.preventDefault();
                this.navigateChorus(1);
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
                console.log('Plus/Equals key pressed!');
                e.preventDefault();
                this.increaseFontSize();
                break;
            case '-':
                console.log('Minus key pressed!');
                e.preventDefault();
                this.decreaseFontSize();
                break;
        }
    }
    
    async navigate(direction) {
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
            // If there's only one page and we have multiple choruses, navigate to next/previous chorus
            if (this.choruses && this.choruses.length > 1) {
                await this.navigateChorus(direction);
            } else {
                // Single page, single chorus - no navigation possible
                this.showNotification('This chorus has only one page', 'info');
            }
        }
    }

    async navigateChorus(direction) {
        console.log('=== NAVIGATE CHORUS CALLED ===');
        console.log('Direction:', direction);
        console.log('Current choruses array:', this.choruses);
        console.log('Choruses length:', this.choruses ? this.choruses.length : 'null');
        console.log('Current chorus index:', this.currentChorusIndex);

        if (!this.choruses || this.choruses.length <= 1) {
            console.log('Not enough choruses to navigate');
            this.showNotification('No other choruses available', 'info');
            return;
        }

        // Calculate new index
        let newIndex = this.currentChorusIndex + direction;

        // Wrap around if necessary
        if (newIndex < 0) {
            newIndex = this.choruses.length - 1;
        } else if (newIndex >= this.choruses.length) {
            newIndex = 0;
        }

        console.log('New index:', newIndex);

        // Load the new chorus
        const chorus = this.choruses[newIndex];

        console.log('Chorus to load:', chorus);

        if (!chorus) {
            console.error('Chorus not found at index:', newIndex);
            return;
        }

        // Update sessionStorage with new current chorus ID before navigating
        sessionStorage.setItem('currentChorusId', chorus.id);

        // Navigate to the new chorus URL (full page load)
        // This is necessary because the chorus data comes from the server render
        window.location.href = `/Home/ChorusDisplay/${chorus.id}`;
    }

    // Set the chorus list for navigation (can be called from UI)
    setChorusList(chorusList, currentChorusId = null) {
        this.choruses = chorusList || [];

        // Find current chorus index if ID provided
        if (currentChorusId) {
            this.currentChorusIndex = this.choruses.findIndex(c => c && c.id === currentChorusId);
            if (this.currentChorusIndex === -1) {
                this.currentChorusIndex = 0;
            }
        }

        this.updateNavigationButtons();
        console.log(`Chorus list set: ${this.choruses.length} choruses, current index: ${this.currentChorusIndex}`);
    }

    // Get the current chorus list
    getChorusList() {
        return this.choruses;
    }

    // Get the current chorus index
    getCurrentChorusIndex() {
        return this.currentChorusIndex;
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
            
            // Fallback to loading the ChorusDisplay page
            const detailResponse = await fetch(`/Home/ChorusDisplay/${chorusId}`);
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
        console.log('getKeyDisplay called with:', keyValue);
        console.log('keyValue type:', typeof keyValue);
        
        // Handle different key formats
        if (keyValue === null || keyValue === undefined || keyValue === '') {
            console.log('Key value is null/undefined/empty, returning "Not Set"');
            return 'Not Set';
        }
        
        // If it's already a string and looks like a key, return it
        if (typeof keyValue === 'string' && keyValue.trim() !== '') {
            console.log('Key value is string:', keyValue);
            return keyValue.trim();
        }
        
        // If it's a number, convert to key
        if (typeof keyValue === 'number' || !isNaN(parseInt(keyValue))) {
            const numValue = parseInt(keyValue);
            console.log('Key value as number:', numValue);
            const keys = ['Not Set', 'C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B', 'C♭', 'D♭', 'E♭', 'F♭', 'G♭', 'A♭', 'B♭'];
            const result = keys[numValue] || 'Not Set';
            console.log('Converted number to key:', result);
            return result;
        }
        
        console.log('Could not process key value, returning "Not Set"');
        return 'Not Set';
    }
    
    updateDisplay(chorusData) {
        console.log('Updating display with chorus data:', chorusData);
        console.log('Key value received:', chorusData.key);
        console.log('Key type:', typeof chorusData.key);

        // Update title and key
        const titleElement = document.getElementById('chorusTitle');
        const keyElement = document.getElementById('chorusKey');

        if (titleElement) titleElement.textContent = chorusData.name;

        // Debug the key display
        const keyDisplay = this.getKeyDisplay(chorusData.key);
        console.log('Key display result:', keyDisplay);

        if (keyElement) keyElement.textContent = keyDisplay;

        // Parse chorus text into pages based on [PAGE] markers
        this.parseChorusPages(chorusData.text);

        // Initialize display
        this.currentPage = 0;
        this.currentFontSize = 86; // Start with 86px font size
        console.log('Setting initial font size to 86px');

        // Calculate initial layout
        this.calculateWrappedLines();
        this.calculateLinesPerPage();

        // Show page indicator if multiple pages
        this.updatePageIndicator();

        // Display the first page
        this.displayCurrentPage();

        // Update navigation buttons
        this.updateNavigationButtons();

        // Apply initial font size
        this.applyFontSize();
        console.log('Applied font size:', this.currentFontSize, 'px');

        // Trigger resize event to ensure proper font size application
        console.log('Triggering resize event to ensure proper font size application');
        this.handleResize();

        console.log(`Display initialized: ${this.totalPages} pages, ${this.linesPerPage} lines per page`);
    }

    parseChorusPages(text) {
        // Check if text contains explicit page breaks [PAGE]
        if (text.includes('[PAGE]')) {
            // Split by [PAGE] marker
            const pages = text.split('[PAGE]');
            this.explicitPages = pages.map(page =>
                page.split('\n').filter(line => line.trim() !== '')
            );
            this.hasExplicitPageBreaks = true;
            console.log(`Found ${this.explicitPages.length} explicit pages`);
        } else {
            // No explicit page breaks, use automatic pagination
            this.currentChorusLines = text.split('\n').filter(line => line.trim() !== '');
            this.hasExplicitPageBreaks = false;
            console.log(`Parsed ${this.currentChorusLines.length} lines from chorus text (automatic pagination)`);
        }
    }
    
    // Update page indicator
    updatePageIndicator() {
        const pageIndicator = document.getElementById('pageIndicator');
        if (!pageIndicator) return;
        
        if (this.totalPages > 1) {
            pageIndicator.textContent = `Page ${this.currentPage + 1} of ${this.totalPages}`;
            pageIndicator.style.display = 'block';
        } else {
            pageIndicator.style.display = 'none';
        }
    }
    
    // Auto-fit text to fill the screen optimally
    autoFitText() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        // Get available space
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        
        console.log(`Container size: ${containerWidth}x${containerHeight}`);
        
        // Don't change font size - just recalculate pagination with current font size
        this.applyFontSize();
        this.calculateLinesPerPage();
        
        // Ensure current page is valid after resize
        if (this.currentPage >= this.totalPages) {
            this.currentPage = this.totalPages - 1;
        }
        if (this.currentPage < 0) {
            this.currentPage = 0;
        }
        
        // Display the current page
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
        
        // If we have more pages than before, adjust current page
        if (this.currentPage >= this.totalPages) {
            this.currentPage = Math.max(0, this.totalPages - 1);
        }
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
    
    // Display the current page
    displayCurrentPage() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        // Ensure current page is within bounds
        if (this.currentPage >= this.totalPages) {
            this.currentPage = this.totalPages - 1;
        }
        if (this.currentPage < 0) {
            this.currentPage = 0;
        }
        
        // Get lines for current page
        const linesForPage = this.getLinesForPage(this.currentPage);
        
        console.log(`Displaying page ${this.currentPage + 1}/${this.totalPages} with ${linesForPage.length} lines:`, linesForPage);
        
        // Clear container
        container.innerHTML = '';
        
        // Calculate line height - use tighter spacing if we have multiple pages
        let lineHeight = this.currentFontSize * 1.5; // Default 1.5 line height ratio
        if (this.currentChorusLines.length > this.linesPerPage && this.totalPages > 1) {
            lineHeight = this.currentFontSize * 1.4; // Tighter spacing for better space utilization
        }
        
        // Get font from settings
        const chorusFont = sessionStorage.getItem('chorusFont') || 'Inter';

        // Create and display lines
        linesForPage.forEach(line => {
            const lineElement = document.createElement('div');
            lineElement.className = 'text-line';
            lineElement.textContent = line;
            // Apply current font size and color to the new element
            lineElement.style.fontSize = `${this.currentFontSize}px`;
            lineElement.style.lineHeight = `${lineHeight}px`;
            lineElement.style.color = 'white'; // Ensure white color is applied
            lineElement.style.textAlign = 'center'; // Ensure centering
            lineElement.style.zIndex = '25'; // Ensure text stays above other elements
            lineElement.style.position = 'relative'; // Required for z-index to work
            lineElement.style.fontFamily = `'${chorusFont}', sans-serif`; // Apply font family
            container.appendChild(lineElement);
        });
        
        // Update page indicator
        this.updatePageIndicator();
        
        console.log(`Displayed ${linesForPage.length} lines on page ${this.currentPage + 1}/${this.totalPages}`);
    }
    
    // Get lines for a specific page
    getLinesForPage(pageIndex) {
        if (pageIndex < 0 || pageIndex >= this.totalPages) {
            console.log(`Invalid page index: ${pageIndex}, total pages: ${this.totalPages}`);
            return [];
        }

        // If we have explicit page breaks, return the explicit page
        if (this.hasExplicitPageBreaks) {
            const pageLines = this.explicitPages[pageIndex] || [];
            console.log(`Page ${pageIndex + 1}: Explicit page with ${pageLines.length} lines`);
            return pageLines;
        }

        // Calculate which original lines should be on this page
        const startLineIndex = pageIndex * this.linesPerPage;
        const endLineIndex = Math.min(startLineIndex + this.linesPerPage, this.currentChorusLines.length);

        // Get the original lines for this page
        const pageLines = [];
        for (let i = startLineIndex; i < endLineIndex; i++) {
            if (this.currentChorusLines[i]) {
                pageLines.push(this.currentChorusLines[i]);
            }
        }

        console.log(`Page ${pageIndex + 1}: Original lines ${startLineIndex + 1}-${endLineIndex} of ${this.currentChorusLines.length} total lines`);
        console.log(`Page ${pageIndex + 1}: Returning ${pageLines.length} lines:`, pageLines);
        return pageLines;
    }
    
    // Update navigation buttons
    updateNavigationButtons() {
        // Update chorus navigation buttons
        const prevChorusBtn = document.getElementById('prevChorusBtn');
        const nextChorusBtn = document.getElementById('nextChorusBtn');

        if (prevChorusBtn && nextChorusBtn) {
            // Show chorus navigation buttons if we have multiple choruses
            if (this.choruses && this.choruses.length > 1) {
                prevChorusBtn.style.display = 'flex';
                nextChorusBtn.style.display = 'flex';

                // Enable/disable based on current position (no wrapping for buttons)
                prevChorusBtn.disabled = this.currentChorusIndex <= 0;
                nextChorusBtn.disabled = this.currentChorusIndex >= this.choruses.length - 1;

                // Update button styles based on disabled state
                if (prevChorusBtn.disabled) {
                    prevChorusBtn.style.opacity = '0.5';
                    prevChorusBtn.style.cursor = 'not-allowed';
                } else {
                    prevChorusBtn.style.opacity = '1';
                    prevChorusBtn.style.cursor = 'pointer';
                }

                if (nextChorusBtn.disabled) {
                    nextChorusBtn.style.opacity = '0.5';
                    nextChorusBtn.style.cursor = 'not-allowed';
                } else {
                    nextChorusBtn.style.opacity = '1';
                    nextChorusBtn.style.cursor = 'pointer';
                }
            } else {
                // Hide chorus navigation buttons if only one chorus
                prevChorusBtn.style.display = 'none';
                nextChorusBtn.style.display = 'none';
            }

            console.log(`Chorus navigation buttons updated: ${this.choruses ? this.choruses.length : 0} choruses, current index: ${this.currentChorusIndex}`);
        }

        // Update page navigation buttons
        const prevPageBtn = document.getElementById('prevPageBtn');
        const nextPageBtn = document.getElementById('nextPageBtn');

        if (prevPageBtn && nextPageBtn) {
            // Show page navigation buttons if we have multiple pages
            if (this.totalPages > 1) {
                prevPageBtn.style.display = 'flex';
                nextPageBtn.style.display = 'flex';

                // Page navigation wraps around, so buttons are always enabled
                prevPageBtn.disabled = false;
                nextPageBtn.disabled = false;
                prevPageBtn.style.opacity = '1';
                prevPageBtn.style.cursor = 'pointer';
                nextPageBtn.style.opacity = '1';
                nextPageBtn.style.cursor = 'pointer';
            } else {
                // Hide page navigation buttons if only one page
                prevPageBtn.style.display = 'none';
                nextPageBtn.style.display = 'none';
            }

            console.log(`Page navigation buttons updated: ${this.totalPages} pages, current page: ${this.currentPage + 1}`);
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
        console.log('=== INCREASE FONT SIZE CALLED ===');
        console.log('Current font size:', this.currentFontSize);
        console.log('Current lines per page:', this.linesPerPage);
        console.log('Current total pages:', this.totalPages);
        console.log('Total chorus lines:', this.currentChorusLines.length);
        
        if (this.currentFontSize < this.maxFontSize) {
            this.currentFontSize += this.fontSizeStep;
            
            console.log(`Increasing font size to ${this.currentFontSize}px`);
            
            // Recalculate lines per page first
            this.calculateLinesPerPage();
            
            // Ensure current page is valid
            if (this.currentPage >= this.totalPages) {
                this.currentPage = this.totalPages - 1;
            }
            
            // Display the current page (this creates new elements)
            this.displayCurrentPage();
            
            // Apply font size to the newly created elements
            this.applyFontSize();
            
            // Update UI elements
            this.updateNavigationButtons();
            this.updatePageIndicator();
            
            console.log('=== AFTER INCREASE ===');
            console.log('New font size:', this.currentFontSize);
            console.log('New lines per page:', this.linesPerPage);
            console.log('New total pages:', this.totalPages);
            
            this.showNotification(`Font size: ${this.currentFontSize}px, Lines per page: ${this.linesPerPage}, Pages: ${this.totalPages}`, 'info');
        } else {
            this.showNotification('Maximum font size reached', 'warning');
        }
    }
    
    decreaseFontSize() {
        console.log('=== DECREASE FONT SIZE CALLED ===');
        console.log('Current font size:', this.currentFontSize);
        console.log('Current lines per page:', this.linesPerPage);
        console.log('Current total pages:', this.totalPages);
        console.log('Total chorus lines:', this.currentChorusLines.length);
        
        if (this.currentFontSize > this.minFontSize) {
            this.currentFontSize -= this.fontSizeStep;
            
            console.log(`Decreasing font size to ${this.currentFontSize}px`);
            
            // Recalculate lines per page first
            this.calculateLinesPerPage();
            
            // Ensure current page is valid
            if (this.currentPage >= this.totalPages) {
                this.currentPage = this.totalPages - 1;
            }
            
            // Display the current page (this creates new elements)
            this.displayCurrentPage();
            
            // Apply font size to the newly created elements
            this.applyFontSize();
            
            // Update UI elements
            this.updateNavigationButtons();
            this.updatePageIndicator();
            
            console.log('=== AFTER DECREASE ===');
            console.log('New font size:', this.currentFontSize);
            console.log('New lines per page:', this.linesPerPage);
            console.log('New total pages:', this.totalPages);
            
            this.showNotification(`Font size: ${this.currentFontSize}px, Lines per page: ${this.linesPerPage}, Pages: ${this.totalPages}`, 'info');
        } else {
            this.showNotification('Minimum font size reached', 'warning');
        }
    }
    
    // Recalculate everything and redisplay optimally
    recalculateAndRedisplay() {
        // Recalculate wrapped lines with new font size
        this.calculateWrappedLines();
        
        // Recalculate lines per page
        this.calculateLinesPerPage();
        
        // Update navigation buttons
        this.updateNavigationButtons();
        
        console.log(`Recalculated: Font size ${this.currentFontSize}px, Lines per page: ${this.linesPerPage}, Total pages: ${this.totalPages}`);
    }
    
    // Optimize font size to fill screen with current font size
    optimizeFontSizeForCurrentFont() {
        const container = document.querySelector('.chorus-content');
        if (!container) return;
        
        const containerHeight = container.clientHeight;
        const containerWidth = container.clientWidth;
        
        // Calculate how many lines we can fit with current font size
        const maxLines = Math.floor((containerHeight - 40) / (this.currentFontSize * 1.5));
        
        // If we can fit all text on one page, maximize font size to fill screen
        if (this.currentChorusLines.length <= maxLines) {
            // Try to increase font size while keeping everything on one page
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
        } else {
            // Multiple pages needed, optimize font size for best screen usage
            // Try to increase font size while maintaining reasonable page distribution
            while (this.currentFontSize < this.maxFontSize) {
                const testFontSize = this.currentFontSize + this.fontSizeStep;
                const testLineHeight = testFontSize * 1.5;
                const testLinesPerPage = Math.floor((containerHeight - 40) / testLineHeight);
                
                // Only increase if we maintain reasonable page distribution (4-12 lines per page)
                if (testLinesPerPage >= 4 && testLinesPerPage <= 12) {
                    this.currentFontSize = testFontSize;
                    this.applyFontSize();
                    this.calculateLinesPerPage();
                } else {
                    break;
                }
            }
        }
        
        console.log(`Optimized font size: ${this.currentFontSize}px, Pages: ${this.totalPages}, Lines per page: ${this.linesPerPage}`);
    }
    
    // Calculate how many lines fit per page with current font size
    calculateLinesPerPage() {
        console.log('=== CALCULATE LINES PER PAGE ===');

        const container = document.querySelector('.chorus-content');
        if (!container) {
            console.log('Container not found!');
            return;
        }

        // If we have explicit page breaks, use them
        if (this.hasExplicitPageBreaks) {
            this.totalPages = this.explicitPages.length;
            console.log(`Using explicit page breaks: ${this.totalPages} pages`);
            this.updatePageIndicator();
            return;
        }

        // Get the parent container (chorus-content-wrapper) for fixed height
        const parentContainer = container.parentElement;
        const containerHeight = parentContainer ? parentContainer.clientHeight : container.clientHeight;
        const lineHeight = this.currentFontSize * 1.5; // 1.5 line height ratio

        // Account for padding and margins
        const computedStyle = window.getComputedStyle(container);
        const paddingTop = parseFloat(computedStyle.paddingTop);
        const paddingBottom = parseFloat(computedStyle.paddingBottom);
        const marginTop = parseFloat(computedStyle.marginTop);
        const marginBottom = parseFloat(computedStyle.marginBottom);

        const availableHeight = containerHeight - paddingTop - paddingBottom - marginTop - marginBottom;

        // Calculate how many lines can fit
        this.linesPerPage = Math.floor(availableHeight / lineHeight);

        // Ensure minimum of 1 line per page
        this.linesPerPage = Math.max(1, this.linesPerPage);

        // Calculate total pages needed based on original lines
        this.totalPages = Math.ceil(this.currentChorusLines.length / this.linesPerPage);

        // Ensure at least 1 page
        this.totalPages = Math.max(1, this.totalPages);

        console.log(`Parent container height: ${containerHeight}px`);
        console.log(`Available height: ${availableHeight}px`);
        console.log(`Line height: ${lineHeight}px`);
        console.log(`Font size: ${this.currentFontSize}px`);
        console.log(`Lines per page: ${this.linesPerPage}`);
        console.log(`Total original lines: ${this.currentChorusLines.length}`);
        console.log(`Total pages: ${this.totalPages}`);

        // Update page indicator immediately
        this.updatePageIndicator();
    }
    
    // Adjust current page if text is too large for the current page
    adjustPageIfNeeded() {
        if (!this.wrappedLinesPerOriginalLine) return;
        
        const startWrappedLine = this.currentPage * this.linesPerPage;
        const endWrappedLine = startWrappedLine + this.linesPerPage;
        
        // Check if current page has any content
        const linesToShow = this.getLinesForPage(this.currentPage);
        
        // If no lines to show and we're not on the last page, move to next page
        if (linesToShow.length === 0 && this.currentPage < this.totalPages - 1) {
            this.currentPage++;
            console.log(`Adjusted to page ${this.currentPage + 1} due to font size change`);
        }
    }
    
    // Apply font size to the text
    applyFontSize() {
        const chorusText = document.querySelector('.chorus-text');
        if (!chorusText) return;
        
        // Calculate line height - use tighter spacing if we have multiple pages
        let lineHeight = this.currentFontSize * 1.5; // Default 1.5 line height ratio
        if (this.currentChorusLines.length > this.linesPerPage && this.totalPages > 1) {
            lineHeight = this.currentFontSize * 1.4; // Tighter spacing for better space utilization
        }
        
        // Apply font size to the chorus text container
        chorusText.style.fontSize = `${this.currentFontSize}px`;
        chorusText.style.lineHeight = `${lineHeight}px`;
        chorusText.style.color = 'white'; // Ensure white color
        chorusText.style.textAlign = 'center'; // Ensure centering
        
        // Also apply to individual text lines for consistency
        const textLines = document.querySelectorAll('.text-line');
        textLines.forEach(line => {
            line.style.fontSize = `${this.currentFontSize}px`;
            line.style.lineHeight = `${lineHeight}px`;
            line.style.color = 'white'; // Ensure white color
            line.style.textAlign = 'center'; // Ensure centering
        });
        
        console.log(`Applied font size: ${this.currentFontSize}px with line height: ${lineHeight}px`);
    }
    
    // Handle window resize
    handleResize() {
        console.log('handleResize called!');
        // Debounce resize events
        clearTimeout(this.resizeTimeout);
        this.resizeTimeout = setTimeout(() => {
            console.log('Resize timeout fired, calling autoFitText');
            if (this.currentChorusLines.length > 0) {
                // Recalculate optimal font size to fill the screen
                this.autoFitText();
            }
        }, 250);
    }

    // Setup auto-hide buttons on mouse inactivity
    setupAutoHideButtons() {
        console.log('Setting up auto-hide buttons...');

        // Get all button containers
        this.buttonElements = {
            navButtons: document.querySelectorAll('.nav-btn'),
            controlsContainer: document.querySelector('.controls-container'),
            pageIndicator: document.querySelector('.page-indicator')
        };

        // Start the hide timer
        this.resetHideTimer();

        // Listen for mouse movement
        document.addEventListener('mousemove', () => {
            this.showButtons();
            this.resetHideTimer();
        });

        // Also show buttons on any interaction
        document.addEventListener('click', () => {
            this.showButtons();
            this.resetHideTimer();
        });

        console.log('Auto-hide buttons initialized');
    }

    // Reset the hide timer
    resetHideTimer() {
        // Clear existing timeout
        if (this.hideButtonsTimeout) {
            clearTimeout(this.hideButtonsTimeout);
        }

        // Set new timeout to hide buttons
        this.hideButtonsTimeout = setTimeout(() => {
            this.hideButtons();
        }, this.hideDelay);
    }

    // Show all buttons with fade-in
    showButtons() {
        if (this.buttonsVisible) return;

        this.buttonsVisible = true;

        // Show navigation buttons
        if (this.buttonElements.navButtons) {
            this.buttonElements.navButtons.forEach(btn => {
                btn.style.opacity = '1';
                btn.style.transition = 'opacity 0.3s ease-in';
                btn.style.pointerEvents = 'auto';
            });
        }

        // Show controls container
        if (this.buttonElements.controlsContainer) {
            this.buttonElements.controlsContainer.style.opacity = '1';
            this.buttonElements.controlsContainer.style.transition = 'opacity 0.3s ease-in';
            this.buttonElements.controlsContainer.style.pointerEvents = 'auto';
        }

        // Show page indicator
        if (this.buttonElements.pageIndicator) {
            this.buttonElements.pageIndicator.style.opacity = '1';
            this.buttonElements.pageIndicator.style.transition = 'opacity 0.3s ease-in';
        }

        console.log('Buttons shown');
    }

    // Hide all buttons with fade-out
    hideButtons() {
        if (!this.buttonsVisible) return;

        this.buttonsVisible = false;

        // Hide navigation buttons
        if (this.buttonElements.navButtons) {
            this.buttonElements.navButtons.forEach(btn => {
                btn.style.opacity = '0';
                btn.style.transition = 'opacity 0.5s ease-out';
                btn.style.pointerEvents = 'none';
            });
        }

        // Hide controls container
        if (this.buttonElements.controlsContainer) {
            this.buttonElements.controlsContainer.style.opacity = '0';
            this.buttonElements.controlsContainer.style.transition = 'opacity 0.5s ease-out';
            this.buttonElements.controlsContainer.style.pointerEvents = 'none';
        }

        // Hide page indicator
        if (this.buttonElements.pageIndicator) {
            this.buttonElements.pageIndicator.style.opacity = '0';
            this.buttonElements.pageIndicator.style.transition = 'opacity 0.5s ease-out';
        }

        console.log('Buttons hidden after inactivity');
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOMContentLoaded event fired');
    console.log('window.chorusData:', window.chorusData);
    console.log('window.location.pathname:', window.location.pathname);
    console.log('Includes /ChorusDisplay/:', window.location.pathname.includes('/ChorusDisplay/'));

    // Only initialize ChorusDisplay if we're on a chorus display page
    // Check if we have chorus data or if we're on a ChorusDisplay page
    if (window.chorusData || window.location.pathname.includes('/ChorusDisplay/')) {
        console.log('Creating ChorusDisplay instance...');
        window.chorusDisplay = new ChorusDisplay();
        console.log('ChorusDisplay instance created and stored in window.chorusDisplay');
    } else {
        console.log('Not on chorus display page, skipping ChorusDisplay initialization');
    }
});

// Show loading state while initializing (only on chorus display pages)
if (window.chorusData || window.location.pathname.includes('/ChorusDisplay/')) {
    document.body.classList.add('loading');
}

// Remove loading state after initialization
window.addEventListener('load', () => {
    document.body.classList.remove('loading');
});

// Global helper function to set the chorus list from the UI
window.setChorusList = function(chorusList, currentChorusId = null) {
    if (window.chorusDisplay) {
        window.chorusDisplay.setChorusList(chorusList, currentChorusId);
        console.log('Chorus list set via global function');
    } else {
        console.warn('ChorusDisplay instance not available. Make sure you are on a chorus display page.');
    }
};