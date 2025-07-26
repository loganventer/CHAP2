/**
 * System Restart functionality for CHAP2 Web Portal
 * Provides a way to restart all containers and the portal itself
 */

class SystemRestart {
    constructor() {
        this.isRestarting = false;
        this.init();
    }

    init() {
        // Add restart button to the page if it doesn't exist
        this.addRestartButton();
    }

    addRestartButton() {
        // Check if restart button already exists
        if (document.getElementById('system-restart-btn')) {
            return;
        }

        // Create restart button
        const restartBtn = document.createElement('button');
        restartBtn.id = 'system-restart-btn';
        restartBtn.className = 'btn btn-warning btn-sm';
        restartBtn.innerHTML = '<i class="fas fa-sync-alt"></i> Restart System';
        restartBtn.style.position = 'fixed';
        restartBtn.style.bottom = '20px';
        restartBtn.style.right = '20px';
        restartBtn.style.zIndex = '9999';
        restartBtn.style.display = 'none'; // Hidden by default

        // Add click handler
        restartBtn.addEventListener('click', (e) => {
            e.preventDefault();
            this.showRestartDialog();
        });

        // Add to page
        document.body.appendChild(restartBtn);

        // Show button only in development mode (you can customize this)
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            restartBtn.style.display = 'block';
        }
    }

    showRestartDialog() {
        if (this.isRestarting) {
            return;
        }

        const confirmation = prompt(
            '⚠️ SYSTEM RESTART WARNING ⚠️\n\n' +
            'This will restart all services:\n' +
            '• Qdrant Vector Database\n' +
            '• Ollama LLM Service\n' +
            '• LangChain Search Service\n' +
            '• Web Portal\n\n' +
            'Type "RESTART_ALL_SERVICES" to confirm:'
        );

        if (confirmation === 'RESTART_ALL_SERVICES') {
            this.executeRestart();
        } else if (confirmation !== null) {
            alert('Invalid confirmation code. Restart cancelled.');
        }
    }

    async executeRestart() {
        if (this.isRestarting) {
            return;
        }

        this.isRestarting = true;
        const restartBtn = document.getElementById('system-restart-btn');
        if (restartBtn) {
            restartBtn.disabled = true;
            restartBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Restarting...';
        }

        try {
            // Show progress dialog
            this.showProgressDialog();

            const response = await fetch('/api/restart-system', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    confirmation: 'RESTART_ALL_SERVICES'
                })
            });

            const result = await response.json();

            if (response.ok) {
                this.showSuccessDialog(result.message);
                
                // Wait a bit then redirect to show the restart
                setTimeout(() => {
                    window.location.reload();
                }, 3000);
            } else {
                this.showErrorDialog(result.error || 'Restart failed');
            }
        } catch (error) {
            console.error('Restart error:', error);
            this.showErrorDialog('Failed to restart system: ' + error.message);
        } finally {
            this.isRestarting = false;
            if (restartBtn) {
                restartBtn.disabled = false;
                restartBtn.innerHTML = '<i class="fas fa-sync-alt"></i> Restart System';
            }
        }
    }

    showProgressDialog() {
        // Remove existing dialog
        const existing = document.getElementById('restart-progress-dialog');
        if (existing) {
            existing.remove();
        }

        const dialog = document.createElement('div');
        dialog.id = 'restart-progress-dialog';
        dialog.className = 'modal fade show';
        dialog.style.display = 'block';
        dialog.style.backgroundColor = 'rgba(0,0,0,0.5)';
        dialog.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">🔄 System Restart in Progress</h5>
                    </div>
                    <div class="modal-body">
                        <div class="text-center">
                            <div class="spinner-border text-primary mb-3" role="status">
                                <span class="visually-hidden">Loading...</span>
                            </div>
                            <p>Restarting all services...</p>
                            <div class="progress">
                                <div class="progress-bar progress-bar-striped progress-bar-animated" 
                                     role="progressbar" style="width: 100%"></div>
                            </div>
                            <small class="text-muted mt-2 d-block">
                                This may take up to 2 minutes. Please wait...
                            </small>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);
    }

    showSuccessDialog(message) {
        // Remove progress dialog
        const progressDialog = document.getElementById('restart-progress-dialog');
        if (progressDialog) {
            progressDialog.remove();
        }

        const dialog = document.createElement('div');
        dialog.id = 'restart-success-dialog';
        dialog.className = 'modal fade show';
        dialog.style.display = 'block';
        dialog.style.backgroundColor = 'rgba(0,0,0,0.5)';
        dialog.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">✅ Restart Initiated</h5>
                    </div>
                    <div class="modal-body">
                        <p>${message}</p>
                        <p>The portal will reload automatically in a few seconds.</p>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            dialog.remove();
        }, 5000);
    }

    showErrorDialog(error) {
        // Remove progress dialog
        const progressDialog = document.getElementById('restart-progress-dialog');
        if (progressDialog) {
            progressDialog.remove();
        }

        const dialog = document.createElement('div');
        dialog.id = 'restart-error-dialog';
        dialog.className = 'modal fade show';
        dialog.style.display = 'block';
        dialog.style.backgroundColor = 'rgba(0,0,0,0.5)';
        dialog.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">❌ Restart Failed</h5>
                    </div>
                    <div class="modal-body">
                        <p>${error}</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" onclick="this.closest('.modal').remove()">Close</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(dialog);
    }
}

// Initialize system restart functionality when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.systemRestart = new SystemRestart();
});

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SystemRestart;
} 