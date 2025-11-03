// Kiosk page JavaScript functionality

// Success overlay functionality
function closeSuccessOverlay() {
    const overlay = document.getElementById('successOverlay');
    overlay.style.display = 'none';
}

function closeReturnSuccessOverlay() {
    const overlay = document.getElementById('returnSuccessOverlay');
    overlay.style.display = 'none';
}

// Initialize kiosk page functionality
function initializeKioskPage(options) {
    const { showCheckoutSuccess = false, showReturnSuccess = false } = options;

    // Show success overlay if checkout was successful
    if (showCheckoutSuccess) {
        document.addEventListener('DOMContentLoaded', function () {
            const overlay = document.getElementById('successOverlay');
            overlay.style.display = 'flex';

            // Auto-close after 5 seconds
            setTimeout(function () {
                closeSuccessOverlay();
            }, 5000);
        });
    }

    // Show return success overlay if return was successful
    if (showReturnSuccess) {
        document.addEventListener('DOMContentLoaded', function () {
            const overlay = document.getElementById('returnSuccessOverlay');
            overlay.style.display = 'flex';

            // Auto-close after 5 seconds
            setTimeout(function () {
                closeReturnSuccessOverlay();
            }, 5000);
        });
    }
}

// Initialize FingerprintJS and handle redirect with localStorage parameters
async function initializeFingerprintAndRedirect() {
    const urlParams = new URLSearchParams(window.location.search);
    const hasLibraryId = urlParams.has('libraryId');
    const hasKioskId = urlParams.has('kioskId');
    const hasBrowserFingerprint = urlParams.has('browserFingerprint');

    // If all parameters are already in the URL, do nothing
    if (hasLibraryId && hasKioskId && hasBrowserFingerprint) {
        return;
    }

    // Get or generate browserFingerprint
    let browserFingerprint = localStorage.getItem('browserFingerprint');

    if (!browserFingerprint) {
        try {
            // Initialize FingerprintJS
            const fp = await FingerprintJS.load();
            const result = await fp.get();
            browserFingerprint = result.visitorId;

            // Save to localStorage
            localStorage.setItem('browserFingerprint', browserFingerprint);
        } catch (error) {
            console.error('Error generating fingerprint:', error);
            // Fallback to a random ID if FingerprintJS fails
            browserFingerprint = 'fallback-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('browserFingerprint', browserFingerprint);
        }
    }

    // Get other parameters from localStorage
    const libraryId = localStorage.getItem('libraryId');
    const kioskId = localStorage.getItem('kioskId');

    // If we have all required parameters in localStorage, redirect
    if (libraryId && kioskId && browserFingerprint) {
        const params = new URLSearchParams();
        params.set('libraryId', libraryId);
        params.set('kioskId', kioskId);
        params.set('browserFingerprint', browserFingerprint);

        // Redirect to the same page with parameters
        window.location.href = `${window.location.pathname}?${params.toString()}`;
    } else {
        // If no parameters available, show error message
        console.warn('Missing library/kiosk information in localStorage');
    }
}