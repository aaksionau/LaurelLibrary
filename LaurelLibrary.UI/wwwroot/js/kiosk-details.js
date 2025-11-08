/**
 * Kiosk Details JavaScript functionality
 * Handles browser fingerprinting, kiosk registration/unregistration, and status management
 */

// State variables
let isCurrentComputerKiosk = false;
let kioskData = {};

// Initialize the page when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    initializeKioskDetails();
});

/**
 * Initialize kiosk details functionality
 */
function initializeKioskDetails() {
    // Extract kiosk data from the page (will be set by the Razor page)
    if (window.kioskPageData) {
        kioskData = window.kioskPageData;
        checkIfCurrentComputerIsKiosk();
    }
}

/**
 * Check if the current computer is registered as this kiosk
 */
async function checkIfCurrentComputerIsKiosk() {
    try {
        // Check localStorage first
        const storedKioskId = localStorage.getItem('kioskId');
        const storedLibraryId = localStorage.getItem('libraryId');
        const currentKioskId = kioskData.kioskId;
        const currentLibraryId = kioskData.libraryId;

        if (storedKioskId == currentKioskId && storedLibraryId === currentLibraryId) {
            // Get current browser fingerprint
            const fp = await FingerprintJS.load();
            const result = await fp.get();
            const currentFingerprint = result.visitorId;
            const kioskFingerprint = kioskData.browserFingerprint;

            // Check if current browser fingerprint matches kiosk fingerprint
            if (currentFingerprint === kioskFingerprint) {
                isCurrentComputerKiosk = true;
            }
        }

        updateButtonState();
    } catch (error) {
        console.error('Error checking kiosk status:', error);
        updateButtonState();
    }
}

/**
 * Update the button state based on current kiosk status
 */
function updateButtonState() {
    const btn = document.getElementById('makeKioskBtn');
    if (!btn) return;

    if (isCurrentComputerKiosk) {
        btn.classList.remove('btn-success');
        btn.classList.add('btn-warning');
        btn.innerHTML = '<i class="bi bi-pc-display-horizontal me-2"></i>Unregister This Computer';
    } else {
        btn.classList.remove('btn-warning');
        btn.classList.add('btn-success');
        btn.innerHTML = '<i class="bi bi-pc-display me-2"></i>Make Current Computer a Kiosk';
    }
}

/**
 * Toggle kiosk status (register/unregister)
 */
async function toggleKioskStatus() {
    if (isCurrentComputerKiosk) {
        await removeCurrentComputerAsKiosk();
    } else {
        await makeCurrentComputerKiosk();
    }
}

/**
 * Register the current computer as a kiosk
 */
async function makeCurrentComputerKiosk() {
    const btn = document.getElementById('makeKioskBtn');
    if (!btn) return;

    try {
        // Disable button and show loading state
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Setting up...';

        // Initialize FingerprintJS
        const fp = await FingerprintJS.load();
        const result = await fp.get();
        const visitorId = result.visitorId;

        // Get kiosk details
        const kioskId = kioskData.kioskId;
        const libraryId = kioskData.libraryId;

        // Save to localStorage
        localStorage.setItem('kioskId', kioskId);
        localStorage.setItem('libraryId', libraryId);

        // Send fingerprint to server to update kiosk
        const response = await fetch(`/Administration/Kiosks/Details/${libraryId}/${kioskId}?handler=UpdateFingerprint`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                kioskId: kioskId,
                browserFingerprint: visitorId
            })
        });

        if (response.ok) {
            // Update state
            isCurrentComputerKiosk = true;

            // Show success message
            btn.classList.remove('btn-success');
            btn.classList.add('btn-outline-success');
            btn.innerHTML = '<i class="bi bi-check-circle me-2"></i>Kiosk Setup Complete!';

            // Reload page after 2 seconds to show updated fingerprint
            setTimeout(() => {
                window.location.reload();
            }, 2000);
        } else {
            throw new Error('Failed to update kiosk');
        }
    } catch (error) {
        console.error('Error setting up kiosk:', error);
        showErrorState(btn, 'Setup Failed');
    }
}

/**
 * Unregister the current computer as a kiosk
 */
async function removeCurrentComputerAsKiosk() {
    const btn = document.getElementById('makeKioskBtn');
    if (!btn) return;

    try {
        // Disable button and show loading state
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Removing...';

        // Get kiosk details
        const kioskId = kioskData.kioskId;
        const libraryId = kioskData.libraryId;

        // Send request to server to clear fingerprint
        const response = await fetch(`/Administration/Kiosks/Details/${libraryId}/${kioskId}?handler=UpdateFingerprint`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                kioskId: kioskId,
                browserFingerprint: null
            })
        });

        if (response.ok) {
            // Clear localStorage
            localStorage.removeItem('kioskId');
            localStorage.removeItem('libraryId');

            // Update state
            isCurrentComputerKiosk = false;

            // Show success message
            btn.classList.remove('btn-warning');
            btn.classList.add('btn-outline-success');
            btn.innerHTML = '<i class="bi bi-check-circle me-2"></i>Kiosk Removed!';

            // Reload page after 2 seconds to show updated fingerprint
            setTimeout(() => {
                window.location.reload();
            }, 2000);
        } else {
            throw new Error('Failed to remove kiosk');
        }
    } catch (error) {
        console.error('Error removing kiosk:', error);
        showErrorState(btn, 'Removal Failed');
    }
}

/**
 * Show error state on button
 */
function showErrorState(btn, message) {
    btn.disabled = false;
    btn.classList.add('btn-danger');
    btn.innerHTML = `<i class="bi bi-x-circle me-2"></i>${message}`;

    // Reset button after 3 seconds
    setTimeout(() => {
        btn.classList.remove('btn-danger');
        updateButtonState();
        btn.disabled = false;
    }, 3000);
}

/**
 * Get the anti-forgery token from the page
 */
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

/**
 * Set kiosk data (to be called by the Razor page)
 */
function setKioskData(data) {
    kioskData = data;
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', checkIfCurrentComputerIsKiosk);
    } else {
        checkIfCurrentComputerIsKiosk();
    }
}

// Export functions for global access
window.toggleKioskStatus = toggleKioskStatus;
window.setKioskData = setKioskData;