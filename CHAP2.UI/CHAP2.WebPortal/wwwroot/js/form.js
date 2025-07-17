// Form JavaScript for CHAP2 Web Portal

document.addEventListener('DOMContentLoaded', function() {
    // Initialize form functionality
    initializeForm();
    
    // Add loading state to form submission
    setupFormSubmission();
    
    // Add character counter for textarea
    setupCharacterCounter();
    
    // Add auto-save functionality
    setupAutoSave();
});

function initializeForm() {
    // Add focus effects to form inputs
    const inputs = document.querySelectorAll('.form-input, .form-select, .form-textarea');
    inputs.forEach(input => {
        input.addEventListener('focus', function() {
            this.parentElement.classList.add('focused');
        });
        
        input.addEventListener('blur', function() {
            this.parentElement.classList.remove('focused');
        });
    });
    
    // Add real-time validation feedback
    inputs.forEach(input => {
        input.addEventListener('input', function() {
            validateField(this);
        });
        
        input.addEventListener('blur', function() {
            validateField(this);
        });
    });
}

function setupFormSubmission() {
    const forms = document.querySelectorAll('.chorus-form');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            // Add loading state
            document.body.classList.add('loading');
            
            // Show loading message
            showNotification('Saving chorus...', 'info');
            
            // Remove loading state after a delay (in case of errors)
            setTimeout(() => {
                document.body.classList.remove('loading');
            }, 10000);
        });
    });
}

function setupCharacterCounter() {
    const textarea = document.querySelector('.form-textarea');
    if (textarea) {
        const counter = document.createElement('div');
        counter.className = 'character-counter';
        counter.style.cssText = `
            font-size: 0.8rem;
            color: rgba(255, 255, 255, 0.6);
            text-align: right;
            margin-top: 0.25rem;
        `;
        textarea.parentElement.appendChild(counter);
        
        function updateCounter() {
            const count = textarea.value.length;
            const maxLength = 10000; // Reasonable limit for chorus text
            counter.textContent = `${count} / ${maxLength} characters`;
            
            if (count > maxLength * 0.9) {
                counter.style.color = '#ff6b6b';
            } else if (count > maxLength * 0.7) {
                counter.style.color = '#ffd93d';
            } else {
                counter.style.color = 'rgba(255, 255, 255, 0.6)';
            }
        }
        
        textarea.addEventListener('input', updateCounter);
        updateCounter(); // Initial count
    }
}

function setupAutoSave() {
    const textarea = document.querySelector('.form-textarea');
    const nameInput = document.querySelector('input[name="Name"]');
    let autoSaveTimeout;
    
    function autoSave() {
        const formData = {
            name: nameInput?.value || '',
            text: textarea?.value || '',
            timestamp: new Date().toISOString()
        };
        
        localStorage.setItem('chorusFormDraft', JSON.stringify(formData));
        showNotification('Draft saved', 'info', 2000);
    }
    
    function debounce(func, wait) {
        clearTimeout(autoSaveTimeout);
        autoSaveTimeout = setTimeout(func, wait);
    }
    
    if (textarea) {
        textarea.addEventListener('input', () => debounce(autoSave, 2000));
    }
    
    if (nameInput) {
        nameInput.addEventListener('input', () => debounce(autoSave, 2000));
    }
    
    // Restore draft on page load
    const draft = localStorage.getItem('chorusFormDraft');
    if (draft && !isEditPage()) {
        try {
            const data = JSON.parse(draft);
            if (textarea && data.text) {
                textarea.value = data.text;
                textarea.dispatchEvent(new Event('input'));
            }
            if (nameInput && data.name) {
                nameInput.value = data.name;
                nameInput.dispatchEvent(new Event('input'));
            }
        } catch (e) {
            console.warn('Failed to restore draft:', e);
        }
    }
    
    // Clear draft on successful submission
    const form = document.querySelector('.chorus-form');
    if (form) {
        form.addEventListener('submit', () => {
            localStorage.removeItem('chorusFormDraft');
        });
    }
}

function isEditPage() {
    return window.location.pathname.includes('/Edit');
}

function validateField(field) {
    const value = field.value.trim();
    const fieldName = field.name;
    const errorElement = field.parentElement.querySelector('.validation-error');
    
    // Clear previous error
    field.classList.remove('input-validation-error');
    if (errorElement) {
        errorElement.textContent = '';
    }
    
    // Validation rules
    let isValid = true;
    let errorMessage = '';
    
    switch (fieldName) {
        case 'Name':
            if (!value) {
                isValid = false;
                errorMessage = 'Chorus name is required';
            } else if (value.length > 200) {
                isValid = false;
                errorMessage = 'Chorus name cannot exceed 200 characters';
            }
            break;
            
        case 'ChorusText':
            if (!value) {
                isValid = false;
                errorMessage = 'Chorus text is required';
            }
            break;
            
        case 'Key':
            if (!value || value === 'NotSet') {
                isValid = false;
                errorMessage = 'Please select a musical key';
            }
            break;
            
        case 'Type':
            if (!value || value === 'NotSet') {
                isValid = false;
                errorMessage = 'Please select a chorus type';
            }
            break;
            
        case 'TimeSignature':
            if (!value || value === 'NotSet') {
                isValid = false;
                errorMessage = 'Please select a time signature';
            }
            break;
    }
    
    // Apply validation result
    if (!isValid) {
        field.classList.add('input-validation-error');
        if (errorElement) {
            errorElement.textContent = errorMessage;
        }
    }
    
    return isValid;
}

function validateForm() {
    const inputs = document.querySelectorAll('.form-input, .form-select, .form-textarea');
    let isValid = true;
    
    inputs.forEach(input => {
        if (!validateField(input)) {
            isValid = false;
        }
    });
    
    return isValid;
}

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

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // Ctrl/Cmd + S to save
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        const submitButton = document.querySelector('.btn-primary');
        if (submitButton) {
            submitButton.click();
        }
    }
    
    // Escape to cancel
    if (e.key === 'Escape') {
        const cancelButton = document.querySelector('.btn-secondary');
        if (cancelButton) {
            cancelButton.click();
        }
    }
});

// Form enhancement: Auto-resize textarea
function autoResizeTextarea() {
    const textarea = document.querySelector('.form-textarea');
    if (textarea) {
        textarea.addEventListener('input', function() {
            this.style.height = 'auto';
            this.style.height = this.scrollHeight + 'px';
        });
        
        // Initial resize
        textarea.dispatchEvent(new Event('input'));
    }
}

// Initialize auto-resize
document.addEventListener('DOMContentLoaded', autoResizeTextarea);

// Form enhancement: Smart placeholder
function setupSmartPlaceholder() {
    const textarea = document.querySelector('.form-textarea');
    if (textarea) {
        const placeholder = textarea.placeholder;
        let placeholderIndex = 0;
        let placeholderInterval;
        
        textarea.addEventListener('focus', function() {
            if (!this.value) {
                placeholderInterval = setInterval(() => {
                    this.placeholder = placeholder.substring(0, placeholderIndex + 1);
                    placeholderIndex++;
                    if (placeholderIndex >= placeholder.length) {
                        clearInterval(placeholderInterval);
                    }
                }, 50);
            }
        });
        
        textarea.addEventListener('blur', function() {
            clearInterval(placeholderInterval);
            this.placeholder = placeholder;
            placeholderIndex = 0;
        });
    }
}

// Initialize smart placeholder
document.addEventListener('DOMContentLoaded', setupSmartPlaceholder); 