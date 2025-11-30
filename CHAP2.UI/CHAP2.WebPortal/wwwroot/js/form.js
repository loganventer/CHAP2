// Modern Form JavaScript for CHAP2 Web Portal

document.addEventListener('DOMContentLoaded', function() {
    console.log('Form page loaded');
    
    // Initialize form functionality
    initializeForm();
    
    // Add loading state to form submission
    setupFormSubmission();
    
    // Add character counter for textarea
    setupCharacterCounter();
    
    // Add auto-save functionality
    setupAutoSave();
    
    // Add form enhancement
    autoResizeTextarea();
    setupSmartPlaceholder();
    
    console.log('Form initialization complete');
});

function initializeForm() {
    console.log('Initializing form...');
    
    // Add focus effects to form inputs
    const inputs = document.querySelectorAll('.form-input, .form-select, .form-textarea');
    console.log('Found inputs:', inputs.length);
    
    inputs.forEach(input => {
        input.addEventListener('focus', function() {
            this.parentElement.classList.add('focused');
            console.log('Input focused:', this.name);
        });
        
        input.addEventListener('blur', function() {
            this.parentElement.classList.remove('focused');
        });
    });
    
    // Add real-time validation feedback using HTML5 validation
    inputs.forEach(input => {
        input.addEventListener('input', function() {
            validateField(this);
        });
        
        input.addEventListener('blur', function() {
            validateField(this);
        });
    });
    
    console.log('Form initialization complete');
}

function setupFormSubmission() {
    const forms = document.querySelectorAll('.chorus-form');
    console.log('Found forms:', forms.length);
    
    forms.forEach((form, index) => {
        console.log(`Setting up form ${index + 1}:`, form);
        console.log('Form action:', form.action);
        console.log('Form method:', form.method);
        console.log('Form enctype:', form.enctype);
        
        form.addEventListener('submit', function(e) {
            console.log('=== FORM SUBMISSION START ===');
            console.log('Form submission event triggered');
            console.log('Form action:', form.action);
            console.log('Form method:', form.method);
            console.log('Form enctype:', form.enctype);
            console.log('Event type:', e.type);
            
            // Log form data
            const formData = new FormData(form);
            console.log('Form data entries:');
            for (let [key, value] of formData.entries()) {
                console.log(`  ${key}: ${value}`);
            }
            
            // Log form elements
            console.log('Form elements:');
            const elements = form.elements;
            for (let i = 0; i < elements.length; i++) {
                const element = elements[i];
                if (element.name) {
                    console.log(`  ${element.name}: ${element.value} (type: ${element.type})`);
                }
            }
            
            // Use HTML5 validation first
            console.log('Checking HTML5 validity...');
            if (!form.checkValidity()) {
                console.log('HTML5 validation failed');
                e.preventDefault();
                form.reportValidity();
                showNotification('Please fix the errors before submitting', 'error');
                return;
            }
            console.log('HTML5 validation passed');
            
            // Additional custom validation
            console.log('Running custom validation...');
            if (!validateForm()) {
                console.log('Custom validation failed');
                e.preventDefault();
                showNotification('Please fix the errors before submitting', 'error');
                return;
            }
            console.log('Custom validation passed');
            
            // Add loading state
            console.log('Adding loading state...');
            document.body.classList.add('loading');
            
            // Show loading message
            showNotification('Saving chorus...', 'info');
            
            console.log('Form submission proceeding...');
            console.log('=== FORM SUBMISSION END ===');
            
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
    
    // Use HTML5 validation first
    if (!field.checkValidity()) {
        field.classList.add('input-validation-error');
        if (errorElement) {
            errorElement.textContent = field.validationMessage;
        }
        return false;
    }
    
    // Additional custom validation
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

// Save Confirmation Modal Functions
function showSaveConfirmation() {
    console.log('Showing save confirmation modal');
    
    // Validate form first
    const form = document.querySelector('.chorus-form');
    if (!form.checkValidity()) {
        form.reportValidity();
        showNotification('Please fix the errors before saving', 'error');
        return;
    }
    
    // Get the chorus name for confirmation
    const chorusNameInput = document.querySelector('input[name="Name"]');
    const chorusName = chorusNameInput ? chorusNameInput.value.trim() : 'Unknown Chorus';
    
    // Update the confirmation modal with the chorus name
    const confirmChorusName = document.getElementById('confirmChorusName');
    if (confirmChorusName) {
        confirmChorusName.textContent = chorusName;
    }
    
    // Show the modal
    const modal = document.getElementById('saveConfirmationModal');
    if (modal) {
        modal.style.display = 'flex';
        modal.classList.add('show');
        
        // Focus the first button for accessibility
        const firstButton = modal.querySelector('button');
        if (firstButton) {
            firstButton.focus();
        }
    }
}

function hideSaveConfirmation() {
    console.log('Hiding save confirmation modal');
    
    const modal = document.getElementById('saveConfirmationModal');
    if (modal) {
        modal.classList.remove('show');
        setTimeout(() => {
            modal.style.display = 'none';
        }, 300);
    }
}

function confirmSave() {
    console.log('User confirmed save, submitting form');
    
    // Hide the modal
    hideSaveConfirmation();
    
    // Submit the form
    const form = document.querySelector('.chorus-form');
    if (form) {
        // Add loading state
        document.body.classList.add('loading');
        
        // Show loading message
        showNotification('Saving changes...', 'info');
        
        // Submit the form
        form.submit();
    }
}

// Close modal when clicking outside
document.addEventListener('click', function(e) {
    const modal = document.getElementById('saveConfirmationModal');
    if (modal && e.target === modal) {
        hideSaveConfirmation();
    }
});

// Close modal with Escape key
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        hideSaveConfirmation();
    }
});

// Close window function
function closeWindow() {
    // If this window was opened from another window, refresh the parent
    if (window.opener && !window.opener.closed) {
        window.opener.location.reload();
    }
    window.close();
}

// Add global function
window.closeWindow = closeWindow;

// Insert page break function
function insertPageBreak() {
    const textarea = document.getElementById('chorusTextArea');
    if (!textarea) return;

    const cursorPosition = textarea.selectionStart;
    const textBefore = textarea.value.substring(0, cursorPosition);
    const textAfter = textarea.value.substring(cursorPosition);

    // Insert [PAGE] marker at cursor position
    const pageBreak = '[PAGE]';
    textarea.value = textBefore + pageBreak + textAfter;

    // Set cursor position after the inserted page break
    const newCursorPosition = cursorPosition + pageBreak.length;
    textarea.setSelectionRange(newCursorPosition, newCursorPosition);

    // Focus the textarea
    textarea.focus();

    // Trigger input event for auto-save and character counter
    textarea.dispatchEvent(new Event('input'));

    // Show notification
    showNotification('Page break inserted', 'success', 2000);
}

// Make function globally available
window.insertPageBreak = insertPageBreak; 