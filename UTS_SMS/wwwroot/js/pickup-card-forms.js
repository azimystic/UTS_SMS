// Handle edit pickup card form submission
document.getElementById('editPickupCardForm').addEventListener('submit', function(e) {
    e.preventDefault();
    const formData = new FormData(this);
    const studentId = document.getElementById('editStudentId').value;
    const submitBtn = this.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    
    // Show loading state
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="inline-flex items-center"><svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>Updating...</span>';
    
    fetch('/Students/EditPickupCard', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        // Reset button state immediately upon receiving response
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        return response.json();
    })
    .then(data => {
        if (data.success) {
            closeEditPickupCardModal();
            loadPickupCards(studentId);
            showNotification('Pickup card updated successfully', 'success');
        } else {
            showNotification(data.message || 'Error updating pickup card', 'error');
        }
    })
    .catch(error => {
        // Reset button state on error
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        console.error('Error:', error);
        showNotification('Error updating pickup card', 'error');
    });
});

// Handle add pickup card form submission
document.getElementById('addPickupCardForm').addEventListener('submit', function(e) {
    e.preventDefault();
    const formData = new FormData(this);
    const submitBtn = this.querySelector('button[type="submit"]');
    const originalText = submitBtn.innerHTML;
    
    // Show loading state
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="inline-flex items-center"><svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>Adding...</span>';
    
    fetch('/Students/AddPickupCard', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        // Reset button state immediately upon receiving response
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        return response.json();
    })
    .then(data => {
        if (data.success) {
            this.reset();
            loadPickupCards(document.getElementById('studentIdForPickup').value);
            showNotification('Pickup card added successfully', 'success');
        } else {
            showNotification(data.message || 'Error adding pickup card', 'error');
        }
    })
    .catch(error => {
        // Reset button state on error
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
        console.error('Error:', error);
        showNotification('Error adding pickup card', 'error');
    });
});
