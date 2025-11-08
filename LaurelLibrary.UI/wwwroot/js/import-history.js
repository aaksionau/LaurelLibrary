/**
 * Import History JavaScript functionality
 * Handles progress polling, navigation, and UI interactions for book import operations
 */

// Initialize the page when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    initializeTooltips();
    initializeFileInput();
    initializeProgressPolling();
});

/**
 * Initialize Bootstrap tooltips
 */
function initializeTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * Function to switch to history tab
 */
function switchToHistoryTab() {
    const urlParams = new URLSearchParams(window.location.search);
    const pageNumber = urlParams.get('pageNumber') || '1';
    const pageSize = urlParams.get('pageSize') || '10';
    window.location.href = `?tab=history&pageNumber=${pageNumber}&pageSize=${pageSize}`;
}

/**
 * Function to switch to import tab
 */
function switchToImportTab() {
    const urlParams = new URLSearchParams(window.location.search);
    const pageNumber = urlParams.get('pageNumber') || '1';
    const pageSize = urlParams.get('pageSize') || '10';
    window.location.href = `?pageNumber=${pageNumber}&pageSize=${pageSize}`;
}

/**
 * Initialize file input event listener
 */
function initializeFileInput() {
    const fileInput = document.getElementById('csvFile');
    if (fileInput) {
        fileInput.addEventListener('change', function (e) {
            if (e.target.files.length > 0) {
                var fileName = e.target.files[0].name;
                var fileSize = (e.target.files[0].size / 1024).toFixed(2);
                console.log('Selected file: ' + fileName + ' (' + fileSize + ' KB)');
            }
        });
    }
}

/**
 * Initialize progress polling for active imports
 */
function initializeProgressPolling() {
    const importHistoryIdElement = document.getElementById('importHistoryId');
    if (importHistoryIdElement) {
        const importHistoryId = importHistoryIdElement.value;
        if (importHistoryId) {
            const progressPoller = new ImportProgressPoller(importHistoryId);
            progressPoller.start();
        }
    }
}

/**
 * Import Progress Polling Class
 * Handles real-time polling of import progress
 */
class ImportProgressPoller {
    constructor(importHistoryId) {
        this.importHistoryId = importHistoryId;
        this.pollInterval = null;
        this.isPolling = false;
        this.pollIntervalMs = 2000; // Poll every 2 seconds
    }

    start() {
        if (this.isPolling) return;

        this.isPolling = true;
        console.log('Starting import progress polling for ID:', this.importHistoryId);

        // Poll immediately
        this.pollProgress();

        // Set up polling interval
        this.pollInterval = setInterval(() => this.pollProgress(), this.pollIntervalMs);

        // Clean up when page unloads
        window.addEventListener('beforeunload', () => this.stop());
    }

    stop() {
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
            this.pollInterval = null;
        }
        this.isPolling = false;
        console.log('Stopped import progress polling');
    }

    async pollProgress() {
        try {
            const response = await fetch(`/api/ImportProgress/${this.importHistoryId}`);

            if (!response.ok) {
                if (response.status === 404) {
                    console.warn('Import history not found');
                    this.stop();
                    return;
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            this.updateProgress(data);

            // Stop polling if import is completed or failed
            if (data.status === 'Completed' || data.status === 'Failed') {
                this.showCompletedState();
                this.stop();
            }

        } catch (error) {
            console.error('Error polling import progress:', error);
            // Don't stop polling on error, just log it
        }
    }

    updateProgress(data) {
        // Update progress bar
        const progressPercentage = data.totalChunks > 0
            ? Math.round((data.processedChunks / data.totalChunks) * 100)
            : 0;

        const progressBar = document.getElementById('progressBar');
        const progressText = document.getElementById('progressText');

        if (progressBar) {
            progressBar.style.width = progressPercentage + '%';
            progressBar.setAttribute('aria-valuenow', progressPercentage);
        }

        if (progressText) {
            progressText.textContent = progressPercentage + '%';
        }

        // Update counters
        this.updateElement('processedChunks', data.processedChunks);
        this.updateElement('totalChunks', data.totalChunks);
        this.updateElement('successCount', data.successCount);
        this.updateElement('failedCount', data.failedCount);

        // Update status badge
        this.updateStatusBadge(data.status);
    }

    updateElement(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = value;
        }
    }

    updateStatusBadge(status) {
        const statusBadge = document.getElementById('statusBadge');
        if (!statusBadge) return;

        switch (status) {
            case 'Completed':
                statusBadge.className = 'badge bg-success ms-2';
                statusBadge.innerHTML = '<i class="fas fa-check-circle"></i> Completed';
                break;
            case 'Failed':
                statusBadge.className = 'badge bg-danger ms-2';
                statusBadge.innerHTML = '<i class="fas fa-exclamation-circle"></i> Failed';
                break;
            default:
                statusBadge.className = 'badge bg-info ms-2';
                statusBadge.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing';
                break;
        }
    }

    showCompletedState() {
        // Remove animation from progress bar
        const progressBar = document.getElementById('progressBar');
        if (progressBar) {
            progressBar.classList.remove('progress-bar-animated', 'progress-bar-striped');
        }

        // Show completed actions
        const completedActions = document.getElementById('completedActions');
        if (completedActions) {
            completedActions.style.display = 'block';
        }

        // Update title
        const statusTitle = document.getElementById('statusTitle');
        if (statusTitle) {
            statusTitle.textContent = 'Import Completed';
        }
    }
}

// Export functions for global access if needed
window.switchToHistoryTab = switchToHistoryTab;
window.switchToImportTab = switchToImportTab;