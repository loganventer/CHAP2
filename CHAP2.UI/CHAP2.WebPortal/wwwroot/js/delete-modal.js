// Delete Modal JavaScript for CHAP2 Web Portal

let currentChorusId = null;
let currentChorusName = null;

// Show delete confirmation modal
function showDeleteConfirmation(chorusId, chorusName) {
    currentChorusId = chorusId;
    currentChorusName = chorusName;
    
    // Update the modal content
    document.getElementById('deleteChorusName').textContent = chorusName;
    
    // Show the modal
    const modal = document.getElementById('deleteModal');
    modal.classList.add('show');
    
    // Prevent body scroll
    document.body.style.overflow = 'hidden';
    
    // Focus on the cancel button for accessibility
    setTimeout(() => {
        const cancelButton = modal.querySelector('.btn-secondary');
        if (cancelButton) {
            cancelButton.focus();
        }
    }, 100);
}

// Hide delete confirmation modal
function hideDeleteModal() {
    const modal = document.getElementById('deleteModal');
    modal.classList.remove('show');
    
    // Restore body scroll
    document.body.style.overflow = '';
    
    // Clear current values
    currentChorusId = null;
    currentChorusName = null;
}

// Confirm delete action
async function confirmDelete() {
    if (!currentChorusId) {
        console.error('No chorus ID set for deletion');
        return;
    }
    
    try {
        // Show loading state
        const deleteButton = document.querySelector('.delete-modal-actions .btn-danger');
        const originalText = deleteButton.innerHTML;
        deleteButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Deleting...';
        deleteButton.disabled = true;
        
        // Make API call
        const response = await fetch(`/Home/Delete/${currentChorusId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Show success message
            showNotification('Chorus deleted successfully', 'success');
            
            // Hide modal
            hideDeleteModal();
            
            // Redirect to home page after a short delay
            setTimeout(() => {
                window.location.href = '/';
            }, 1500);
        } else {
            // Show error message
            showNotification(result.message || 'Failed to delete chorus', 'error');
            
            // Reset button
            deleteButton.innerHTML = originalText;
            deleteButton.disabled = false;
        }
    } catch (error) {
        console.error('Error deleting chorus:', error);
        showNotification('An error occurred while deleting the chorus', 'error');
        
        // Reset button
        const deleteButton = document.querySelector('.delete-modal-actions .btn-danger');
        deleteButton.innerHTML = '<i class="fas fa-trash"></i> Delete Chorus';
        deleteButton.disabled = false;
    }
}

// Get anti-forgery token
function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

// Show notification
function showNotification(message, type = 'info', duration = 5000) {
    // Remove existing notifications
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => notification.remove());
    
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-message">${message}</span>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;
    
    // Add to page
    document.body.appendChild(notification);
    
    // Auto-remove after duration
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, duration);
}

// Close modal when clicking outside
document.addEventListener('DOMContentLoaded', function() {
    const modal = document.getElementById('deleteModal');
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                hideDeleteModal();
            }
        });
    }
    
    // Close modal with Escape key
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            const modal = document.getElementById('deleteModal');
            if (modal && modal.classList.contains('show')) {
                hideDeleteModal();
            }
        }
    });
});

// Prevent modal from closing when clicking inside the modal
document.addEventListener('DOMContentLoaded', function() {
    const modalContent = document.querySelector('.delete-modal');
    if (modalContent) {
        modalContent.addEventListener('click', function(e) {
            e.stopPropagation();
        });
    }
});

// Add loading state to the page during deletion
function addPageLoadingState() {
    const container = document.querySelector('.detail-container');
    if (container) {
        container.classList.add('loading');
    }
}

function removePageLoadingState() {
    const container = document.querySelector('.detail-container');
    if (container) {
        container.classList.remove('loading');
    }
}

// Enhanced confirm delete with additional confirmation
function confirmDeleteWithDoubleCheck() {
    const deleteButton = document.querySelector('.delete-modal-actions .btn-danger');
    const originalText = deleteButton.innerHTML;
    
    // Change button text to require double-click
    deleteButton.innerHTML = '<i class="fas fa-exclamation-triangle"></i> Click again to confirm';
    deleteButton.style.background = 'linear-gradient(135deg, #ff6b6b 0%, #ee5a52 100%)';
    
    // Set a timeout to revert the button
    setTimeout(() => {
        deleteButton.innerHTML = originalText;
        deleteButton.style.background = '';
    }, 3000);
    
    // Replace the onclick handler temporarily
    deleteButton.onclick = function() {
        deleteButton.onclick = confirmDelete; // Restore original handler
        confirmDelete();
    };
}

// Initialize delete modal functionality
document.addEventListener('DOMContentLoaded', function() {
    // Add anti-forgery token to the page if not present
    if (!document.querySelector('input[name="__RequestVerificationToken"]')) {
        const token = document.querySelector('meta[name="__RequestVerificationToken"]');
        if (token) {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = '__RequestVerificationToken';
            input.value = token.getAttribute('content');
            document.body.appendChild(input);
        }
    }
}); 