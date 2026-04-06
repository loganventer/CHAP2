/**
 * ResponsiveInitializer - Application-level composition root for responsive features.
 *
 * Single Responsibility: Bootstrap and wire together the responsive/mobile system.
 * Creates instances of DeviceDetector, ChorusSyncService, and MobileChorusView,
 * injecting dependencies following the Dependency Inversion Principle.
 */
class ResponsiveInitializer {
    /**
     * Initialize the responsive system. Called at DOMContentLoaded.
     * Detects page context and wires appropriate components.
     */
    static init() {
        const CHAP2 = window.CHAP2 || {};

        // Create shared DeviceDetector
        const deviceDetector = new CHAP2.DeviceDetector();
        CHAP2.deviceDetector = deviceDetector;

        // Initialize page-specific features
        if (window.chorusData) {
            ResponsiveInitializer._initChorusDisplayPage(CHAP2, deviceDetector);
        }

        debug('[ResponsiveInitializer] Initialized. Mobile:', deviceDetector.isMobile());
    }

    /**
     * @private Initialize features specific to the ChorusDisplay page.
     */
    static _initChorusDisplayPage(CHAP2, deviceDetector) {
        // Create sync service with status callback
        const syncService = new CHAP2.ChorusSyncService({
            onStatusChange: (status) => {
                if (CHAP2.mobileChorusView) {
                    CHAP2.mobileChorusView.updateSyncStatus(status);
                }
            }
        });
        CHAP2.syncService = syncService;

        // Connect to SignalR hub
        syncService.connect();

        // Create MobileChorusView with injected dependencies (Composition)
        const mobileView = new CHAP2.MobileChorusView({
            deviceDetector: deviceDetector,
            syncService: syncService,
            chorusData: window.chorusData
        });
        mobileView.init();
        CHAP2.mobileChorusView = mobileView;

        // Listen for viewport changes to toggle mobile/desktop
        deviceDetector.onViewportChange((isMobile) => {
            debug('[ResponsiveInitializer] Viewport changed. Mobile:', isMobile);
            // CSS handles visibility via media queries.
            // We just need to manage focus and ARIA states.
            if (isMobile) {
                mobileView.show();
            } else {
                mobileView.hide();
            }
        });

        // Show the mobile sync URL label on desktop
        ResponsiveInitializer._populateMobileSyncLabel();

        // Hook into existing ChorusDisplay navigation to broadcast changes.
        // The desktop ChorusDisplay navigates via full page reload (window.location.href),
        // so we broadcast BEFORE the navigation happens.
        ResponsiveInitializer._hookDesktopNavigation(CHAP2, syncService);

        // Listen for remote chorus changes on desktop (triggers navigation)
        syncService.onChorusChanged((chorusId) => {
            // If we're on desktop, navigate to the new chorus
            if (!deviceDetector.isMobile()) {
                debug('[ResponsiveInitializer] Desktop received remote chorus change:', chorusId);
                window.location.href = `/Home/ChorusDisplay/${chorusId}`;
            }
        });
    }

    /**
     * @private Fetch the server's network IP and display it in the sync label.
     * Shows the URL that mobile devices should open to sync with the desktop.
     */
    static async _populateMobileSyncLabel() {
        const labelEl = document.getElementById('mobileSyncUrl');
        if (!labelEl) return;

        try {
            const response = await fetch('/Home/NetworkInfo');
            if (!response.ok) throw new Error('Failed to fetch network info');
            const data = await response.json();

            if (data.url) {
                const path = window.location.pathname;
                labelEl.textContent = `Mobile sync: ${data.url}${path}`;
                debug('[ResponsiveInitializer] Mobile sync URL:', data.url + path);
            } else {
                labelEl.textContent = 'Network unavailable';
            }
        } catch (err) {
            debug('[ResponsiveInitializer] Could not fetch network info:', err);
            labelEl.textContent = 'Network unavailable';
        }
    }

    /**
     * @private Hook into the existing ChorusDisplay.navigateChorus to broadcast sync events.
     * Uses a non-invasive monkey-patch on the prototype since ChorusDisplay
     * is already instantiated by the time this runs.
     */
    static _hookDesktopNavigation(CHAP2, syncService) {
        // Wait for ChorusDisplay instance to be available
        const chorusDisplay = window.chorusDisplay;
        if (!chorusDisplay || !chorusDisplay.navigateChorus) {
            debug('[ResponsiveInitializer] ChorusDisplay not found, skipping navigation hook.');
            return;
        }

        const originalNavigate = chorusDisplay.navigateChorus.bind(chorusDisplay);
        chorusDisplay.navigateChorus = async function(direction) {
            // Calculate the target chorus ID before navigation
            const choruses = chorusDisplay.choruses;
            if (choruses && choruses.length > 1) {
                let newIndex = chorusDisplay.currentChorusIndex + direction;
                if (newIndex < 0) newIndex = choruses.length - 1;
                if (newIndex >= choruses.length) newIndex = 0;
                const targetChorus = choruses[newIndex];
                if (targetChorus) {
                    // Broadcast before the page navigates away
                    syncService.broadcastChorusChange(targetChorus.id);
                }
            }
            // Call the original navigation (triggers page reload)
            return originalNavigate(direction);
        };

        debug('[ResponsiveInitializer] Desktop navigation hook installed.');
    }
}

// Register on namespace
window.CHAP2 = window.CHAP2 || {};
window.CHAP2.ResponsiveInitializer = ResponsiveInitializer;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Small delay to ensure ChorusDisplay has initialized first
    // (it also uses DOMContentLoaded, but loads earlier in script order)
    setTimeout(() => ResponsiveInitializer.init(), 0);
});
